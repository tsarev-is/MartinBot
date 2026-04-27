using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting.Strategies;

/// <summary>
/// Emits no orders. Used as the executable form of a regime-selector "Pause" decision —
/// the runner swaps to this strategy when the selector classifies a slice as TrendDown
/// (docs/strategies.md §2, docs/phase6-experiments.md line 954).
/// </summary>
public sealed class NoOpStrategy : IStrategy
{
    public IEnumerable<OrderIntent> OnCandle(Candle candle, IPortfolioView portfolio)
    {
        yield break;
    }
}
