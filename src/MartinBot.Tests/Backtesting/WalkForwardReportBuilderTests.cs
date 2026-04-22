using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Entities.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class WalkForwardReportBuilderTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static WalkForwardRunEntity MakeRun(OptimizationMetric metric, params WalkForwardWindowEntity[] windows)
    {
        var run = new WalkForwardRunEntity(
            id: 42, pair: "BTC_USD", timeframe: "60", from: Origin, to: Origin.AddDays(100),
            initialCash: 1_000m, feeBps: 0m, slippageBps: 0m, strategyName: "dca_mr",
            parameterGridJson: null, optimizationMetric: metric,
            trainDays: 30, testDays: 10, stepDays: 10,
            status: WalkForwardRunStatus.Succeeded,
            totalWindows: windows.Length, completedWindows: windows.Length,
            aggregateTotalReturn: 0m, aggregateMaxDrawdown: 0m, aggregateSharpe: 0m,
            errorMessage: null, startedAt: Origin, completedAt: Origin.AddMinutes(5),
            createdAt: Origin, updatedAt: Origin.AddMinutes(5));
        foreach (var w in windows)
            run.Windows.Add(w);
        return run;
    }

    private static WalkForwardWindowEntity MakeWindow(int index, string bestParametersJson,
        decimal inSampleMetric, decimal oosTotalReturn, decimal oosMaxDrawdown, decimal oosSharpe,
        int oosTradeCount = 0)
    {
        return new WalkForwardWindowEntity(
            id: 0, runId: 42, windowIndex: index,
            trainFrom: Origin.AddDays(index * 10), trainTo: Origin.AddDays(index * 10 + 30),
            testFrom: Origin.AddDays(index * 10 + 30), testTo: Origin.AddDays(index * 10 + 40),
            bestParametersJson: bestParametersJson, inSampleMetricValue: inSampleMetric,
            outOfSampleTotalReturn: oosTotalReturn, outOfSampleMaxDrawdown: oosMaxDrawdown,
            outOfSampleSharpe: oosSharpe, outOfSampleTradeCount: oosTradeCount,
            createdAt: Origin);
    }

    [Test]
    public void Build_NoWindows_ReturnsEmptyReport()
    {
        var run = MakeRun(OptimizationMetric.Sharpe);

        var report = WalkForwardReportBuilder.Build(run);

        Assert.That(report.Rows, Is.Empty);
        Assert.That(report.ParameterStability, Is.Empty);
        Assert.That(report.Summary.TotalWindows, Is.EqualTo(0));
        Assert.That(report.Summary.MeanInSampleMetric, Is.EqualTo(0m));
    }

    [Test]
    public void Build_ExtractsOosMetric_Sharpe()
    {
        var run = MakeRun(OptimizationMetric.Sharpe,
            MakeWindow(0, "{\"emaPeriod\":100}", inSampleMetric: 1.5m,
                oosTotalReturn: 0.1m, oosMaxDrawdown: 0.05m, oosSharpe: 1.2m));

        var report = WalkForwardReportBuilder.Build(run);

        Assert.That(report.Rows[0].OutOfSampleMetric, Is.EqualTo(1.2m));
        Assert.That(report.Rows[0].Degradation, Is.EqualTo(0.3m));
    }

    [Test]
    public void Build_ExtractsOosMetric_TotalReturn()
    {
        var run = MakeRun(OptimizationMetric.TotalReturn,
            MakeWindow(0, "{\"emaPeriod\":100}", inSampleMetric: 0.2m,
                oosTotalReturn: 0.08m, oosMaxDrawdown: 0.05m, oosSharpe: 1.2m));

        var report = WalkForwardReportBuilder.Build(run);

        Assert.That(report.Rows[0].OutOfSampleMetric, Is.EqualTo(0.08m));
    }

    [Test]
    public void Build_ExtractsOosMetric_ReturnOverMaxDrawdown()
    {
        var run = MakeRun(OptimizationMetric.ReturnOverMaxDrawdown,
            MakeWindow(0, "{\"emaPeriod\":100}", inSampleMetric: 4m,
                oosTotalReturn: 0.2m, oosMaxDrawdown: 0.1m, oosSharpe: 1m));

        var report = WalkForwardReportBuilder.Build(run);

        Assert.That(report.Rows[0].OutOfSampleMetric, Is.EqualTo(2m));
    }

    [Test]
    public void Build_ComputesParameterStability_AcrossMultipleWindows()
    {
        var run = MakeRun(OptimizationMetric.TotalReturn,
            MakeWindow(0, "{\"emaPeriod\":100,\"entryRsi\":25}", 0.1m, 0.05m, 0.02m, 1m),
            MakeWindow(1, "{\"emaPeriod\":200,\"entryRsi\":25}", 0.15m, 0.06m, 0.02m, 1m),
            MakeWindow(2, "{\"emaPeriod\":150,\"entryRsi\":30}", 0.12m, 0.04m, 0.02m, 1m));

        var report = WalkForwardReportBuilder.Build(run);

        Assert.That(report.ParameterStability.Keys, Is.EquivalentTo(new[] { "emaPeriod", "entryRsi" }));
        var emaStats = report.ParameterStability["emaPeriod"];
        Assert.That(emaStats.Mean, Is.EqualTo(150m));
        Assert.That(emaStats.Min, Is.EqualTo(100m));
        Assert.That(emaStats.Max, Is.EqualTo(200m));
        Assert.That(emaStats.UniqueValues, Is.EqualTo(3));
        Assert.That(emaStats.StdDev, Is.GreaterThan(0m));

        var rsiStats = report.ParameterStability["entryRsi"];
        Assert.That(rsiStats.UniqueValues, Is.EqualTo(2));
    }

    [Test]
    public void Build_CountsOosWorseThanIs()
    {
        var run = MakeRun(OptimizationMetric.Sharpe,
            MakeWindow(0, "{}", inSampleMetric: 1.5m, oosTotalReturn: 0m, oosMaxDrawdown: 0m, oosSharpe: 0.9m),
            MakeWindow(1, "{}", inSampleMetric: 1.0m, oosTotalReturn: 0m, oosMaxDrawdown: 0m, oosSharpe: 1.2m),
            MakeWindow(2, "{}", inSampleMetric: 2.0m, oosTotalReturn: 0m, oosMaxDrawdown: 0m, oosSharpe: 0.5m));

        var report = WalkForwardReportBuilder.Build(run);

        Assert.That(report.Summary.OosWorseThanIsCount, Is.EqualTo(2));
    }

    [Test]
    public void Build_SummaryMeans_MatchRows()
    {
        var run = MakeRun(OptimizationMetric.Sharpe,
            MakeWindow(0, "{}", inSampleMetric: 1m, oosTotalReturn: 0m, oosMaxDrawdown: 0m, oosSharpe: 0.5m),
            MakeWindow(1, "{}", inSampleMetric: 2m, oosTotalReturn: 0m, oosMaxDrawdown: 0m, oosSharpe: 1.5m));

        var report = WalkForwardReportBuilder.Build(run);

        Assert.That(report.Summary.MeanInSampleMetric, Is.EqualTo(1.5m));
        Assert.That(report.Summary.MeanOutOfSampleMetric, Is.EqualTo(1.0m));
        Assert.That(report.Summary.MeanDegradation, Is.EqualTo(0.5m));
    }
}
