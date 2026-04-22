using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Entities.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class WalkForwardCsvExporterTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static WalkForwardRunEntity MakeRun(params WalkForwardWindowEntity[] windows)
    {
        var run = new WalkForwardRunEntity(
            id: 7, pair: "BTC_USD", timeframe: "60", from: Origin, to: Origin.AddDays(100),
            initialCash: 1_000m, feeBps: 0m, slippageBps: 0m, strategyName: "dca_mr",
            parameterGridJson: null, optimizationMetric: OptimizationMetric.TotalReturn,
            trainDays: 30, testDays: 10, stepDays: 10,
            status: WalkForwardRunStatus.Succeeded,
            totalWindows: windows.Length, completedWindows: windows.Length,
            aggregateTotalReturn: null, aggregateMaxDrawdown: null, aggregateSharpe: null,
            errorMessage: null, startedAt: Origin, completedAt: Origin.AddMinutes(1),
            createdAt: Origin, updatedAt: Origin.AddMinutes(1));
        foreach (var w in windows)
            run.Windows.Add(w);
        return run;
    }

    private static WalkForwardWindowEntity MakeWindow(int index, string bestJson,
        decimal inSample, decimal oosTotalReturn)
    {
        return new WalkForwardWindowEntity(
            id: 0, runId: 7, windowIndex: index,
            trainFrom: Origin.AddDays(index * 10), trainTo: Origin.AddDays(index * 10 + 30),
            testFrom: Origin.AddDays(index * 10 + 30), testTo: Origin.AddDays(index * 10 + 40),
            bestParametersJson: bestJson, inSampleMetricValue: inSample,
            outOfSampleTotalReturn: oosTotalReturn, outOfSampleMaxDrawdown: 0m,
            outOfSampleSharpe: 0m, outOfSampleTradeCount: 0,
            createdAt: Origin);
    }

    [Test]
    public void ToCsv_EmptyReport_HeaderOnly()
    {
        var csv = WalkForwardCsvExporter.ToCsv(WalkForwardReportBuilder.Build(MakeRun()));

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines, Has.Length.EqualTo(1));
        Assert.That(lines[0], Does.StartWith("windowIndex,trainFrom,trainTo"));
        Assert.That(lines[0], Does.Contain("inSampleMetric"));
    }

    [Test]
    public void ToCsv_IncludesParameterColumnsInAlphabeticOrder()
    {
        var run = MakeRun(
            MakeWindow(0, "{\"emaPeriod\":100,\"entryRsi\":25}", 0.1m, 0.05m),
            MakeWindow(1, "{\"emaPeriod\":200,\"entryRsi\":30}", 0.2m, 0.08m));
        var csv = WalkForwardCsvExporter.ToCsv(WalkForwardReportBuilder.Build(run));
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.That(lines[0], Does.Contain(",emaPeriod,"));
        Assert.That(lines[0], Does.Contain(",entryRsi,"));
        var emaIdx = lines[0].IndexOf("emaPeriod", StringComparison.Ordinal);
        var rsiIdx = lines[0].IndexOf("entryRsi", StringComparison.Ordinal);
        Assert.That(emaIdx, Is.LessThan(rsiIdx), "alphabetic ordering expected");
    }

    [Test]
    public void ToCsv_RowsHaveStableColumnCount()
    {
        var run = MakeRun(
            MakeWindow(0, "{\"emaPeriod\":100,\"entryRsi\":25}", 0.1m, 0.05m),
            MakeWindow(1, "{\"emaPeriod\":200,\"entryRsi\":30}", 0.2m, 0.08m));
        var csv = WalkForwardCsvExporter.ToCsv(WalkForwardReportBuilder.Build(run));
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var headerCols = lines[0].Split(',').Length;
        for (var i = 1; i < lines.Length; i++)
            Assert.That(lines[i].Split(',').Length, Is.EqualTo(headerCols),
                $"row {i} column count mismatch");
    }

    [Test]
    public void ToCsv_UsesInvariantCultureDecimals()
    {
        var run = MakeRun(MakeWindow(0, "{\"emaPeriod\":100.5}", 0.125m, 0.067m));
        var csv = WalkForwardCsvExporter.ToCsv(WalkForwardReportBuilder.Build(run));

        Assert.That(csv, Does.Contain("0.125"));
        Assert.That(csv, Does.Contain("0.067"));
        Assert.That(csv, Does.Not.Contain("0,125"), "decimals must use '.' separator, not ','");
    }
}
