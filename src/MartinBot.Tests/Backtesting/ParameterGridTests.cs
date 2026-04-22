using MartinBot.Domain.Backtesting;

namespace MartinBot.Tests.Backtesting;

public sealed class ParameterGridTests
{
    [Test]
    public void Cartesian_NullGrid_YieldsOneEmptyCombo()
    {
        var combos = ParameterGrid.Cartesian(null).ToList();

        Assert.That(combos, Has.Count.EqualTo(1));
        Assert.That(combos[0], Is.Empty);
    }

    [Test]
    public void Cartesian_EmptyGrid_YieldsOneEmptyCombo()
    {
        var combos = ParameterGrid.Cartesian(new Dictionary<string, decimal[]>()).ToList();

        Assert.That(combos, Has.Count.EqualTo(1));
        Assert.That(combos[0], Is.Empty);
    }

    [Test]
    public void Cartesian_SingleAxis_YieldsNCombos()
    {
        var grid = new Dictionary<string, decimal[]> { ["emaPeriod"] = new[] { 50m, 100m, 200m } };

        var combos = ParameterGrid.Cartesian(grid).ToList();

        Assert.That(combos, Has.Count.EqualTo(3));
        Assert.That(combos.Select(c => c["emaPeriod"]), Is.EqualTo(new[] { 50m, 100m, 200m }));
    }

    [Test]
    public void Cartesian_TwoAxes_YieldsProduct()
    {
        var grid = new Dictionary<string, decimal[]>
        {
            ["a"] = new[] { 1m, 2m },
            ["b"] = new[] { 10m, 20m, 30m }
        };

        var combos = ParameterGrid.Cartesian(grid).ToList();

        Assert.That(combos, Has.Count.EqualTo(6));
        var pairs = combos.Select(c => (c["a"], c["b"])).ToList();
        Assert.That(pairs, Is.EquivalentTo(new[]
        {
            (1m, 10m), (1m, 20m), (1m, 30m),
            (2m, 10m), (2m, 20m), (2m, 30m)
        }));
    }

    [Test]
    public void Cartesian_EmptyAxis_YieldsNothing()
    {
        var grid = new Dictionary<string, decimal[]>
        {
            ["a"] = new[] { 1m, 2m },
            ["b"] = Array.Empty<decimal>()
        };

        var combos = ParameterGrid.Cartesian(grid).ToList();

        Assert.That(combos, Is.Empty);
    }

    [Test]
    public void CountCombinations_MatchesCartesianCount()
    {
        var grid = new Dictionary<string, decimal[]>
        {
            ["a"] = new[] { 1m, 2m, 3m, 4m },
            ["b"] = new[] { 10m, 20m, 30m },
            ["c"] = new[] { 100m, 200m }
        };

        var count = ParameterGrid.CountCombinations(grid);
        var actual = ParameterGrid.Cartesian(grid).Count();

        Assert.That(count, Is.EqualTo(24));
        Assert.That(actual, Is.EqualTo(count));
    }

    [Test]
    public void CountCombinations_NullOrEmpty_ReturnsOne()
    {
        Assert.That(ParameterGrid.CountCombinations(null), Is.EqualTo(1));
        Assert.That(ParameterGrid.CountCombinations(new Dictionary<string, decimal[]>()), Is.EqualTo(1));
    }

    [Test]
    public void CountCombinations_WithEmptyAxis_ReturnsZero()
    {
        var grid = new Dictionary<string, decimal[]>
        {
            ["a"] = new[] { 1m },
            ["b"] = Array.Empty<decimal>()
        };

        Assert.That(ParameterGrid.CountCombinations(grid), Is.EqualTo(0));
    }
}
