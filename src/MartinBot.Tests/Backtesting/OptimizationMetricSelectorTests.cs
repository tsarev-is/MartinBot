using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Tests.Backtesting;

public sealed class OptimizationMetricSelectorTests
{
    private static BacktestResult MakeResult(decimal totalReturn, decimal maxDrawdown, decimal sharpe)
    {
        return new BacktestResult(initialCash: 1_000m, finalEquity: 1_000m + totalReturn * 1_000m,
            totalReturn: totalReturn, maxDrawdown: maxDrawdown, sharpe: sharpe,
            tradeCount: 0, droppedIntents: 0, winRate: 0m,
            equityCurve: Array.Empty<EquityPoint>(), fills: Array.Empty<Fill>());
    }

    [Test]
    public void Select_TotalReturn_ReturnsTotalReturn()
    {
        var result = MakeResult(totalReturn: 0.42m, maxDrawdown: 0.1m, sharpe: 1.5m);

        Assert.That(OptimizationMetricSelector.Select(OptimizationMetric.TotalReturn, result),
            Is.EqualTo(0.42m));
    }

    [Test]
    public void Select_Sharpe_ReturnsSharpe()
    {
        var result = MakeResult(totalReturn: 0.42m, maxDrawdown: 0.1m, sharpe: 1.5m);

        Assert.That(OptimizationMetricSelector.Select(OptimizationMetric.Sharpe, result),
            Is.EqualTo(1.5m));
    }

    [Test]
    public void Select_ReturnOverMaxDrawdown_ReturnsRatio()
    {
        var result = MakeResult(totalReturn: 0.5m, maxDrawdown: 0.25m, sharpe: 0m);

        Assert.That(OptimizationMetricSelector.Select(OptimizationMetric.ReturnOverMaxDrawdown, result),
            Is.EqualTo(2m));
    }

    [Test]
    public void Select_ReturnOverMaxDrawdown_ZeroDrawdown_FallsBackToTotalReturn()
    {
        var result = MakeResult(totalReturn: 0.3m, maxDrawdown: 0m, sharpe: 0m);

        Assert.That(OptimizationMetricSelector.Select(OptimizationMetric.ReturnOverMaxDrawdown, result),
            Is.EqualTo(0.3m));
    }

    [TestCase("total_return", OptimizationMetric.TotalReturn)]
    [TestCase("TotalReturn", OptimizationMetric.TotalReturn)]
    [TestCase("sharpe", OptimizationMetric.Sharpe)]
    [TestCase("SHARPE", OptimizationMetric.Sharpe)]
    [TestCase("return_over_max_drawdown", OptimizationMetric.ReturnOverMaxDrawdown)]
    [TestCase("ReturnOverMaxDrawdown", OptimizationMetric.ReturnOverMaxDrawdown)]
    public void TryParse_KnownValues_Succeeds(string input, OptimizationMetric expected)
    {
        Assert.That(OptimizationMetricSelector.TryParse(input, out var metric), Is.True);
        Assert.That(metric, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("sortino")]
    [TestCase("garbage")]
    public void TryParse_UnknownValues_Fails(string? input)
    {
        Assert.That(OptimizationMetricSelector.TryParse(input, out _), Is.False);
    }

    [Test]
    public void SelectFromScalars_TotalReturn_MatchesInput()
    {
        Assert.That(OptimizationMetricSelector.Select(OptimizationMetric.TotalReturn,
            totalReturn: 0.42m, maxDrawdown: 0.1m, sharpe: 1.5m), Is.EqualTo(0.42m));
    }

    [Test]
    public void SelectFromScalars_Sharpe_MatchesInput()
    {
        Assert.That(OptimizationMetricSelector.Select(OptimizationMetric.Sharpe,
            totalReturn: 0.42m, maxDrawdown: 0.1m, sharpe: 1.5m), Is.EqualTo(1.5m));
    }

    [Test]
    public void SelectFromScalars_ReturnOverMaxDrawdown_ZeroDrawdown_FallsBackToTotalReturn()
    {
        Assert.That(OptimizationMetricSelector.Select(OptimizationMetric.ReturnOverMaxDrawdown,
            totalReturn: 0.3m, maxDrawdown: 0m, sharpe: 0m), Is.EqualTo(0.3m));
    }
}
