using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.Strategies;
using MartinBot.Domain.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class GridStrategyTests
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

    private static IReadOnlyList<Candle> MakeCandlesOhlc(params (decimal o, decimal h, decimal l, decimal c)[] bars)
    {
        var list = new List<Candle>(bars.Length);
        for (var i = 0; i < bars.Length; i++)
            list.Add(new Candle(Origin.AddHours(i), bars[i].o, bars[i].h, bars[i].l, bars[i].c, 0m));
        return list;
    }

    private static GridStrategy MakeStrategy(decimal initialCash = 10_000m,
        int channelLookback = 20, int gridLevels = 5, decimal gridBudgetFraction = 0.5m,
        decimal invalidationPct = 0.05m, decimal lowQuantile = 0.10m, decimal highQuantile = 0.90m,
        decimal feeBps = 0m, decimal slippageBps = 0m, int cooldownCandles = 0)
    {
        return new GridStrategy(feeBps, slippageBps, initialCash,
            channelLookback, gridLevels, gridBudgetFraction, invalidationPct,
            lowQuantile, highQuantile, cooldownCandles);
    }

    private static BacktestRequest MakeRequest(IReadOnlyList<Candle> candles, decimal initialCash = 10_000m,
        decimal feeBps = 0m, decimal slippageBps = 0m)
    {
        return new BacktestRequest("BTC_USD", "60", candles[0].Timestamp, candles[^1].Timestamp,
            initialCash, feeBps, slippageBps);
    }

    [Test]
    public void Warmup_FewerThanLookback_EmitsNothing()
    {
        var candles = MakeCandles(100m, 101m, 99m, 100m, 102m);
        var strategy = MakeStrategy(channelLookback: 20);

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, strategy);

        Assert.That(result.TradeCount, Is.EqualTo(0));
        Assert.That(result.DroppedIntents, Is.EqualTo(0));
        Assert.That(result.FinalEquity, Is.EqualTo(10_000m));
    }

    [Test]
    public void Initialization_PlacesBuyLimitsBelowCurrentPrice()
    {
        // 20 warmup closes between 90 and 110. Quantile(0.10) ~= 92, Quantile(0.90) ~= 108.
        // After warmup at the 20th candle, current price = 110 → all 5 levels < 110 → 5 buys queued.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i);
        var candles = MakeCandles(closes.ToArray());

        IPortfolioView? viewAtEnd = null;
        var probe = new ProbeStrategy(MakeStrategy(channelLookback: 20, gridLevels: 5),
            (c, p) => { viewAtEnd = p; });

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, probe);

        Assert.That(viewAtEnd, Is.Not.Null);
        var openLimits = viewAtEnd!.OpenLimitOrders;
        Assert.That(openLimits.Count, Is.GreaterThan(0));
        Assert.That(openLimits.Count, Is.LessThanOrEqualTo(5));
        foreach (var intent in openLimits)
        {
            Assert.That(intent.Side, Is.EqualTo(OrderSide.Buy));
            Assert.That(intent.LimitPrice, Is.Not.Null);
            Assert.That(intent.LimitPrice!.Value, Is.LessThan(candles[^1].Close));
        }
        Assert.That(result.TradeCount, Is.EqualTo(0), "no fills since all levels are below the touched range");
    }

    [Test]
    public void BuyFill_EmitsPairedSell_OneStepAbove()
    {
        // Warmup builds channel ~[91, 109] over 20 candles between 90 and 110.
        // Add a candle that drops to 92 to fill the lowest buy(s).
        // Then a flat candle so we can observe the strategy's emission.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i);
        // candle 20: large red bar engulfing only the lowest level near range_low
        closes.Add(91m);
        closes.Add(96m);
        var candles = MakeCandles(closes.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5, initialCash: 10_000m);
        var result = new BacktestEngine().Run(MakeRequest(candles), candles, strategy);

        Assert.That(result.TradeCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(result.Fills[0].Side, Is.EqualTo(OrderSide.Buy));
    }

    [Test]
    public void OscillatingWithinChannel_GeneratesBuySellPairs_PositiveReturn()
    {
        // 20 warmup candles between 95 and 105 → range ~[95.9, 104.1], 5 levels at ~95.9, 97.95, 100, 102.05, 104.1.
        // Then oscillate: 95-105-95-105 within channel — should fill several pairs.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(95m + (i % 11));
        for (var i = 0; i < 30; i++)
            closes.Add(i % 2 == 0 ? 95m : 105m);
        var candles = MakeCandles(closes.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5, initialCash: 10_000m,
            invalidationPct: 0.10m);
        var result = new BacktestEngine().Run(MakeRequest(candles), candles, strategy);

        Assert.That(result.TradeCount, Is.GreaterThan(2),
            "oscillating prices should fill at least one buy/sell pair");
        Assert.That(result.FinalEquity, Is.GreaterThanOrEqualTo(10_000m),
            "with zero fees and uniform oscillation, equity must not decrease");
    }

    [Test]
    public void ProfitabilityGate_NarrowChannel_PreventsAnyEmissions()
    {
        // Tiny channel: prices 99.99..100.01 → step is microscopic, fee=30bps → gate fails.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(100m + (i % 3) * 0.005m);
        var candles = MakeCandles(closes.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5, feeBps: 30m);
        var result = new BacktestEngine().Run(MakeRequest(candles, feeBps: 30m), candles, strategy);

        Assert.That(result.TradeCount, Is.EqualTo(0));
        Assert.That(result.DroppedIntents, Is.EqualTo(0));
        Assert.That(result.FinalEquity, Is.EqualTo(10_000m));
    }

    [Test]
    public void Invalidation_DownBreak_ClosesPositionAndCancelsLimits()
    {
        // Warmup [90..109], range ~[91, 108]. Then drop hard to 80 → close < range_low × 0.95 = 86.45.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i);
        closes.Add(95m); // first post-warmup candle: fill some buys via [low=90, high=95]? No, open=109, close=95 → low=95, high=109.
        // Use OHLC variant to make the down-bar engulf low levels first.
        var ohlc = new List<(decimal o, decimal h, decimal l, decimal c)>();
        for (var i = 0; i < closes.Count; i++)
        {
            var open = i == 0 ? closes[0] : closes[i - 1];
            var close = closes[i];
            ohlc.Add((open, Math.Max(open, close), Math.Min(open, close), close));
        }
        // candle 21: hard down to 80 — engulfs all buy levels, then closes below invalidation
        ohlc.Add((95m, 95m, 80m, 80m));
        ohlc.Add((80m, 80m, 80m, 80m)); // observation candle
        var candles = MakeCandlesOhlc(ohlc.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5, initialCash: 10_000m,
            invalidationPct: 0.05m);

        IPortfolioView? finalView = null;
        var probe = new ProbeStrategy(strategy, (c, p) =>
        {
            if (c.Timestamp == candles[^1].Timestamp)
                finalView = p;
        });

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, probe);

        Assert.That(finalView, Is.Not.Null);
        Assert.That(finalView!.OpenLimitOrders, Is.Empty,
            "all open limits must be cancelled on invalidation");
        Assert.That(finalView.Position, Is.EqualTo(0m),
            "position must be liquidated by market sell after invalidation");
    }

    [Test]
    public void Invalidation_UpBreak_ClosesPositionAndCancelsLimits()
    {
        // Warmup [90..109]. Range ~[91, 108]. Push close above 108 × 1.05 = 113.4 → 120.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i);
        closes.Add(120m);
        closes.Add(120m); // observation
        var candles = MakeCandles(closes.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5, initialCash: 10_000m,
            invalidationPct: 0.05m);

        IPortfolioView? finalView = null;
        var probe = new ProbeStrategy(strategy, (c, p) =>
        {
            if (c.Timestamp == candles[^1].Timestamp)
                finalView = p;
        });

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, probe);

        Assert.That(finalView, Is.Not.Null);
        Assert.That(finalView!.OpenLimitOrders, Is.Empty);
        Assert.That(finalView.Position, Is.EqualTo(0m));
    }

    [Test]
    public void Invalidation_NoNewOrdersDuringCooldown_OnSubsequentBreachCandles()
    {
        // V2: invalidation is transient. With cooldownCandles >= post-breach window length,
        // additional below-channel candles must not produce more orders (re-warmup hasn't
        // begun yet). Tests cooldown phase, not absorbing termination.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i);
        closes.Add(80m);
        closes.Add(75m);
        closes.Add(70m);
        closes.Add(65m);
        var candles = MakeCandles(closes.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5, invalidationPct: 0.05m,
            cooldownCandles: 10);
        var result = new BacktestEngine().Run(MakeRequest(candles), candles, strategy);

        Assert.That(result.DroppedIntents, Is.EqualTo(0),
            "cooldown phase must not emit new orders that get dropped");
    }

    [Test]
    public void ReGrid_AfterCooldownAndRewarmup_PlacesFreshChannelInPostBreachRegime()
    {
        // V2 contract: invalidation is transient. After cooldownCandles + channelLookback
        // candles in a post-breach regime, the strategy re-initializes with a channel
        // computed from the post-breach closes only — not the pre-breach ones.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i); // warmup channel ~[91, 108]
        closes.Add(80m); // breach down (80 < 91 × 0.95 = 86.45)
        closes.Add(70m);
        closes.Add(70m);
        closes.Add(70m); // 3 cooldown candles
        for (var i = 0; i < 20; i++)
            closes.Add(60m + i); // fresh re-warmup; new channel ~[62, 77]
        var candles = MakeCandles(closes.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5,
            invalidationPct: 0.05m, cooldownCandles: 3);

        IPortfolioView? finalView = null;
        var probe = new ProbeStrategy(strategy, (c, p) =>
        {
            if (c.Timestamp == candles[^1].Timestamp)
                finalView = p;
        });

        new BacktestEngine().Run(MakeRequest(candles), candles, probe);

        Assert.That(finalView, Is.Not.Null);
        var openBuys = finalView!.OpenLimitOrders.Where(i => i.Side == OrderSide.Buy).ToList();
        Assert.That(openBuys, Is.Not.Empty,
            "after cooldown + re-warmup, fresh channel must place new buy limits");
        Assert.That(openBuys.Max(b => b.LimitPrice!.Value), Is.LessThan(85m),
            "new channel reflects post-breach regime [60..79], not pre-breach [90..109]");
    }

    [Test]
    public void Cooldown_AfterInvalidation_KeepsLimitBookEmptyForCooldownCandles()
    {
        // During cooldown, no orders may be emitted regardless of price action.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i);
        closes.Add(80m); // breach
        closes.Add(70m);
        closes.Add(85m);
        closes.Add(75m);
        closes.Add(72m);
        closes.Add(78m); // 5 cooldown candles, oscillating but channel not yet rebuilt
        var candles = MakeCandles(closes.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5,
            invalidationPct: 0.05m, cooldownCandles: 5);

        IPortfolioView? finalView = null;
        var probe = new ProbeStrategy(strategy, (c, p) =>
        {
            if (c.Timestamp == candles[^1].Timestamp)
                finalView = p;
        });

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, probe);

        Assert.That(finalView, Is.Not.Null);
        Assert.That(finalView!.OpenLimitOrders, Is.Empty,
            "cooldown must hold the open-limit book empty");
        Assert.That(result.DroppedIntents, Is.EqualTo(0));
    }

    [Test]
    public void ReGrid_ZeroCooldown_RewarmupStartsImmediatelyAfterInvalidation()
    {
        // Edge case: cooldownCandles=0 means re-warmup begins on the very next candle
        // after the breach. After channelLookback fresh closes, init succeeds.
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i);
        closes.Add(80m); // breach
        for (var i = 0; i < 20; i++)
            closes.Add(60m + i); // fresh post-breach data feeds re-warmup directly
        var candles = MakeCandles(closes.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5,
            invalidationPct: 0.05m, cooldownCandles: 0);

        IPortfolioView? finalView = null;
        var probe = new ProbeStrategy(strategy, (c, p) =>
        {
            if (c.Timestamp == candles[^1].Timestamp)
                finalView = p;
        });

        new BacktestEngine().Run(MakeRequest(candles), candles, probe);

        Assert.That(finalView, Is.Not.Null);
        var openBuys = finalView!.OpenLimitOrders.Where(i => i.Side == OrderSide.Buy).ToList();
        Assert.That(openBuys, Is.Not.Empty);
        Assert.That(openBuys.Max(b => b.LimitPrice!.Value), Is.LessThan(85m));
    }

    [Test]
    public void BigBarEngulfsMultipleBuys_AllFill_Then_OnNextCandle_PairedSellsEmitted()
    {
        // Warmup [90..109], range ~[91, 108]. Five levels at ~91, 95.25, 99.5, 103.75, 108.
        // Then a giant red bar with low=88: engulfs all sub-current-price levels (which were
        // placed at init-time, so 4 buys are open under initial close=109).
        var closes = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closes.Add(90m + i);
        var ohlc = new List<(decimal o, decimal h, decimal l, decimal c)>();
        for (var i = 0; i < closes.Count; i++)
        {
            var open = i == 0 ? closes[0] : closes[i - 1];
            var close = closes[i];
            ohlc.Add((open, Math.Max(open, close), Math.Min(open, close), close));
        }
        // post-warmup giant red bar that engulfs all buys but stays above invalidation (range_low=91, invalidation@86.45)
        ohlc.Add((109m, 109m, 88m, 92m));
        ohlc.Add((92m, 95m, 92m, 95m)); // observation candle
        var candles = MakeCandlesOhlc(ohlc.ToArray());

        var strategy = MakeStrategy(channelLookback: 20, gridLevels: 5, initialCash: 10_000m,
            invalidationPct: 0.05m);

        IPortfolioView? viewAtObservation = null;
        var probe = new ProbeStrategy(strategy, (c, p) =>
        {
            if (c.Timestamp == candles[^1].Timestamp)
                viewAtObservation = p;
        });

        var result = new BacktestEngine().Run(MakeRequest(candles), candles, probe);

        Assert.That(result.TradeCount, Is.GreaterThan(1),
            "multiple buys should fill on the engulfing bar");
        Assert.That(viewAtObservation, Is.Not.Null);
        // After the engulfing bar, the strategy should have emitted paired sells.
        // Final candle observation: the open-limit book contains those sells.
        var sells = viewAtObservation!.OpenLimitOrders.Count(i => i.Side == OrderSide.Sell);
        Assert.That(sells, Is.GreaterThan(0), "paired sells must have been emitted after the buy fills");
    }

    [Test]
    public void FreshInstance_RecomputesChannel_DoesNotShareState()
    {
        // Run two engines with the same strategy class but different price ranges;
        // the second instance must compute its own channel, not inherit from the first.
        var closesA = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closesA.Add(90m + i);
        var candlesA = MakeCandles(closesA.ToArray());

        var closesB = new List<decimal>();
        for (var i = 0; i < 20; i++)
            closesB.Add(990m + i);
        var candlesB = MakeCandles(closesB.ToArray());

        IPortfolioView? viewA = null;
        IPortfolioView? viewB = null;
        var probeA = new ProbeStrategy(MakeStrategy(channelLookback: 20, gridLevels: 5),
            (c, p) => viewA = p);
        var probeB = new ProbeStrategy(MakeStrategy(channelLookback: 20, gridLevels: 5),
            (c, p) => viewB = p);

        new BacktestEngine().Run(MakeRequest(candlesA), candlesA, probeA);
        new BacktestEngine().Run(MakeRequest(candlesB), candlesB, probeB);

        Assert.That(viewA, Is.Not.Null);
        Assert.That(viewB, Is.Not.Null);
        var levelsA = viewA!.OpenLimitOrders.Select(i => i.LimitPrice!.Value).Min();
        var levelsB = viewB!.OpenLimitOrders.Select(i => i.LimitPrice!.Value).Min();
        Assert.That(levelsA, Is.LessThan(200m));
        Assert.That(levelsB, Is.GreaterThan(900m), "second instance's channel must reflect its own warmup");
    }

    /// <summary>
    /// Wraps another strategy and exposes a callback that captures the IPortfolioView
    /// at each candle so tests can inspect post-OnCandle state.
    /// </summary>
    private sealed class ProbeStrategy : IStrategy
    {
        private readonly IStrategy _inner;
        private readonly Action<Candle, IPortfolioView> _probe;

        public ProbeStrategy(IStrategy inner, Action<Candle, IPortfolioView> probe)
        {
            _inner = inner;
            _probe = probe;
        }

        public IEnumerable<OrderIntent> OnCandle(Candle candle, IPortfolioView portfolio)
        {
            foreach (var intent in _inner.OnCandle(candle, portfolio))
                yield return intent;
            _probe(candle, portfolio);
        }
    }
}
