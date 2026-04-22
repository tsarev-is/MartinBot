using MartinBot.Domain;
using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Entities;

namespace MartinBot.Backtesting;

public sealed class ParameterSweepRunnerService : BackgroundService
{
    private readonly ParameterSweepQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ParameterSweepRunnerService> _logger;

    public ParameterSweepRunnerService(ParameterSweepQueue queue, IServiceScopeFactory scopes,
        ILogger<ParameterSweepRunnerService> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Parameter sweep runner started");

        await foreach (var sweepId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await RunOneAsync(sweepId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Parameter sweep {sweepId} crashed unexpectedly");
            }
        }
    }

    private async Task RunOneAsync(long sweepId, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<BotContext>();
        var engine = sp.GetRequiredService<BacktestEngine>();
        var factory = sp.GetRequiredService<BacktestStrategyFactory>();
        var exmo = sp.GetRequiredService<IExmoService>();

        var sweep = await db.FindParameterSweepRunAsync(sweepId, ct);
        if (sweep is null)
        {
            _logger.LogWarning($"Parameter sweep {sweepId} not found in DB, skipping");
            return;
        }

        try
        {
            var grid = ParameterGridSerializer.Deserialize(sweep.ParameterGridJson);
            var total = ParameterGrid.CountCombinations(grid);
            sweep.MarkRunning(total, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);

            var candles = await exmo.GetCandlesHistoryAsync(sweep.Pair, sweep.Timeframe, sweep.From, sweep.To, ct);
            _logger.LogInformation($"Parameter sweep {sweepId}: fetched {candles.Count} candles, running {total} combinations");

            var request = new BacktestRequest(sweep.Pair, sweep.Timeframe, sweep.From, sweep.To, sweep.InitialCash, sweep.FeeBps, sweep.SlippageBps);

            decimal? bestMetric = null;
            var completed = 0;

            foreach (var combo in ParameterGrid.Cartesian(grid))
            {
                ct.ThrowIfCancellationRequested();

                var strategy = factory.Create(sweep.StrategyName, request, combo);
                var result = engine.Run(request, candles, strategy);
                var metricValue = OptimizationMetricSelector.Select(sweep.OptimizationMetric, result);

                if (bestMetric is null || metricValue > bestMetric.Value)
                {
                    bestMetric = metricValue;
                    var parametersJson = StrategyParametersSerializer.Serialize(combo);
                    sweep.RecordBest(parametersJson ?? "{}", metricValue, result, DateTimeOffset.UtcNow);
                }

                completed++;
                sweep.RecordProgress(completed, DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(ct);
            }

            sweep.MarkSucceeded(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation($"Parameter sweep {sweepId} done: best {sweep.OptimizationMetric}={bestMetric} params={sweep.BestParametersJson}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Parameter sweep {sweepId} failed");
            sweep.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
