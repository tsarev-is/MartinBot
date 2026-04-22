using MartinBot.Domain.Backtesting;

namespace MartinBot.Tests.Backtesting;

public sealed class ParameterGridSerializerTests
{
    [Test]
    public void Serialize_Null_ReturnsNull()
    {
        Assert.That(ParameterGridSerializer.Serialize(null), Is.Null);
    }

    [Test]
    public void Serialize_Empty_ReturnsNull()
    {
        Assert.That(ParameterGridSerializer.Serialize(new Dictionary<string, decimal[]>()), Is.Null);
    }

    [Test]
    public void Deserialize_Null_ReturnsNull()
    {
        Assert.That(ParameterGridSerializer.Deserialize(null), Is.Null);
    }

    [Test]
    public void Deserialize_Empty_ReturnsNull()
    {
        Assert.That(ParameterGridSerializer.Deserialize(string.Empty), Is.Null);
    }

    [Test]
    public void Roundtrip_PreservesKeysAndValues()
    {
        var original = new Dictionary<string, decimal[]>
        {
            ["emaPeriod"] = new[] { 100m, 200m },
            ["entryRsi"] = new[] { 25m, 30m, 35m }
        };

        var json = ParameterGridSerializer.Serialize(original);
        Assert.That(json, Is.Not.Null);

        var decoded = ParameterGridSerializer.Deserialize(json);
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded!["emaPeriod"], Is.EqualTo(new[] { 100m, 200m }));
        Assert.That(decoded["entryRsi"], Is.EqualTo(new[] { 25m, 30m, 35m }));
    }
}
