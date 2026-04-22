using System.Globalization;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Parses EXMO candle timeframes ("1", "5", "60", "D", "W", "M") into calendar time spans
/// and into an annualization factor (periods per 365-day year). Used both by the integration
/// layer when planning candle-history chunks and by <see cref="Metrics"/> for annualized Sharpe.
/// Monthly resolution is treated as 30 days, matching EXMO's public-API convention.
/// </summary>
public static class TimeframeConverter
{
    private const double DaysPerYear = 365d;

    public static TimeSpan ToTimeSpan(string resolution)
    {
        if (int.TryParse(resolution, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) && minutes > 0)
            return TimeSpan.FromMinutes(minutes);
        return resolution.ToUpperInvariant() switch
        {
            "D" => TimeSpan.FromDays(1),
            "W" => TimeSpan.FromDays(7),
            "M" => TimeSpan.FromDays(30),
            _ => throw new ArgumentException($"Unknown timeframe resolution: {resolution}")
        };
    }

    public static double PeriodsPerYear(string resolution)
    {
        var span = ToTimeSpan(resolution);
        if (span <= TimeSpan.Zero)
            throw new ArgumentException($"Timeframe resolution must map to a positive span: {resolution}");
        return TimeSpan.FromDays(DaysPerYear).TotalSeconds / span.TotalSeconds;
    }
}
