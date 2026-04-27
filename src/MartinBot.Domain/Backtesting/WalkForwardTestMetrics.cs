using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Extracts out-of-sample metrics from a combined [trainFrom, testTo] engine run.
/// The strategy is fed warmup candles from the train slice so indicators (EMA, RSI) are
/// primed by the time the test slice starts; metrics are then computed only on the
/// test portion of the equity curve, with the baseline equity re-anchored at testFrom.
/// See strategies-roadmap-next.md §8a.
/// </summary>
public static class WalkForwardTestMetrics
{
    public static WalkForwardTestSlice Extract(BacktestResult combined, DateTimeOffset testFrom, string timeframe)
    {
        var filteredCurve = FilterCurve(combined.EquityCurve, testFrom);
        if (filteredCurve.Count == 0)
            return new WalkForwardTestSlice(0m, 0m, 0m, 0, Array.Empty<EquityPoint>());

        var baselineEquity = filteredCurve[0].Equity;
        var filteredFills = FilterFills(combined.Fills, testFrom);
        var metrics = Metrics.Compute(filteredCurve, filteredFills, baselineEquity, timeframe);
        return new WalkForwardTestSlice(metrics.TotalReturn, metrics.MaxDrawdown, metrics.Sharpe,
            filteredFills.Count, filteredCurve);
    }

    private static IReadOnlyList<EquityPoint> FilterCurve(IReadOnlyList<EquityPoint> curve,
        DateTimeOffset testFrom)
    {
        var result = new List<EquityPoint>();
        foreach (var p in curve)
        {
            if (p.Timestamp >= testFrom)
                result.Add(p);
        }
        return result;
    }

    private static IReadOnlyList<Fill> FilterFills(IReadOnlyList<Fill> fills, DateTimeOffset testFrom)
    {
        var result = new List<Fill>();
        foreach (var f in fills)
        {
            if (f.Timestamp >= testFrom)
                result.Add(f);
        }
        return result;
    }
}

public sealed class WalkForwardTestSlice
{
    public WalkForwardTestSlice(decimal totalReturn, decimal maxDrawdown, decimal sharpe, int tradeCount,
        IReadOnlyList<EquityPoint> equityCurve)
    {
        TotalReturn = totalReturn;
        MaxDrawdown = maxDrawdown;
        Sharpe = sharpe;
        TradeCount = tradeCount;
        EquityCurve = equityCurve;
    }

    public decimal TotalReturn { get; }

    public decimal MaxDrawdown { get; }

    public decimal Sharpe { get; }

    public int TradeCount { get; }

    public IReadOnlyList<EquityPoint> EquityCurve { get; }
}
