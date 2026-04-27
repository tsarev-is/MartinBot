using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class WalkForwardTestMetricsTests
{
    private const string HourlyTimeframe = "60";

    private static readonly DateTimeOffset Origin =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static EquityPoint Ep(int hourOffset, decimal equity)
        => new(Origin.AddHours(hourOffset), equity);

    private static Fill Buy(int hourOffset, decimal price, decimal qty)
        => new(Origin.AddHours(hourOffset), OrderSide.Buy, price, qty, fee: 0m);

    private static BacktestResult Combined(IReadOnlyList<EquityPoint> curve, IReadOnlyList<Fill> fills)
        => new(initialCash: 1000m, finalEquity: curve.Count == 0 ? 1000m : curve[^1].Equity,
            totalReturn: 0m, maxDrawdown: 0m, sharpe: 0m,
            tradeCount: fills.Count, droppedIntents: 0, winRate: 0m,
            equityCurve: curve, fills: fills);

    [Test]
    public void Extract_EmptyCurve_ReturnsZeroes()
    {
        var result = WalkForwardTestMetrics.Extract(Combined(Array.Empty<EquityPoint>(), Array.Empty<Fill>()),
            Origin, HourlyTimeframe);

        Assert.That(result.TotalReturn, Is.EqualTo(0m));
        Assert.That(result.MaxDrawdown, Is.EqualTo(0m));
        Assert.That(result.Sharpe, Is.EqualTo(0m));
        Assert.That(result.TradeCount, Is.EqualTo(0));
        Assert.That(result.EquityCurve, Is.Empty);
    }

    [Test]
    public void Extract_TestFromAfterLastCandle_ReturnsZeroes()
    {
        var curve = new[] { Ep(0, 1000m), Ep(1, 1010m), Ep(2, 1020m) };
        var result = WalkForwardTestMetrics.Extract(Combined(curve, Array.Empty<Fill>()),
            Origin.AddHours(10), HourlyTimeframe);

        Assert.That(result.EquityCurve, Is.Empty);
        Assert.That(result.TotalReturn, Is.EqualTo(0m));
    }

    [Test]
    public void Extract_TestFromCoversWholeCurve_MatchesFullMetrics()
    {
        var curve = new[] { Ep(0, 1000m), Ep(1, 1010m), Ep(2, 990m), Ep(3, 1100m) };
        var result = WalkForwardTestMetrics.Extract(Combined(curve, Array.Empty<Fill>()),
            Origin, HourlyTimeframe);

        Assert.That(result.EquityCurve.Count, Is.EqualTo(4));
        Assert.That(result.TotalReturn, Is.EqualTo(0.1m).Within(0.0001m));
        Assert.That(result.MaxDrawdown, Is.EqualTo(20m / 1010m).Within(0.0001m));
    }

    [Test]
    public void Extract_ReanchorsBaselineAtTestFrom()
    {
        // equity: 1000 (train) -> 1200 (train) -> 1200 (testFrom) -> 1320 (test)
        // old formula: (1320 - 1000) / 1000 = 0.32  — включает прирост за warmup
        // новый: (1320 - 1200) / 1200 = 0.10  — только OOS
        var curve = new[]
        {
            Ep(0, 1000m), Ep(1, 1200m), Ep(2, 1200m), Ep(3, 1320m)
        };
        var testFrom = Origin.AddHours(2);

        var result = WalkForwardTestMetrics.Extract(Combined(curve, Array.Empty<Fill>()),
            testFrom, HourlyTimeframe);

        Assert.That(result.EquityCurve.Count, Is.EqualTo(2));
        Assert.That(result.TotalReturn, Is.EqualTo(0.1m).Within(0.0001m));
    }

    [Test]
    public void Extract_FiltersFillsByTimestamp()
    {
        var curve = new[] { Ep(0, 1000m), Ep(1, 1000m), Ep(2, 1000m), Ep(3, 1000m) };
        var fills = new[]
        {
            Buy(0, 100m, 1m),
            Buy(1, 101m, 1m),
            Buy(2, 102m, 1m),
            Buy(3, 103m, 1m)
        };

        var result = WalkForwardTestMetrics.Extract(Combined(curve, fills),
            Origin.AddHours(2), HourlyTimeframe);

        Assert.That(result.TradeCount, Is.EqualTo(2));
    }

    [Test]
    public void Extract_MaxDrawdownIgnoresWarmupDrawdown()
    {
        // большой warmup-DD (1000 -> 500 -> 1000), затем мелкий OOS-DD (1000 -> 950)
        var curve = new[]
        {
            Ep(0, 1000m), Ep(1, 500m), Ep(2, 1000m), Ep(3, 950m), Ep(4, 1000m)
        };
        var testFrom = Origin.AddHours(2);

        var result = WalkForwardTestMetrics.Extract(Combined(curve, Array.Empty<Fill>()),
            testFrom, HourlyTimeframe);

        Assert.That(result.MaxDrawdown, Is.EqualTo(0.05m).Within(0.0001m));
    }
}
