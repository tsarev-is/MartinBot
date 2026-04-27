using MartinBot.Domain.Backtesting.Indicators;
using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class IndicatorsTests
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
    public void Ema_ConstantSeries_EqualsConstant()
    {
        var values = new decimal[50];
        for (var i = 0; i < values.Length; i++)
            values[i] = 100m;

        var ema = Indicators.ComputeEma(values, period: 10);

        Assert.That(ema[9], Is.EqualTo(100m));
        Assert.That(ema[^1], Is.EqualTo(100m));
    }

    [Test]
    public void Ema_TooShort_ReturnsAllZeros()
    {
        var values = new decimal[] { 1m, 2m, 3m };

        var ema = Indicators.ComputeEma(values, period: 10);

        Assert.That(ema, Has.Length.EqualTo(3));
        Assert.That(ema, Is.All.EqualTo(0m));
    }

    [Test]
    public void Ema_KnownSeries_MatchesHandComputed()
    {
        // period=3, alpha=2/4=0.5; seed = avg(1,2,3) = 2 at index 2;
        // i=3: 0.5*4 + 0.5*2 = 3
        // i=4: 0.5*5 + 0.5*3 = 4
        // i=5: 0.5*6 + 0.5*4 = 5
        var values = new decimal[] { 1m, 2m, 3m, 4m, 5m, 6m };

        var ema = Indicators.ComputeEma(values, period: 3);

        Assert.That(ema[0], Is.EqualTo(0m));
        Assert.That(ema[1], Is.EqualTo(0m));
        Assert.That(ema[2], Is.EqualTo(2m));
        Assert.That(ema[3], Is.EqualTo(3m));
        Assert.That(ema[4], Is.EqualTo(4m));
        Assert.That(ema[5], Is.EqualTo(5m));
    }

    [Test]
    public void Adx_StrongUptrend_RisesAboveTwentyFive()
    {
        var closes = new decimal[100];
        for (var i = 0; i < closes.Length; i++)
            closes[i] = 100m + i * 1m;
        var candles = CandlesFromCloses(closes);

        var adx = Indicators.ComputeAdx(candles, period: 14);

        Assert.That(adx[^1], Is.GreaterThan(25m),
            $"strong uptrend should produce ADX > 25, got {adx[^1]}");
    }

    [Test]
    public void Adx_StrongDowntrend_RisesAboveTwentyFive()
    {
        var closes = new decimal[100];
        for (var i = 0; i < closes.Length; i++)
            closes[i] = 200m - i * 1m;
        var candles = CandlesFromCloses(closes);

        var adx = Indicators.ComputeAdx(candles, period: 14);

        Assert.That(adx[^1], Is.GreaterThan(25m),
            $"strong downtrend should produce ADX > 25, got {adx[^1]}");
    }

    [Test]
    public void Adx_SidewaysNoise_StaysLow()
    {
        // Real chop: bullish and bearish bars alternate, so +DM and −DM cancel.
        // Bullish bar: open at low, close at high; high rises further than the bearish low falls.
        // Next bearish bar reverses. Highs and lows swing both directions every bar.
        var candles = new List<Candle>(120);
        for (var i = 0; i < 120; i++)
        {
            var bullish = i % 2 == 0;
            var high = bullish ? 101m : 100.5m;
            var low = bullish ? 99.5m : 99m;
            var open = bullish ? low : high;
            var close = bullish ? high : low;
            candles.Add(new Candle(Origin.AddHours(i), open, high, low, close, 0m));
        }

        var adx = Indicators.ComputeAdx(candles, period: 14);

        Assert.That(adx[^1], Is.LessThan(25m),
            $"sideways chop should keep ADX < 25, got {adx[^1]}");
    }

    [Test]
    public void Adx_TooShort_ReturnsAllZeros()
    {
        var candles = CandlesFromCloses(new decimal[] { 100m, 101m, 102m });

        var adx = Indicators.ComputeAdx(candles, period: 14);

        Assert.That(adx, Has.Length.EqualTo(3));
        Assert.That(adx, Is.All.EqualTo(0m));
    }
}
