using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class MetricsTests
{
    private static IReadOnlyList<EquityPoint> Curve(params decimal[] values)
    {
        var origin = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var list = new List<EquityPoint>(values.Length);
        for (var i = 0; i < values.Length; i++)
            list.Add(new EquityPoint(origin.AddHours(i), values[i]));
        return list;
    }

    [Test]
    public void MaxDrawdown_PeakThenHalved_Is50Percent()
    {
        var m = Metrics.Compute(Curve(100m, 120m, 60m, 90m), Array.Empty<Fill>(), 100m);

        Assert.That(m.MaxDrawdown, Is.EqualTo(0.5m).Within(0.0001m));
    }

    [Test]
    public void TotalReturn_TrackesFinalEquityMinusInitial()
    {
        var m = Metrics.Compute(Curve(100m, 120m, 150m), Array.Empty<Fill>(), 100m);

        Assert.That(m.TotalReturn, Is.EqualTo(0.5m).Within(0.0001m));
    }

    [Test]
    public void FlatEquity_HasZeroDrawdownAndZeroSharpe()
    {
        var m = Metrics.Compute(Curve(100m, 100m, 100m), Array.Empty<Fill>(), 100m);

        Assert.That(m.MaxDrawdown, Is.EqualTo(0m));
        Assert.That(m.Sharpe, Is.EqualTo(0m));
        Assert.That(m.TotalReturn, Is.EqualTo(0m));
    }

    [Test]
    public void WinRate_CountsProfitableRoundTrips()
    {
        var ts = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var fills = new[]
        {
            new Fill(ts, OrderSide.Buy, 100m, 1m, 0m),
            new Fill(ts.AddHours(1), OrderSide.Sell, 120m, 1m, 0m),
            new Fill(ts.AddHours(2), OrderSide.Buy, 100m, 1m, 0m),
            new Fill(ts.AddHours(3), OrderSide.Sell, 90m, 1m, 0m),
        };

        var m = Metrics.Compute(Curve(100m, 100m, 100m, 100m, 100m), fills, 100m);

        Assert.That(m.WinRate, Is.EqualTo(0.5m).Within(0.0001m));
    }

    [Test]
    public void EmptyCurve_ReturnsZeroEverything()
    {
        var m = Metrics.Compute(Array.Empty<EquityPoint>(), Array.Empty<Fill>(), 100m);

        Assert.That(m.TotalReturn, Is.EqualTo(0m));
        Assert.That(m.MaxDrawdown, Is.EqualTo(0m));
        Assert.That(m.Sharpe, Is.EqualTo(0m));
        Assert.That(m.WinRate, Is.EqualTo(0m));
    }
}
