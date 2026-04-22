using MartinBot.Domain.Backtesting;

namespace MartinBot.Tests.Backtesting;

public sealed class TimeframeConverterTests
{
    [TestCase("1", 1)]
    [TestCase("5", 5)]
    [TestCase("15", 15)]
    [TestCase("60", 60)]
    [TestCase("240", 240)]
    public void ToTimeSpan_NumericMinutes_ParsesCorrectly(string input, int expectedMinutes)
    {
        Assert.That(TimeframeConverter.ToTimeSpan(input), Is.EqualTo(TimeSpan.FromMinutes(expectedMinutes)));
    }

    [Test]
    public void ToTimeSpan_DailyResolution_IsOneDay()
    {
        Assert.That(TimeframeConverter.ToTimeSpan("D"), Is.EqualTo(TimeSpan.FromDays(1)));
    }

    [Test]
    public void ToTimeSpan_WeeklyResolution_IsSevenDays()
    {
        Assert.That(TimeframeConverter.ToTimeSpan("W"), Is.EqualTo(TimeSpan.FromDays(7)));
    }

    [Test]
    public void ToTimeSpan_MonthlyResolution_IsThirtyDays()
    {
        Assert.That(TimeframeConverter.ToTimeSpan("M"), Is.EqualTo(TimeSpan.FromDays(30)));
    }

    [Test]
    public void ToTimeSpan_LowercaseD_IsCaseInsensitive()
    {
        Assert.That(TimeframeConverter.ToTimeSpan("d"), Is.EqualTo(TimeSpan.FromDays(1)));
    }

    [Test]
    public void ToTimeSpan_UnknownResolution_Throws()
    {
        Assert.Throws<ArgumentException>(() => TimeframeConverter.ToTimeSpan("X"));
    }

    [Test]
    public void ToTimeSpan_ZeroMinutes_Throws()
    {
        Assert.Throws<ArgumentException>(() => TimeframeConverter.ToTimeSpan("0"));
    }

    [Test]
    public void PeriodsPerYear_Hourly_Is8760()
    {
        Assert.That(TimeframeConverter.PeriodsPerYear("60"), Is.EqualTo(365d * 24d).Within(0.001));
    }

    [Test]
    public void PeriodsPerYear_Daily_Is365()
    {
        Assert.That(TimeframeConverter.PeriodsPerYear("D"), Is.EqualTo(365d).Within(0.001));
    }

    [Test]
    public void PeriodsPerYear_FifteenMinute_Is35040()
    {
        Assert.That(TimeframeConverter.PeriodsPerYear("15"), Is.EqualTo(365d * 24d * 4d).Within(0.001));
    }
}
