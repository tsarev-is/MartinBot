using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Extracts the scalar a sweep maximizes for a given <see cref="OptimizationMetric"/>.
/// All metrics are bigger-is-better. Ties are broken by first-wins at the caller (the sweep
/// runner replaces its best only on a strict &gt; comparison).
/// </summary>
public static class OptimizationMetricSelector
{
    public static decimal Select(OptimizationMetric metric, BacktestResult result)
        => Select(metric, result.TotalReturn, result.MaxDrawdown, result.Sharpe);

    /// <summary>
    /// Extracts the metric value from the individual scalars — used when the full
    /// <see cref="BacktestResult"/> is not available (e.g. report building from persisted fields).
    /// </summary>
    public static decimal Select(OptimizationMetric metric, decimal totalReturn, decimal maxDrawdown, decimal sharpe)
    {
        return metric switch
        {
            OptimizationMetric.TotalReturn => totalReturn,
            OptimizationMetric.Sharpe => sharpe,
            OptimizationMetric.ReturnOverMaxDrawdown => maxDrawdown == 0m
                ? totalReturn
                : totalReturn / maxDrawdown,
            _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown optimization metric")
        };
    }

    public static bool TryParse(string? value, out OptimizationMetric metric)
    {
        metric = OptimizationMetric.TotalReturn;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.Trim().ToLowerInvariant() switch
        {
            "total_return" or "totalreturn" => SetOut(OptimizationMetric.TotalReturn, out metric),
            "sharpe" => SetOut(OptimizationMetric.Sharpe, out metric),
            "return_over_max_drawdown" or "returnovermaxdrawdown" => SetOut(OptimizationMetric.ReturnOverMaxDrawdown, out metric),
            _ => false
        };
    }

    private static bool SetOut(OptimizationMetric value, out OptimizationMetric target)
    {
        target = value;
        return true;
    }
}
