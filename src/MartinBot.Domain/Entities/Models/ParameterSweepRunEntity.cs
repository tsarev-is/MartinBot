using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Entities.Models;

/// <summary>
/// Record of a single parameter-sweep execution: fixed request range, a grid of parameter values,
/// the optimization metric, progress counters, and a summary of the best combination found.
/// Per-combo results are not persisted — if a combo's detailed metrics are needed later, re-run
/// a normal backtest with the stored <see cref="BestParametersJson"/>.
/// </summary>
public sealed class ParameterSweepRunEntity
{
    public ParameterSweepRunEntity(long id, string pair, string timeframe, DateTimeOffset from, DateTimeOffset to,
        decimal initialCash, decimal feeBps, decimal slippageBps, string strategyName,
        string? parameterGridJson, OptimizationMetric optimizationMetric,
        ParameterSweepRunStatus status, int? totalCombinations, int? completedCombinations,
        string? bestParametersJson, decimal? bestMetricValue,
        decimal? bestTotalReturn, decimal? bestMaxDrawdown, decimal? bestSharpe,
        int? bestTradeCount, decimal? bestWinRate, int? bestDroppedIntents,
        string? errorMessage, DateTimeOffset? startedAt, DateTimeOffset? completedAt,
        DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Pair = pair;
        Timeframe = timeframe;
        From = from;
        To = to;
        InitialCash = initialCash;
        FeeBps = feeBps;
        SlippageBps = slippageBps;
        StrategyName = strategyName;
        ParameterGridJson = parameterGridJson;
        OptimizationMetric = optimizationMetric;
        Status = status;
        TotalCombinations = totalCombinations;
        CompletedCombinations = completedCombinations;
        BestParametersJson = bestParametersJson;
        BestMetricValue = bestMetricValue;
        BestTotalReturn = bestTotalReturn;
        BestMaxDrawdown = bestMaxDrawdown;
        BestSharpe = bestSharpe;
        BestTradeCount = bestTradeCount;
        BestWinRate = bestWinRate;
        BestDroppedIntents = bestDroppedIntents;
        ErrorMessage = errorMessage;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public long Id { get; private set; }

    public string Pair { get; private set; }

    public string Timeframe { get; private set; }

    public DateTimeOffset From { get; private set; }

    public DateTimeOffset To { get; private set; }

    public decimal InitialCash { get; private set; }

    public decimal FeeBps { get; private set; }

    public decimal SlippageBps { get; private set; }

    public string StrategyName { get; private set; }

    public string? ParameterGridJson { get; private set; }

    public OptimizationMetric OptimizationMetric { get; private set; }

    public ParameterSweepRunStatus Status { get; private set; }

    public int? TotalCombinations { get; private set; }

    public int? CompletedCombinations { get; private set; }

    public string? BestParametersJson { get; private set; }

    public decimal? BestMetricValue { get; private set; }

    public decimal? BestTotalReturn { get; private set; }

    public decimal? BestMaxDrawdown { get; private set; }

    public decimal? BestSharpe { get; private set; }

    public int? BestTradeCount { get; private set; }

    public decimal? BestWinRate { get; private set; }

    public int? BestDroppedIntents { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkRunning(int totalCombinations, DateTimeOffset now)
    {
        Status = ParameterSweepRunStatus.Running;
        TotalCombinations = totalCombinations;
        CompletedCombinations = 0;
        StartedAt = now;
        UpdatedAt = now;
    }

    public void RecordProgress(int completed, DateTimeOffset now)
    {
        CompletedCombinations = completed;
        UpdatedAt = now;
    }

    public void RecordBest(string parametersJson, decimal metricValue, BacktestResult result, DateTimeOffset now)
    {
        BestParametersJson = parametersJson;
        BestMetricValue = metricValue;
        BestTotalReturn = result.TotalReturn;
        BestMaxDrawdown = result.MaxDrawdown;
        BestSharpe = result.Sharpe;
        BestTradeCount = result.TradeCount;
        BestWinRate = result.WinRate;
        BestDroppedIntents = result.DroppedIntents;
        UpdatedAt = now;
    }

    public void MarkSucceeded(DateTimeOffset now)
    {
        Status = ParameterSweepRunStatus.Succeeded;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = ParameterSweepRunStatus.Failed;
        ErrorMessage = error;
        CompletedAt = now;
        UpdatedAt = now;
    }
}
