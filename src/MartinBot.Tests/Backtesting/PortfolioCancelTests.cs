using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class PortfolioCancelTests
{
    [Test]
    public void Cancel_RemovesIntent_FromOpenLimitOrders()
    {
        var portfolio = new Portfolio(initialCash: 1_000m);
        var intent = new OrderIntent(OrderSide.Buy, 1m, limitPrice: 100m);
        portfolio.QueueLimit(intent);
        Assert.That(portfolio.OpenLimitOrders, Has.Count.EqualTo(1));

        portfolio.Cancel(intent);

        Assert.That(portfolio.OpenLimitOrders, Is.Empty);
    }

    [Test]
    public void Cancel_Idempotent_OnUnknownIntent()
    {
        var portfolio = new Portfolio(initialCash: 1_000m);
        var unknown = new OrderIntent(OrderSide.Buy, 1m, limitPrice: 100m);

        Assert.DoesNotThrow(() => portfolio.Cancel(unknown));
        Assert.That(portfolio.OpenLimitOrders, Is.Empty);
    }

    [Test]
    public void Cancel_OnlyTargetIntent_LeavesOthersIntact()
    {
        var portfolio = new Portfolio(initialCash: 1_000m);
        var a = new OrderIntent(OrderSide.Buy, 1m, limitPrice: 100m);
        var b = new OrderIntent(OrderSide.Buy, 1m, limitPrice: 95m);
        var c = new OrderIntent(OrderSide.Buy, 1m, limitPrice: 90m);
        portfolio.QueueLimit(a);
        portfolio.QueueLimit(b);
        portfolio.QueueLimit(c);

        portfolio.Cancel(b);

        Assert.That(portfolio.OpenLimitOrders, Has.Count.EqualTo(2));
        Assert.That(portfolio.OpenLimitOrders, Does.Contain(a));
        Assert.That(portfolio.OpenLimitOrders, Does.Contain(c));
        Assert.That(portfolio.OpenLimitOrders, Does.Not.Contain(b));
    }

    [Test]
    public void Cancel_ThenSameIntent_DoesNotThrow()
    {
        var portfolio = new Portfolio(initialCash: 1_000m);
        var intent = new OrderIntent(OrderSide.Buy, 1m, limitPrice: 100m);
        portfolio.QueueLimit(intent);
        portfolio.Cancel(intent);

        Assert.DoesNotThrow(() => portfolio.Cancel(intent));
        Assert.That(portfolio.OpenLimitOrders, Is.Empty);
    }
}
