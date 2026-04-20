using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.Strategies;
using MartinBot.Domain.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class DcaMeanReversionStrategyTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static IReadOnlyList<Candle> MakeCandles(params decimal[] closes)
    {
        var list = new List<Candle>(closes.Length);
        for (var i = 0; i < closes.Length; i++)
        {
            var open = i == 0 ? closes[0] : closes[i - 1];
            var close = closes[i];
            var high = Math.Max(open, close);
            var low = Math.Min(open, close);
            list.Add(new Candle(Origin.AddHours(i), open, high, low, close, 0m));
        }
        return list;
    }

    private static DcaMeanReversionStrategy MakeStrategy(decimal initialCash = 1_000m,
        int emaPeriod = 20, int rsiPeriod = 3, decimal entryRsi = 30m, decimal exitRsi = 50m,
        int maxTranches = 3, decimal trancheFraction = 0.25m, decimal dcaDropPct = 0.03m,
        decimal stopLossPct = 0.10m)
    {
        return new DcaMeanReversionStrategy(feeBps: 0m, slippageBps: 0m, initialCash,
            emaPeriod, rsiPeriod, entryRsi, exitRsi,
            maxTranches, trancheFraction, dcaDropPct, stopLossPct);
    }

    private static BacktestRequest MakeRequest(IReadOnlyList<Candle> candles, decimal initialCash = 1_000m)
    {
        return new BacktestRequest("BTC_USD", "h1", candles[0].Timestamp, candles[^1].Timestamp,
            initialCash, feeBps: 0m, slippageBps: 0m);
    }

    [Test]
    public void Warmup_InsufficientCandles_EmitsNothing()
    {
        var candles = MakeCandles(100m, 101m, 102m, 103m, 104m);
        var strategy = MakeStrategy(emaPeriod: 20, rsiPeriod: 3);

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, strategy);

        Assert.That(result.TradeCount, Is.EqualTo(0));
        Assert.That(result.DroppedIntents, Is.EqualTo(0));
        Assert.That(result.FinalEquity, Is.EqualTo(1_000m));
    }

    [Test]
    public void DescendingPrices_NeverEnters_BecauseCloseBelowEma()
    {
        var closes = new decimal[40];
        for (var i = 0; i < closes.Length; i++)
            closes[i] = 200m - i;
        var candles = MakeCandles(closes);
        var strategy = MakeStrategy(emaPeriod: 20, rsiPeriod: 3);

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, strategy);

        Assert.That(result.TradeCount, Is.EqualTo(0));
    }

    [Test]
    public void OversoldAboveEma_BuysOnce_ThenExitsOnRebound()
    {
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(100m + 5m * i);
        closes.Add(200m);
        closes.Add(198m);
        closes.Add(193m);
        closes.Add(185m);
        closes.Add(185m);
        closes.Add(195m);
        closes.Add(195m);

        var candles = MakeCandles(closes.ToArray());
        var strategy = MakeStrategy(emaPeriod: 20, rsiPeriod: 3, trancheFraction: 0.25m);

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, strategy);

        Assert.That(result.TradeCount, Is.EqualTo(2), "expected one buy followed by one full liquidation");
        Assert.That(result.Fills[0].Side, Is.EqualTo(OrderSide.Buy));
        Assert.That(result.Fills[0].Price, Is.EqualTo(185m));
        Assert.That(result.Fills[1].Side, Is.EqualTo(OrderSide.Sell));
        Assert.That(result.Fills[1].Price, Is.EqualTo(195m));
        Assert.That(result.FinalEquity, Is.GreaterThan(1_000m), "profitable round trip expected");
    }

    [Test]
    public void DeepDrop_AfterEntry_TriggersStopLossLiquidation()
    {
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(100m + 5m * i);
        closes.Add(200m);
        closes.Add(198m);
        closes.Add(193m);
        closes.Add(185m);
        closes.Add(185m);
        closes.Add(100m);
        closes.Add(100m);

        var candles = MakeCandles(closes.ToArray());
        var strategy = MakeStrategy(emaPeriod: 20, rsiPeriod: 3, stopLossPct: 0.10m);

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, strategy);

        Assert.That(result.Fills.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(result.Fills[0].Side, Is.EqualTo(OrderSide.Buy));
        Assert.That(result.Fills[^1].Side, Is.EqualTo(OrderSide.Sell),
            "stop-loss must liquidate the position once close falls >= stopLossPct below average entry");
        Assert.That(result.FinalEquity, Is.LessThan(1_000m));
    }
}
