using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Deterministic per-candle decision surface. The engine guarantees the strategy only sees
/// candles up to and including <paramref name="candle"/>; any returned intents execute on the
/// next candle (no look-ahead bias, see docs/strategies-research.md §6.3).
/// </summary>
public interface IStrategy
{
    IEnumerable<OrderIntent> OnCandle(Candle candle, IPortfolioView portfolio);
}
