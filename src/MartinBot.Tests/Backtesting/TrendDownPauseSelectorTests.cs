using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.RegimeSelector;

namespace MartinBot.Tests.Backtesting;

public sealed class TrendDownPauseSelectorTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static IReadOnlyList<Candle> CandlesFromCloses(IReadOnlyList<decimal> closes)
    {
        var list = new List<Candle>(closes.Count);
        for (var i = 0; i < closes.Count; i++)
        {
            var open = i == 0 ? closes[0] : closes[i - 1];
            var close = closes[i];
            var high = Math.Max(open, close);
            var low = Math.Min(open, close);
            list.Add(new Candle(Origin.AddHours(i), open, high, low, close, 0m));
        }
        return list;
    }

    [Test]
    public void Decide_InsufficientHistory_ReturnsActive()
    {
        var selector = new TrendDownPauseSelector();
        var candles = CandlesFromCloses(new decimal[] { 100m, 99m, 98m });

        var decision = selector.Decide(candles);

        Assert.That(decision.Regime, Is.EqualTo(Regime.InsufficientHistory));
        Assert.That(decision.ShouldPause, Is.False,
            "active-on-uncertainty: a config typo or short slice must not silently sideline trading");
    }

    [Test]
    public void Decide_BearTrend_ReturnsTrendDownAndPauses()
    {
        // 250 candles of monotone fall — EMA50 < EMA200 and ADX rises sharply.
        var closes = new decimal[250];
        for (var i = 0; i < closes.Length; i++)
            closes[i] = 200m - i * 0.5m;
        var candles = CandlesFromCloses(closes);

        var selector = new TrendDownPauseSelector();
        var decision = selector.Decide(candles);

        Assert.That(decision.Regime, Is.EqualTo(Regime.TrendDown));
        Assert.That(decision.ShouldPause, Is.True);
        Assert.That(decision.Reason, Does.Contain("TrendDown"));
        Assert.That(decision.Reason, Does.Contain("EMA50"));
        Assert.That(decision.Reason, Does.Contain("ADX14"));
    }

    [Test]
    public void Decide_BullTrend_ReturnsActive()
    {
        // 250 candles of monotone rise — EMA50 > EMA200, ADX high but EMA condition fails.
        var closes = new decimal[250];
        for (var i = 0; i < closes.Length; i++)
            closes[i] = 100m + i * 0.5m;
        var candles = CandlesFromCloses(closes);

        var selector = new TrendDownPauseSelector();
        var decision = selector.Decide(candles);

        Assert.That(decision.Regime, Is.EqualTo(Regime.Active));
        Assert.That(decision.ShouldPause, Is.False);
    }

    [Test]
    public void Decide_BearEmaButFlatAdx_ReturnsActive()
    {
        // First 50 candles falling (sets EMA50 < EMA200), then 200 candles of real chop with
        // bidirectional bar shapes so Wilder-smoothed +DM/−DM decay to balance and ADX falls
        // below 25. EMA50 stays below EMA200 long after the fall, so the test isolates ADX.
        var candles = new List<Candle>(250);
        for (var i = 0; i < 50; i++)
        {
            var open = i == 0 ? 200m : 200m - (i - 1) * 1m;
            var close = 200m - i * 1m;
            var high = Math.Max(open, close);
            var low = Math.Min(open, close);
            candles.Add(new Candle(Origin.AddHours(i), open, high, low, close, 0m));
        }
        for (var i = 50; i < 250; i++)
        {
            var bullish = i % 2 == 0;
            var high = bullish ? 151m : 150.5m;
            var low = bullish ? 149.5m : 149m;
            var open = bullish ? low : high;
            var close = bullish ? high : low;
            candles.Add(new Candle(Origin.AddHours(i), open, high, low, close, 0m));
        }

        var selector = new TrendDownPauseSelector();
        var decision = selector.Decide(candles);

        Assert.That(decision.Regime, Is.EqualTo(Regime.Active),
            "weak/decayed downtrend (ADX < 25) must not pause: chop is not bear");
        Assert.That(decision.ShouldPause, Is.False);
    }

    [Test]
    public void Constructor_InvalidPeriods_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrendDownPauseSelector(emaShortPeriod: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrendDownPauseSelector(emaShortPeriod: 50, emaLongPeriod: 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrendDownPauseSelector(adxPeriod: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrendDownPauseSelector(adxThreshold: 0m));
    }
}
