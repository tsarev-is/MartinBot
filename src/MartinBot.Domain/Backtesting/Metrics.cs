using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Domain.Backtesting;

public static class Metrics
{
    public static ComputedMetrics Compute(IReadOnlyList<EquityPoint> curve, IReadOnlyList<Fill> fills,
        decimal initialCash, string timeframe)
    {
        if (curve.Count == 0)
            return new ComputedMetrics(0m, 0m, 0m, 0m);

        var totalReturn = initialCash == 0m ? 0m : (curve[^1].Equity - initialCash) / initialCash;
        var maxDrawdown = ComputeMaxDrawdown(curve);
        var sharpe = ComputeSharpe(curve, TimeframeConverter.PeriodsPerYear(timeframe));
        var winRate = ComputeWinRate(fills);
        return new ComputedMetrics(totalReturn, maxDrawdown, sharpe, winRate);
    }

    private static decimal ComputeMaxDrawdown(IReadOnlyList<EquityPoint> curve)
    {
        var peak = curve[0].Equity;
        var maxDd = 0m;
        foreach (var p in curve)
        {
            if (p.Equity > peak)
                peak = p.Equity;
            if (peak <= 0m)
                continue;
            var dd = (peak - p.Equity) / peak;
            if (dd > maxDd)
                maxDd = dd;
        }
        return maxDd;
    }

    private static decimal ComputeSharpe(IReadOnlyList<EquityPoint> curve, double periodsPerYear)
    {
        if (curve.Count < 2)
            return 0m;

        var returns = new List<double>(curve.Count - 1);
        for (var i = 1; i < curve.Count; i++)
        {
            var prev = (double)curve[i - 1].Equity;
            var cur = (double)curve[i].Equity;
            if (prev <= 0d)
                continue;
            returns.Add((cur - prev) / prev);
        }
        if (returns.Count < 2)
            return 0m;

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / (returns.Count - 1);
        var std = Math.Sqrt(variance);
        if (std == 0d)
            return 0m;
        return (decimal)(mean / std * Math.Sqrt(periodsPerYear));
    }

    private static decimal ComputeWinRate(IReadOnlyList<Fill> fills)
    {
        if (fills.Count == 0)
            return 0m;

        decimal cost = 0m;
        decimal position = 0m;
        int closed = 0;
        int wins = 0;
        foreach (var f in fills)
        {
            if (f.Side == OrderSide.Buy)
            {
                cost += f.Price * f.Quantity + f.Fee;
                position += f.Quantity;
                continue;
            }

            if (position <= 0m)
                continue;
            var avgCost = cost / position;
            var proceeds = f.Price * f.Quantity - f.Fee;
            closed++;
            if (proceeds > avgCost * f.Quantity)
                wins++;
            cost -= avgCost * f.Quantity;
            position -= f.Quantity;
        }
        return closed == 0 ? 0m : (decimal)wins / closed;
    }
}

public sealed class ComputedMetrics
{
    public ComputedMetrics(decimal totalReturn, decimal maxDrawdown, decimal sharpe, decimal winRate)
    {
        TotalReturn = totalReturn;
        MaxDrawdown = maxDrawdown;
        Sharpe = sharpe;
        WinRate = winRate;
    }

    public decimal TotalReturn { get; }

    public decimal MaxDrawdown { get; }

    public decimal Sharpe { get; }

    public decimal WinRate { get; }
}
