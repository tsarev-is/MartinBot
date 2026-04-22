namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Which scalar a parameter sweep maximizes when ranking combinations.
/// All values are bigger-is-better; see <see cref="OptimizationMetricSelector"/> for extraction.
/// </summary>
public enum OptimizationMetric
{
    TotalReturn = 0,
    Sharpe = 1,
    ReturnOverMaxDrawdown = 2
}
