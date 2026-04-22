using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class WalkForwardAggregatorTests
{
    private const string HourlyTimeframe = "60";

    private static IReadOnlyList<EquityPoint> Curve(DateTimeOffset origin, params decimal[] values)
    {
        var list = new List<EquityPoint>(values.Length);
        for (var i = 0; i < values.Length; i++)
            list.Add(new EquityPoint(origin.AddHours(i), values[i]));
        return list;
    }

    [Test]
    public void Aggregate_NoWindows_ReturnsZeroes()
    {
        var result = WalkForwardAggregator.Aggregate(
            Enumerable.Empty<IReadOnlyList<EquityPoint>>(), 1_000m, HourlyTimeframe);

        Assert.That(result.TotalReturn, Is.EqualTo(0m));
        Assert.That(result.MaxDrawdown, Is.EqualTo(0m));
        Assert.That(result.Sharpe, Is.EqualTo(0m));
    }

    [Test]
    public void Aggregate_FlatSingleWindow_ZeroMetrics()
    {
        var origin = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var curves = new[] { Curve(origin, 1_000m, 1_000m, 1_000m, 1_000m) };

        var result = WalkForwardAggregator.Aggregate(curves, 1_000m, HourlyTimeframe);

        Assert.That(result.TotalReturn, Is.EqualTo(0m));
        Assert.That(result.MaxDrawdown, Is.EqualTo(0m));
        Assert.That(result.Sharpe, Is.EqualTo(0m));
    }

    [Test]
    public void Aggregate_TwoWindows_CompoundsReturns()
    {
        var origin = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var window1 = Curve(origin, 100m, 110m);
        var window2 = Curve(origin.AddDays(1), 100m, 120m);

        var result = WalkForwardAggregator.Aggregate(new[] { window1, window2 }, 1_000m, HourlyTimeframe);

        var expectedReturn = (1m + 0.1m) * (1m + 0.2m) - 1m;
        Assert.That(result.TotalReturn, Is.EqualTo(expectedReturn).Within(0.0001m));
    }
}
