using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class FillModelTests
{
    private static Candle Bar(decimal open, decimal high, decimal low, decimal close)
        => new(DateTimeOffset.UnixEpoch, open, high, low, close, 0m);

    [Test]
    public void Market_Buy_PaysSlipPlusFee_AboveOpen()
    {
        var intent = new OrderIntent(OrderSide.Buy, quantity: 1m, limitPrice: null);
        var fill = FillModel.ExecuteMarket(intent, Bar(100m, 110m, 95m, 105m), feeBps: 30m, slippageBps: 20m);

        Assert.That(fill.Price, Is.EqualTo(100.20m));
        Assert.That(fill.Fee, Is.EqualTo(100.20m * 0.003m));
        Assert.That(fill.Quantity, Is.EqualTo(1m));
        Assert.That(fill.Side, Is.EqualTo(OrderSide.Buy));
    }

    [Test]
    public void Market_Sell_ReceivesBelowOpen_MinusFee()
    {
        var intent = new OrderIntent(OrderSide.Sell, quantity: 2m, limitPrice: null);
        var fill = FillModel.ExecuteMarket(intent, Bar(100m, 110m, 95m, 105m), feeBps: 30m, slippageBps: 20m);

        Assert.That(fill.Price, Is.EqualTo(99.80m));
        Assert.That(fill.Fee, Is.EqualTo(99.80m * 2m * 0.003m));
    }

    [Test]
    public void Limit_Fills_WhenRangeTouchesPrice()
    {
        var intent = new OrderIntent(OrderSide.Buy, quantity: 1m, limitPrice: 99m);
        var fill = FillModel.TryFillLimit(intent, Bar(100m, 101m, 98m, 100.5m), feeBps: 30m);

        Assert.That(fill, Is.Not.Null);
        Assert.That(fill!.Price, Is.EqualTo(99m));
        Assert.That(fill.Fee, Is.EqualTo(99m * 0.003m));
    }

    [Test]
    public void Limit_DoesNotFill_WhenOutsideRange()
    {
        var intent = new OrderIntent(OrderSide.Sell, quantity: 1m, limitPrice: 120m);
        var fill = FillModel.TryFillLimit(intent, Bar(100m, 110m, 98m, 105m), feeBps: 30m);

        Assert.That(fill, Is.Null);
    }

    [Test]
    public void Limit_ReturnsNull_ForMarketIntent()
    {
        var intent = new OrderIntent(OrderSide.Buy, quantity: 1m, limitPrice: null);
        var fill = FillModel.TryFillLimit(intent, Bar(100m, 110m, 95m, 105m), feeBps: 30m);

        Assert.That(fill, Is.Null);
    }
}
