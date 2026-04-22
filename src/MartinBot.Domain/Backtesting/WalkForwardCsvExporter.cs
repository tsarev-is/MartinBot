using System.Globalization;
using System.Text;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Serializes a <see cref="WalkForwardReport"/> into a flat CSV (one row per window) for manual
/// analysis in spreadsheets. Parameter columns come from <see cref="WalkForwardReport.ParameterStability"/>
/// (stable, alphabetic order) so every row has the same shape even if a window's JSON has more
/// keys than another. Decimals are invariant-culture with '.' separator; fields are unquoted
/// because no value contains a comma (timestamps are ISO-8601).
/// </summary>
public static class WalkForwardCsvExporter
{
    public static string ToCsv(WalkForwardReport report)
    {
        var parameterKeys = report.ParameterStability.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var sb = new StringBuilder();

        sb.Append("windowIndex,trainFrom,trainTo,testFrom,testTo");
        foreach (var key in parameterKeys)
            sb.Append(',').Append(key);
        sb.AppendLine(",inSampleMetric,outOfSampleMetric,degradation,oosTotalReturn,oosMaxDrawdown,oosSharpe,oosTradeCount");

        foreach (var row in report.Rows)
        {
            sb.Append(row.WindowIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.TrainFrom.ToString("o", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.TrainTo.ToString("o", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.TestFrom.ToString("o", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.TestTo.ToString("o", CultureInfo.InvariantCulture));
            foreach (var key in parameterKeys)
            {
                sb.Append(',');
                if (row.BestParameters.TryGetValue(key, out var value))
                    sb.Append(value.ToString(CultureInfo.InvariantCulture));
            }
            sb.Append(',').Append(row.InSampleMetric.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(row.OutOfSampleMetric.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(row.Degradation.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(row.OutOfSampleTotalReturn.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(row.OutOfSampleMaxDrawdown.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(row.OutOfSampleSharpe.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(row.OutOfSampleTradeCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        return sb.ToString();
    }
}
