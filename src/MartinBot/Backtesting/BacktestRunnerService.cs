using MartinBot.Domain;
using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Entities;

namespace MartinBot.Backtesting;

public sealed class BacktestRunnerService : BackgroundService
{
    private readonly BacktestQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<BacktestRunnerService> _logger;

    public BacktestRunnerService(BacktestQueue queue, IServiceScopeFactory scopes,
        ILogger<BacktestRunnerService> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backtest runner started");

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
                _logger.LogError(ex, $"Backtest run {runId} crashed unexpectedly");
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

        var run = await db.FindBacktestRunAsync(runId, ct);
        if (run is null)
        {
            _logger.LogWarning($"Backtest run {runId} not found in DB, skipping");
            return;
        }

        run.MarkRunning(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);

        try
        {
            var candles = await exmo.GetCandlesHistoryAsync(run.Pair, run.Timeframe, run.From, run.To, ct);
            _logger.LogInformation($"Backtest run {runId}: fetched {candles.Count} candles for {run.Pair} {run.Timeframe}");

            var request = new BacktestRequest(run.Pair, run.Timeframe, run.From, run.To,
                run.InitialCash, run.FeeBps, run.SlippageBps);
            var parameters = StrategyParametersSerializer.Deserialize(run.StrategyParametersJson);
            var strategy = factory.Create(run.StrategyName, request, parameters);
            var result = engine.Run(request, candles, strategy);

            run.MarkSucceeded(result.FinalEquity, result.TotalReturn, result.MaxDrawdown, result.Sharpe,
                result.TradeCount, result.WinRate, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation($"Backtest run {runId} done: return={result.TotalReturn:P2} drawdown={result.MaxDrawdown:P2} trades={result.TradeCount} dropped={result.DroppedIntents}");
            if (result.DroppedIntents > 0)
                _logger.LogWarning($"Backtest run {runId}: {result.DroppedIntents} strategy intent(s) discarded by affordability guard — observed fills do not reflect full strategy output");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Backtest run {runId} failed");
            run.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
