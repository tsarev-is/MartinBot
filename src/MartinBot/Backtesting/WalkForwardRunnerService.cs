using MartinBot.Domain;
using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Entities;
using MartinBot.Domain.Entities.Models;

namespace MartinBot.Backtesting;

public sealed class WalkForwardRunnerService : BackgroundService
{
    private readonly WalkForwardQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<WalkForwardRunnerService> _logger;

    public WalkForwardRunnerService(WalkForwardQueue queue, IServiceScopeFactory scopes,
        ILogger<WalkForwardRunnerService> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Walk-forward runner started");

        await foreach (var runId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await RunOneAsync(runId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Walk-forward run {runId} crashed unexpectedly");
            }
        }
    }

    private async Task RunOneAsync(long runId, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<BotContext>();
        var engine = sp.GetRequiredService<BacktestEngine>();
        var factory = sp.GetRequiredService<BacktestStrategyFactory>();
        var exmo = sp.GetRequiredService<IExmoService>();

        var run = await db.FindWalkForwardRunAsync(runId, ct);
        if (run is null)
        {
            _logger.LogWarning($"Walk-forward run {runId} not found in DB, skipping");
            return;
        }

        try
        {
            var grid = ParameterGridSerializer.Deserialize(run.ParameterGridJson);
            var generated = WalkForwardWindowGenerator.Generate(run.From, run.To,
                TimeSpan.FromDays(run.TrainDays), TimeSpan.FromDays(run.TestDays),
                TimeSpan.FromDays(run.StepDays)).ToList();

            if (generated.Count == 0)
            {
                run.MarkFailed("range too short to fit a single train+test window", DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(ct);
                return;
            }

            var candles = await exmo.GetCandlesHistoryAsync(run.Pair, run.Timeframe, run.From, run.To, ct);

            var windows = new List<WalkForwardWindow>(generated.Count);
            foreach (var window in generated)
            {
                var trainCount = CountCandles(candles, window.TrainFrom, window.TrainTo);
                var testCount = CountCandles(candles, window.TestFrom, window.TestTo);
                if (trainCount == 0 || testCount == 0)
                {
                    _logger.LogWarning($"Walk-forward run {runId} window {window.Index}: skipping (train={trainCount}, test={testCount} candles)");
                    continue;
                }
                windows.Add(window);
            }

            if (windows.Count == 0)
            {
                run.MarkFailed("no windows contain candles in both train and test slices", DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(ct);
                return;
            }

            run.MarkRunning(windows.Count, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation($"Walk-forward run {runId}: fetched {candles.Count} candles, running {windows.Count} windows");

            var oosCurves = new List<IReadOnlyList<EquityPoint>>(windows.Count);
            var completed = 0;

            foreach (var window in windows)
            {
                ct.ThrowIfCancellationRequested();

                var trainSlice = SliceCandles(candles, window.TrainFrom, window.TrainTo);
                var testSlice = SliceCandles(candles, window.TestFrom, window.TestTo);

                var trainRequest = new BacktestRequest(run.Pair, run.Timeframe, window.TrainFrom, window.TrainTo,
                    run.InitialCash, run.FeeBps, run.SlippageBps);

                decimal? bestMetric = null;
                IReadOnlyDictionary<string, decimal>? bestCombo = null;
                foreach (var combo in ParameterGrid.Cartesian(grid))
                {
                    ct.ThrowIfCancellationRequested();
                    var strategy = factory.Create(run.StrategyName, trainRequest, combo);
                    var result = engine.Run(trainRequest, trainSlice, strategy);
                    var metric = OptimizationMetricSelector.Select(run.OptimizationMetric, result);
                    if (bestMetric is null || metric > bestMetric.Value)
                    {
                        bestMetric = metric;
                        bestCombo = combo;
                    }
                }

                var testRequest = new BacktestRequest(run.Pair, run.Timeframe, window.TestFrom, window.TestTo,
                    run.InitialCash, run.FeeBps, run.SlippageBps);
                var bestStrategy = factory.Create(run.StrategyName, testRequest, bestCombo);
                var testResult = engine.Run(testRequest, testSlice, bestStrategy);

                var bestParamsJson = StrategyParametersSerializer.Serialize(bestCombo) ?? "{}";
                db.AddWalkForwardWindow(new WalkForwardWindowEntity(
                    id: 0, runId: run.Id, windowIndex: window.Index,
                    trainFrom: window.TrainFrom, trainTo: window.TrainTo,
                    testFrom: window.TestFrom, testTo: window.TestTo,
                    bestParametersJson: bestParamsJson, inSampleMetricValue: bestMetric ?? 0m,
                    outOfSampleTotalReturn: testResult.TotalReturn,
                    outOfSampleMaxDrawdown: testResult.MaxDrawdown,
                    outOfSampleSharpe: testResult.Sharpe,
                    outOfSampleTradeCount: testResult.TradeCount,
                    createdAt: DateTimeOffset.UtcNow));

                oosCurves.Add(testResult.EquityCurve);
                completed++;
                run.RecordProgress(completed, DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(ct);
            }

            var aggregate = WalkForwardAggregator.Aggregate(oosCurves, run.InitialCash, run.Timeframe);
            run.RecordAggregate(aggregate.TotalReturn, aggregate.MaxDrawdown, aggregate.Sharpe, DateTimeOffset.UtcNow);
            run.MarkSucceeded(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation($"Walk-forward run {runId} done: aggregate return={aggregate.TotalReturn:P2} drawdown={aggregate.MaxDrawdown:P2} sharpe={aggregate.Sharpe:F2}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Walk-forward run {runId} failed");
            run.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static IReadOnlyList<Candle> SliceCandles(IReadOnlyList<Candle> candles,
        DateTimeOffset from, DateTimeOffset to)
    {
        var slice = new List<Candle>();
        foreach (var c in candles)
        {
            if (c.Timestamp >= from && c.Timestamp < to)
                slice.Add(c);
        }
        return slice;
    }

    // counting helper rather than reusing SliceCandles to avoid allocating a list we'll discard during pre-filtering
    private static int CountCandles(IReadOnlyList<Candle> candles, DateTimeOffset from, DateTimeOffset to)
    {
        var count = 0;
        foreach (var c in candles)
        {
            if (c.Timestamp >= from && c.Timestamp < to)
                count++;
        }
        return count;
    }
}
