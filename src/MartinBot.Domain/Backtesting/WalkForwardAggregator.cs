using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Stitches per-window test equity curves into a single compounded out-of-sample curve and
/// computes aggregate TotalReturn / MaxDrawdown / Sharpe via <see cref="Metrics.Compute"/>.
/// Each window starts with the same <paramref name="initialCash"/> (fresh strategy per test),
/// so its per-period return stream is extracted and then compounded into a continuous curve
/// as if the windows traded in sequence without interruption.
/// </summary>
public static class WalkForwardAggregator
{
    public static AggregatedMetrics Aggregate(IEnumerable<IReadOnlyList<EquityPoint>> perWindowCurves,
        decimal initialCash, string timeframe)
    {
        var stitched = new List<EquityPoint>();
        var equity = initialCash;
        stitched.Add(new EquityPoint(DateTimeOffset.MinValue, equity));

        foreach (var curve in perWindowCurves)
        {
            for (var i = 1; i < curve.Count; i++)
            {
                var prev = curve[i - 1].Equity;
                var cur = curve[i].Equity;
                if (prev <= 0m)
                    continue;
                var r = (cur - prev) / prev;
                equity *= 1m + r;
                stitched.Add(new EquityPoint(curve[i].Timestamp, equity));
            }
        }

        if (stitched.Count < 2)
            return new AggregatedMetrics(0m, 0m, 0m);

        stitched[0] = new EquityPoint(stitched[1].Timestamp, initialCash);
        var metrics = Metrics.Compute(stitched, Array.Empty<Fill>(), initialCash, timeframe);
        return new AggregatedMetrics(metrics.TotalReturn, metrics.MaxDrawdown, metrics.Sharpe);
    }
}

public sealed class AggregatedMetrics
{
    public AggregatedMetrics(decimal totalReturn, decimal maxDrawdown, decimal sharpe)
    {
        TotalReturn = totalReturn;
        MaxDrawdown = maxDrawdown;
        Sharpe = sharpe;
    }

    public decimal TotalReturn { get; }

    public decimal MaxDrawdown { get; }

    public decimal Sharpe { get; }
}
