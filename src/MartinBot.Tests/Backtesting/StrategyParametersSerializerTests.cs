using MartinBot.Domain.Backtesting;

namespace MartinBot.Tests.Backtesting;

public sealed class StrategyParametersSerializerTests
{
    [Test]
    public void Serialize_Null_ReturnsNull()
    {
        Assert.That(StrategyParametersSerializer.Serialize(null), Is.Null);
    }

    [Test]
    public void Serialize_Empty_ReturnsNull()
    {
        var empty = new Dictionary<string, decimal>();

        Assert.That(StrategyParametersSerializer.Serialize(empty), Is.Null);
    }

    [Test]
    public void Deserialize_Null_ReturnsNull()
    {
        Assert.That(StrategyParametersSerializer.Deserialize(null), Is.Null);
    }

    [Test]
    public void Deserialize_Empty_ReturnsNull()
    {
        Assert.That(StrategyParametersSerializer.Deserialize(string.Empty), Is.Null);
    }

    [Test]
    public void Roundtrip_PreservesKeysAndValues()
    {
        var original = new Dictionary<string, decimal>
        {
            ["emaPeriod"] = 200m,
            ["entryRsi"] = 30m,
            ["trancheFraction"] = 0.25m
        };

        var json = StrategyParametersSerializer.Serialize(original);
        Assert.That(json, Is.Not.Null);

        var decoded = StrategyParametersSerializer.Deserialize(json);
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded!.Count, Is.EqualTo(3));
        Assert.That(decoded["emaPeriod"], Is.EqualTo(200m));
        Assert.That(decoded["entryRsi"], Is.EqualTo(30m));
        Assert.That(decoded["trancheFraction"], Is.EqualTo(0.25m));
    }
}
