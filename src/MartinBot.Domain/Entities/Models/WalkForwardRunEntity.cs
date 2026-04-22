using MartinBot.Domain.Backtesting;

namespace MartinBot.Domain.Entities.Models;

/// <summary>
/// Record of a walk-forward validation job: the full range, the sweep grid applied on each
/// training window, and the aggregate out-of-sample metrics computed over stitched test periods.
/// Per-window details live in <see cref="WalkForwardWindowEntity"/>.
/// </summary>
public sealed class WalkForwardRunEntity
{
    public WalkForwardRunEntity(long id, string pair, string timeframe, DateTimeOffset from, DateTimeOffset to,
        decimal initialCash, decimal feeBps, decimal slippageBps, string strategyName,
        string? parameterGridJson, OptimizationMetric optimizationMetric,
        int trainDays, int testDays, int stepDays,
        WalkForwardRunStatus status, int? totalWindows, int? completedWindows,
        decimal? aggregateTotalReturn, decimal? aggregateMaxDrawdown, decimal? aggregateSharpe,
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
        TrainDays = trainDays;
        TestDays = testDays;
        StepDays = stepDays;
        Status = status;
        TotalWindows = totalWindows;
        CompletedWindows = completedWindows;
        AggregateTotalReturn = aggregateTotalReturn;
        AggregateMaxDrawdown = aggregateMaxDrawdown;
        AggregateSharpe = aggregateSharpe;
        ErrorMessage = errorMessage;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Windows = new List<WalkForwardWindowEntity>();
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

    public int TrainDays { get; private set; }

    public int TestDays { get; private set; }

    public int StepDays { get; private set; }

    public WalkForwardRunStatus Status { get; private set; }

    public int? TotalWindows { get; private set; }

    public int? CompletedWindows { get; private set; }

    public decimal? AggregateTotalReturn { get; private set; }

    public decimal? AggregateMaxDrawdown { get; private set; }

    public decimal? AggregateSharpe { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public List<WalkForwardWindowEntity> Windows { get; private set; }

    public void MarkRunning(int totalWindows, DateTimeOffset now)
    {
        Status = WalkForwardRunStatus.Running;
        TotalWindows = totalWindows;
        CompletedWindows = 0;
        StartedAt = now;
        UpdatedAt = now;
    }

    public void RecordProgress(int completed, DateTimeOffset now)
    {
        CompletedWindows = completed;
        UpdatedAt = now;
    }

    public void RecordAggregate(decimal totalReturn, decimal maxDrawdown, decimal sharpe, DateTimeOffset now)
    {
        AggregateTotalReturn = totalReturn;
        AggregateMaxDrawdown = maxDrawdown;
        AggregateSharpe = sharpe;
        UpdatedAt = now;
    }

    public void MarkSucceeded(DateTimeOffset now)
    {
        Status = WalkForwardRunStatus.Succeeded;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = WalkForwardRunStatus.Failed;
        ErrorMessage = error;
        CompletedAt = now;
        UpdatedAt = now;
    }
}
