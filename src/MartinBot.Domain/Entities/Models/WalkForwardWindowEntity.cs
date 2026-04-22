namespace MartinBot.Domain.Entities.Models;

/// <summary>
/// One train/test split within a walk-forward run: the sweep winner on the training slice plus
/// the out-of-sample metrics measured on the test slice. Written once per window, never mutated.
/// </summary>
public sealed class WalkForwardWindowEntity
{
    public WalkForwardWindowEntity(long id, long runId, int windowIndex,
        DateTimeOffset trainFrom, DateTimeOffset trainTo, DateTimeOffset testFrom, DateTimeOffset testTo,
        string bestParametersJson, decimal inSampleMetricValue,
        decimal outOfSampleTotalReturn, decimal outOfSampleMaxDrawdown, decimal outOfSampleSharpe,
        int outOfSampleTradeCount, DateTimeOffset createdAt)
    {
        Id = id;
        RunId = runId;
        WindowIndex = windowIndex;
        TrainFrom = trainFrom;
        TrainTo = trainTo;
        TestFrom = testFrom;
        TestTo = testTo;
        BestParametersJson = bestParametersJson;
        InSampleMetricValue = inSampleMetricValue;
        OutOfSampleTotalReturn = outOfSampleTotalReturn;
        OutOfSampleMaxDrawdown = outOfSampleMaxDrawdown;
        OutOfSampleSharpe = outOfSampleSharpe;
        OutOfSampleTradeCount = outOfSampleTradeCount;
        CreatedAt = createdAt;
    }

    public long Id { get; private set; }

    public long RunId { get; private set; }

    public int WindowIndex { get; private set; }

    public DateTimeOffset TrainFrom { get; private set; }

    public DateTimeOffset TrainTo { get; private set; }

    public DateTimeOffset TestFrom { get; private set; }

    public DateTimeOffset TestTo { get; private set; }

    public string BestParametersJson { get; private set; }

    public decimal InSampleMetricValue { get; private set; }

    public decimal OutOfSampleTotalReturn { get; private set; }

    public decimal OutOfSampleMaxDrawdown { get; private set; }

    public decimal OutOfSampleSharpe { get; private set; }

    public int OutOfSampleTradeCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
