using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.Strategies;
using MartinBot.Domain.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class BacktestEngineTests
{
    private static IReadOnlyList<Candle> MakeCandles(params decimal[] closes)
    {
        var origin = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var list = new List<Candle>(closes.Length);
        var prevClose = closes[0];
        for (var i = 0; i < closes.Length; i++)
        {
            var open = i == 0 ? closes[0] : prevClose;
            var close = closes[i];
            var high = Math.Max(open, close);
            var low = Math.Min(open, close);
            list.Add(new Candle(origin.AddHours(i), open, high, low, close, 0m));
            prevClose = close;
        }
        return list;
    }

    [Test]
    public void BuyAndHold_NoFeeNoSlip_FullyInvested_ReturnsPriceRatio()
    {
        var candles = MakeCandles(100m, 120m, 110m, 150m);
        var request = new BacktestRequest("BTC_USD", "h1", candles[0].Timestamp, candles[^1].Timestamp,
            initialCash: 1_000m, feeBps: 0m, slippageBps: 0m);

        var result = new BacktestEngine().Run(request, candles, new BuyAndHoldStrategy(0m, 0m));

        Assert.That(result.TradeCount, Is.EqualTo(1));
        Assert.That(result.DroppedIntents, Is.EqualTo(0));
        Assert.That(result.FinalEquity, Is.EqualTo(1_500m).Within(0.01m));
        Assert.That(result.TotalReturn, Is.EqualTo(0.5m).Within(0.0001m));
    }

    [Test]
    public void LimitBuy_FillsWhenRangeTouches_DeductsCashAndAddsPosition()
    {
        var ts = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var candles = new List<Candle>
        {
            new(ts, 100m, 100m, 100m, 100m, 0m),
            new(ts.AddHours(1), 99m, 101m, 95m, 100m, 0m),
        };

        var strategy = new SingleLimitStrategy(new OrderIntent(OrderSide.Buy, 1m, limitPrice: 98m));
        var request = new BacktestRequest("BTC_USD", "h1", ts, ts.AddHours(1),
            initialCash: 500m, feeBps: 0m, slippageBps: 0m);

        var result = new BacktestEngine().Run(request, candles, strategy);

        Assert.That(result.TradeCount, Is.EqualTo(1));
        Assert.That(result.Fills[0].Price, Is.EqualTo(98m));
        Assert.That(result.FinalEquity, Is.EqualTo(500m - 98m + 100m).Within(0.01m));
    }

    [Test]
    public void NoCandles_ReturnsInitialCash_NoTrades()
    {
        var request = new BacktestRequest("BTC_USD", "h1", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch,
            initialCash: 1_000m, feeBps: 30m, slippageBps: 20m);

        var result = new BacktestEngine().Run(request, Array.Empty<Candle>(), new BuyAndHoldStrategy(30m, 20m));

        Assert.That(result.TradeCount, Is.EqualTo(0));
        Assert.That(result.FinalEquity, Is.EqualTo(1_000m));
        Assert.That(result.TotalReturn, Is.EqualTo(0m));
    }

    [Test]
    public void StrategyCannotAfford_IntentIsDropped()
    {
        var candles = MakeCandles(100m, 110m);
        var strategy = new SingleLimitStrategy(new OrderIntent(OrderSide.Buy, 1_000m, limitPrice: null));
        var request = new BacktestRequest("BTC_USD", "h1", candles[0].Timestamp, candles[^1].Timestamp,
            initialCash: 50m, feeBps: 30m, slippageBps: 20m);

        var result = new BacktestEngine().Run(request, candles, strategy);

        Assert.That(result.TradeCount, Is.EqualTo(0));
        Assert.That(result.DroppedIntents, Is.EqualTo(1));
        Assert.That(result.FinalEquity, Is.EqualTo(50m));
    }

    [Test]
    public void BuyAndHold_GapUp_RetriesUntilAffordable()
    {
        var ts = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var candles = new List<Candle>
        {
            new(ts, 100m, 100m, 100m, 100m, 0m),
            new(ts.AddHours(1), 200m, 200m, 200m, 200m, 0m),
            new(ts.AddHours(2), 50m, 50m, 50m, 50m, 0m),
            new(ts.AddHours(3), 60m, 60m, 60m, 60m, 0m),
        };

        var request = new BacktestRequest("BTC_USD", "h1", ts, ts.AddHours(3),
            initialCash: 100m, feeBps: 0m, slippageBps: 0m);

        var result = new BacktestEngine().Run(request, candles, new BuyAndHoldStrategy(0m, 0m));

        Assert.That(result.TradeCount, Is.EqualTo(1), "strategy should retry after the gap-up drop and succeed once price falls");
        Assert.That(result.DroppedIntents, Is.GreaterThanOrEqualTo(1), "first market order should be dropped by affordability guard");
        Assert.That(result.Fills[0].Price, Is.EqualTo(50m));
    }

    private sealed class SingleLimitStrategy : IStrategy
    {
        private readonly OrderIntent _intent;
        private bool _sent;

        public SingleLimitStrategy(OrderIntent intent)
        {
            _intent = intent;
        }

        public IEnumerable<OrderIntent> OnCandle(Candle candle, IPortfolioView portfolio)
        {
            if (_sent)
                yield break;
            _sent = true;
            yield return _intent;
        }
    }
}
