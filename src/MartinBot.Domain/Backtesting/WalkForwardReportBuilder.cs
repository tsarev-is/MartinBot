using MartinBot.Domain.Entities.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Builds a <see cref="WalkForwardReport"/> from a persisted <see cref="WalkForwardRunEntity"/>.
/// Reuses <see cref="OptimizationMetricSelector"/> to derive the OOS metric in the same units
/// as the run's optimization target, so in-sample vs out-of-sample degradation is apples-to-apples.
/// </summary>
public static class WalkForwardReportBuilder
{
    public static WalkForwardReport Build(WalkForwardRunEntity run)
    {
        var sortedWindows = run.Windows.OrderBy(w => w.WindowIndex).ToList();
        var rows = new List<WalkForwardReportRow>(sortedWindows.Count);
        var valuesByKey = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
        decimal sumIs = 0m;
        decimal sumOos = 0m;
        decimal sumDegradation = 0m;
        var oosWorseCount = 0;

        foreach (var window in sortedWindows)
        {
            var parameters = StrategyParametersSerializer.Deserialize(window.BestParametersJson)
                ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in parameters)
            {
                if (!valuesByKey.TryGetValue(key, out var list))
                {
                    list = new List<decimal>();
                    valuesByKey[key] = list;
                }
                list.Add(value);
            }

            var oosMetric = OptimizationMetricSelector.Select(run.OptimizationMetric,
                window.OutOfSampleTotalReturn, window.OutOfSampleMaxDrawdown, window.OutOfSampleSharpe);
            var degradation = window.InSampleMetricValue - oosMetric;
            sumIs += window.InSampleMetricValue;
            sumOos += oosMetric;
            sumDegradation += degradation;
            if (oosMetric < window.InSampleMetricValue)
                oosWorseCount++;

            rows.Add(new WalkForwardReportRow(
                windowIndex: window.WindowIndex,
                trainFrom: window.TrainFrom, trainTo: window.TrainTo,
                testFrom: window.TestFrom, testTo: window.TestTo,
                bestParameters: parameters,
                inSampleMetric: window.InSampleMetricValue,
                outOfSampleMetric: oosMetric,
                degradation: degradation,
                outOfSampleTotalReturn: window.OutOfSampleTotalReturn,
                outOfSampleMaxDrawdown: window.OutOfSampleMaxDrawdown,
                outOfSampleSharpe: window.OutOfSampleSharpe,
                outOfSampleTradeCount: window.OutOfSampleTradeCount));
        }

        var stability = new Dictionary<string, ParameterStatistics>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in valuesByKey)
            stability[key] = Summarize(values);

        var count = rows.Count;
        var summary = count == 0
            ? new WalkForwardReportSummary(0, 0m, 0m, 0m, 0)
            : new WalkForwardReportSummary(count, sumIs / count, sumOos / count,
                sumDegradation / count, oosWorseCount);

        return new WalkForwardReport(run.Id, run.OptimizationMetric, rows, stability, summary);
    }

    private static ParameterStatistics Summarize(IReadOnlyList<decimal> values)
    {
        var count = values.Count;
        var mean = values.Sum() / count;
        decimal stdDev = 0m;
        if (count > 1)
        {
            decimal variance = 0m;
            foreach (var v in values)
                variance += (v - mean) * (v - mean);
            variance /= count - 1;
            stdDev = (decimal)Math.Sqrt((double)variance);
        }
        return new ParameterStatistics(mean, stdDev, values.Min(), values.Max(),
            uniqueValues: values.Distinct().Count());
    }
}
