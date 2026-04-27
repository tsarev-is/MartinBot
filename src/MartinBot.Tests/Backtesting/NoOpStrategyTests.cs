using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.Strategies;

namespace MartinBot.Tests.Backtesting;

public sealed class NoOpStrategyTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Engine_NoOpStrategy_EmitsNoTrades()
    {
        var candles = new List<Candle>(50);
        for (var i = 0; i < 50; i++)
        {
            var price = 100m + (i % 5);
            candles.Add(new Candle(Origin.AddHours(i), price, price, price, price, 0m));
        }
        var request = new BacktestRequest("BTC_USD", "60", candles[0].Timestamp, candles[^1].Timestamp,
            initialCash: 1_000m, feeBps: 30m, slippageBps: 20m);

        var result = new BacktestEngine().Run(request, candles, new NoOpStrategy());

        Assert.That(result.TradeCount, Is.EqualTo(0));
        Assert.That(result.Fills, Is.Empty);
        Assert.That(result.DroppedIntents, Is.EqualTo(0));
        Assert.That(result.FinalEquity, Is.EqualTo(1_000m));
    }
}
