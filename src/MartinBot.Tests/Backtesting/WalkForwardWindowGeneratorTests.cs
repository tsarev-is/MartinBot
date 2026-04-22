using MartinBot.Domain.Backtesting;

namespace MartinBot.Tests.Backtesting;

public sealed class WalkForwardWindowGeneratorTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Generate_ExactFit_YieldsSingleWindow()
    {
        var from = Origin;
        var to = Origin.AddDays(10);
        var windows = WalkForwardWindowGenerator.Generate(from, to,
            TimeSpan.FromDays(7), TimeSpan.FromDays(3), TimeSpan.FromDays(3)).ToList();

        Assert.That(windows, Has.Count.EqualTo(1));
        Assert.That(windows[0].Index, Is.EqualTo(0));
        Assert.That(windows[0].TrainFrom, Is.EqualTo(from));
        Assert.That(windows[0].TrainTo, Is.EqualTo(from.AddDays(7)));
        Assert.That(windows[0].TestFrom, Is.EqualTo(from.AddDays(7)));
        Assert.That(windows[0].TestTo, Is.EqualTo(from.AddDays(10)));
    }

    [Test]
    public void Generate_RangeCleanlyDivides_YieldsMultipleWindows()
    {
        var from = Origin;
        var to = Origin.AddDays(40);
        var windows = WalkForwardWindowGenerator.Generate(from, to,
            TimeSpan.FromDays(10), TimeSpan.FromDays(5), TimeSpan.FromDays(5)).ToList();

        Assert.That(windows.Count, Is.EqualTo(6));
        for (var i = 0; i < windows.Count; i++)
        {
            Assert.That(windows[i].Index, Is.EqualTo(i));
            Assert.That(windows[i].TrainFrom, Is.EqualTo(from.AddDays(5 * i)));
            Assert.That(windows[i].TestTo, Is.LessThanOrEqualTo(to));
        }
    }

    [Test]
    public void Generate_NonDivisibleRange_StopsCleanly()
    {
        var from = Origin;
        var to = Origin.AddDays(17);
        var windows = WalkForwardWindowGenerator.Generate(from, to,
            TimeSpan.FromDays(10), TimeSpan.FromDays(5), TimeSpan.FromDays(5)).ToList();

        Assert.That(windows, Has.Count.EqualTo(1));
        Assert.That(windows[0].TestTo, Is.EqualTo(from.AddDays(15)));
    }

    [Test]
    public void Generate_RangeShorterThanTrainPlusTest_YieldsEmpty()
    {
        var windows = WalkForwardWindowGenerator.Generate(Origin, Origin.AddDays(5),
            TimeSpan.FromDays(10), TimeSpan.FromDays(5), TimeSpan.FromDays(5)).ToList();

        Assert.That(windows, Is.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Generate_NonPositiveTrainDuration_Throws(int days)
    {
        Assert.Throws<ArgumentException>(() =>
            WalkForwardWindowGenerator.Generate(Origin, Origin.AddDays(10),
                TimeSpan.FromDays(days), TimeSpan.FromDays(3), TimeSpan.FromDays(3)).ToList());
    }

    [Test]
    public void Generate_NonPositiveStep_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            WalkForwardWindowGenerator.Generate(Origin, Origin.AddDays(10),
                TimeSpan.FromDays(3), TimeSpan.FromDays(3), TimeSpan.Zero).ToList());
    }

    [Test]
    public void Generate_FromNotBeforeTo_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            WalkForwardWindowGenerator.Generate(Origin, Origin,
                TimeSpan.FromDays(3), TimeSpan.FromDays(3), TimeSpan.FromDays(1)).ToList());
    }

    [Test]
    public void Generate_StepLargerThanTrain_YieldsNonOverlappingWindows()
    {
        var from = Origin;
        var to = Origin.AddDays(100);
        var windows = WalkForwardWindowGenerator.Generate(from, to,
            TimeSpan.FromDays(5), TimeSpan.FromDays(5), TimeSpan.FromDays(20)).ToList();

        Assert.That(windows.Count, Is.GreaterThanOrEqualTo(2));
        for (var i = 1; i < windows.Count; i++)
            Assert.That(windows[i].TrainFrom, Is.EqualTo(windows[i - 1].TrainFrom + TimeSpan.FromDays(20)));
    }
}
