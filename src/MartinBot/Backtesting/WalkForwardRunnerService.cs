using MartinBot.Configuration;
using MartinBot.Domain;
using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.RegimeSelector;
using MartinBot.Domain.Backtesting.Strategies;
using MartinBot.Domain.Entities;
using MartinBot.Domain.Entities.Models;
using Microsoft.Extensions.Options;

namespace MartinBot.Backtesting;

public sealed class WalkForwardRunnerService : BackgroundService
{
    private readonly WalkForwardQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<WalkForwardRunnerService> _logger;
    private readonly IRegimeSelector _selector;
    private readonly RegimeSelectorOptions _selectorOptions;

    public WalkForwardRunnerService(WalkForwardQueue queue, IServiceScopeFactory scopes,
        ILogger<WalkForwardRunnerService> logger, IRegimeSelector selector,
        IOptions<RegimeSelectorOptions> selectorOptions)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
        _selector = selector;
        _selectorOptions = selectorOptions.Value;
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

                // Warmup fix (strategies-roadmap-next.md §8a): run the engine on [trainFrom, testTo]
                // so indicators are primed by the test slice, then extract OOS metrics from the post-testFrom
                // portion of the resulting equity curve. Keeps train-only grid selection above untouched.
                var combinedSlice = SliceCandles(candles, window.TrainFrom, window.TestTo);
                var combinedRequest = new BacktestRequest(run.Pair, run.Timeframe, window.TrainFrom, window.TestTo,
                    run.InitialCash, run.FeeBps, run.SlippageBps);

                // Regime selector (docs/strategies.md §6, docs/phase6-experiments.md). When enabled
                // and the train slice classifies as TrendDown, swap to NoOpStrategy for the OOS run.
                // Selector sees only pre-testFrom candles, so the decision is strictly causal.
                IStrategy bestStrategy;
                if (_selectorOptions.Enabled)
                {
                    var decision = _selector.Decide(trainSlice);
                    _logger.LogInformation($"Walk-forward run {runId} window {window.Index} regime: {decision.Reason}");
                    bestStrategy = decision.ShouldPause
                        ? new NoOpStrategy()
                        : factory.Create(run.StrategyName, combinedRequest, bestCombo);
                }
                else
                {
                    bestStrategy = factory.Create(run.StrategyName, combinedRequest, bestCombo);
                }
                var combinedResult = engine.Run(combinedRequest, combinedSlice, bestStrategy);
                var oos = WalkForwardTestMetrics.Extract(combinedResult, window.TestFrom, run.Timeframe);

                var bestParamsJson = StrategyParametersSerializer.Serialize(bestCombo) ?? "{}";
                db.AddWalkForwardWindow(new WalkForwardWindowEntity(
                    id: 0, runId: run.Id, windowIndex: window.Index,
                    trainFrom: window.TrainFrom, trainTo: window.TrainTo,
                    testFrom: window.TestFrom, testTo: window.TestTo,
                    bestParametersJson: bestParamsJson, inSampleMetricValue: bestMetric ?? 0m,
                    outOfSampleTotalReturn: oos.TotalReturn,
                    outOfSampleMaxDrawdown: oos.MaxDrawdown,
                    outOfSampleSharpe: oos.Sharpe,
                    outOfSampleTradeCount: oos.TradeCount,
                    createdAt: DateTimeOffset.UtcNow));

                oosCurves.Add(oos.EquityCurve);
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
