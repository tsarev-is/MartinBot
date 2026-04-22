namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Ready-to-render view of a walk-forward run: per-window breakdown (with parameters and in-sample
/// vs out-of-sample metric), parameter stability stats across windows, and a rollup summary.
/// Built by <see cref="WalkForwardReportBuilder"/> from persisted entities; no data is re-run.
/// </summary>
public sealed class WalkForwardReport
{
    public WalkForwardReport(long runId, OptimizationMetric metric,
        IReadOnlyList<WalkForwardReportRow> rows,
        IReadOnlyDictionary<string, ParameterStatistics> parameterStability,
        WalkForwardReportSummary summary)
    {
        RunId = runId;
        Metric = metric;
        Rows = rows;
        ParameterStability = parameterStability;
        Summary = summary;
    }

    public long RunId { get; }

    public OptimizationMetric Metric { get; }

    public IReadOnlyList<WalkForwardReportRow> Rows { get; }

    public IReadOnlyDictionary<string, ParameterStatistics> ParameterStability { get; }

    public WalkForwardReportSummary Summary { get; }
}

public sealed class WalkForwardReportRow
{
    public WalkForwardReportRow(int windowIndex, DateTimeOffset trainFrom, DateTimeOffset trainTo,
        DateTimeOffset testFrom, DateTimeOffset testTo,
        IReadOnlyDictionary<string, decimal> bestParameters,
        decimal inSampleMetric, decimal outOfSampleMetric, decimal degradation,
        decimal outOfSampleTotalReturn, decimal outOfSampleMaxDrawdown, decimal outOfSampleSharpe,
        int outOfSampleTradeCount)
    {
        WindowIndex = windowIndex;
        TrainFrom = trainFrom;
        TrainTo = trainTo;
        TestFrom = testFrom;
        TestTo = testTo;
        BestParameters = bestParameters;
        InSampleMetric = inSampleMetric;
        OutOfSampleMetric = outOfSampleMetric;
        Degradation = degradation;
        OutOfSampleTotalReturn = outOfSampleTotalReturn;
        OutOfSampleMaxDrawdown = outOfSampleMaxDrawdown;
        OutOfSampleSharpe = outOfSampleSharpe;
        OutOfSampleTradeCount = outOfSampleTradeCount;
    }

    public int WindowIndex { get; }

    public DateTimeOffset TrainFrom { get; }

    public DateTimeOffset TrainTo { get; }

    public DateTimeOffset TestFrom { get; }

    public DateTimeOffset TestTo { get; }

    public IReadOnlyDictionary<string, decimal> BestParameters { get; }

    public decimal InSampleMetric { get; }

    public decimal OutOfSampleMetric { get; }

    public decimal Degradation { get; }

    public decimal OutOfSampleTotalReturn { get; }

    public decimal OutOfSampleMaxDrawdown { get; }

    public decimal OutOfSampleSharpe { get; }

    public int OutOfSampleTradeCount { get; }
}

public sealed class ParameterStatistics
{
    public ParameterStatistics(decimal mean, decimal stdDev, decimal min, decimal max, int uniqueValues)
    {
        Mean = mean;
        StdDev = stdDev;
        Min = min;
        Max = max;
        UniqueValues = uniqueValues;
    }

    public decimal Mean { get; }

    public decimal StdDev { get; }

    public decimal Min { get; }

    public decimal Max { get; }

    public int UniqueValues { get; }
}

public sealed class WalkForwardReportSummary
{
    public WalkForwardReportSummary(int totalWindows, decimal meanInSampleMetric,
        decimal meanOutOfSampleMetric, decimal meanDegradation, int oosWorseThanIsCount)
    {
        TotalWindows = totalWindows;
        MeanInSampleMetric = meanInSampleMetric;
        MeanOutOfSampleMetric = meanOutOfSampleMetric;
        MeanDegradation = meanDegradation;
        OosWorseThanIsCount = oosWorseThanIsCount;
    }

    public int TotalWindows { get; }

    public decimal MeanInSampleMetric { get; }

    public decimal MeanOutOfSampleMetric { get; }

    public decimal MeanDegradation { get; }

    public int OosWorseThanIsCount { get; }
}
