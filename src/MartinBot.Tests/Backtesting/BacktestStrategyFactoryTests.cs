using MartinBot.Backtesting;
using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.Strategies;
using MartinBot.Domain.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class BacktestStrategyFactoryTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static BacktestRequest MakeRequest()
    {
        return new BacktestRequest("BTC_USD", "60", Origin, Origin.AddHours(30),
            initialCash: 1_000m, feeBps: 0m, slippageBps: 0m);
    }

    private static IReadOnlyList<Candle> OversoldAboveEmaCandles()
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
    public void Create_BuyAndHold_NullParameters_ReturnsStrategy()
    {
        var factory = new BacktestStrategyFactory();

        var strategy = factory.Create(BacktestStrategyFactory.BuyAndHold, MakeRequest(), parameters: null);

        Assert.That(strategy, Is.InstanceOf<BuyAndHoldStrategy>());
    }

    [Test]
    public void Create_UnknownStrategy_Throws()
    {
        var factory = new BacktestStrategyFactory();

        Assert.Throws<ArgumentException>(() => factory.Create("nope", MakeRequest()));
    }

    [Test]
    public void ValidateParameters_UnknownKey_Throws()
    {
        var factory = new BacktestStrategyFactory();
        var parameters = new Dictionary<string, decimal> { ["typoKey"] = 1m };

        var ex = Assert.Throws<ArgumentException>(
            () => factory.ValidateParameters(BacktestStrategyFactory.DcaMeanReversion, parameters));
        Assert.That(ex!.Message, Does.Contain("typoKey"));
    }

    [Test]
    public void ValidateParameters_KnownKeys_DoesNotThrow()
    {
        var factory = new BacktestStrategyFactory();
        var parameters = new Dictionary<string, decimal>
        {
            ["emaPeriod"] = 50m,
            ["entryRsi"] = 25m
        };

        Assert.DoesNotThrow(
            () => factory.ValidateParameters(BacktestStrategyFactory.DcaMeanReversion, parameters));
    }

    [Test]
    public void ValidateParameters_CaseInsensitive()
    {
        var factory = new BacktestStrategyFactory();
        var parameters = new Dictionary<string, decimal> { ["EMAPERIOD"] = 50m };

        Assert.DoesNotThrow(
            () => factory.ValidateParameters(BacktestStrategyFactory.DcaMeanReversion, parameters));
    }

    [Test]
    public void Create_DcaMr_DefaultParameters_DoesNotWarmUp_In27Candles()
    {
        var factory = new BacktestStrategyFactory();
        var candles = OversoldAboveEmaCandles();
        var request = MakeRequest();

        var strategy = factory.Create(BacktestStrategyFactory.DcaMeanReversion, request, parameters: null);
        var result = new BacktestEngine().Run(request, candles, strategy);

        Assert.That(result.TradeCount, Is.EqualTo(0),
            "default emaPeriod=200 must not warm up in 27 candles");
    }

    [Test]
    public void Create_DcaMr_ShortEmaOverride_ProducesTrades_ProvingOverrideFlowsToStrategy()
    {
        var factory = new BacktestStrategyFactory();
        var candles = OversoldAboveEmaCandles();
        var request = MakeRequest();
        var parameters = new Dictionary<string, decimal>
        {
            ["emaPeriod"] = 20m,
            ["rsiPeriod"] = 3m
        };

        var strategy = factory.Create(BacktestStrategyFactory.DcaMeanReversion, request, parameters);
        var result = new BacktestEngine().Run(request, candles, strategy);

        Assert.That(result.TradeCount, Is.GreaterThan(0),
            "override emaPeriod=20 + rsiPeriod=3 must warm up and enter within 27 candles");
        Assert.That(result.Fills[0].Side, Is.EqualTo(OrderSide.Buy));
    }

    [Test]
    public void GetDefaults_DcaMr_ContainsAllKnownKeys()
    {
        var factory = new BacktestStrategyFactory();

        var defaults = factory.GetDefaults(BacktestStrategyFactory.DcaMeanReversion);

        Assert.That(defaults.Keys, Is.EquivalentTo(new[]
        {
            "emaPeriod", "rsiPeriod", "entryRsi", "exitRsi",
            "maxTranches", "trancheFraction", "dcaDropPct", "stopLossPct"
        }));
    }

    [Test]
    public void GetDefaults_BuyAndHold_IsEmpty()
    {
        var factory = new BacktestStrategyFactory();

        var defaults = factory.GetDefaults(BacktestStrategyFactory.BuyAndHold);

        Assert.That(defaults, Is.Empty);
    }

    [Test]
    public void ValidateParameters_NonIntegerForIntKey_Throws()
    {
        var factory = new BacktestStrategyFactory();
        var parameters = new Dictionary<string, decimal> { ["emaPeriod"] = 200.5m };

        var ex = Assert.Throws<ArgumentException>(
            () => factory.ValidateParameters(BacktestStrategyFactory.DcaMeanReversion, parameters));
        Assert.That(ex!.Message, Does.Contain("emaPeriod"));
    }

    [Test]
    public void ValidateParameters_IntegerValuedDecimalForIntKey_DoesNotThrow()
    {
        var factory = new BacktestStrategyFactory();
        var parameters = new Dictionary<string, decimal> { ["emaPeriod"] = 200m };

        Assert.DoesNotThrow(
            () => factory.ValidateParameters(BacktestStrategyFactory.DcaMeanReversion, parameters));
    }

    [Test]
    public void Create_NonIntegerForIntKey_Throws()
    {
        var factory = new BacktestStrategyFactory();
        var parameters = new Dictionary<string, decimal> { ["rsiPeriod"] = 3.2m };

        Assert.Throws<ArgumentException>(
            () => factory.Create(BacktestStrategyFactory.DcaMeanReversion, MakeRequest(), parameters));
    }
}
