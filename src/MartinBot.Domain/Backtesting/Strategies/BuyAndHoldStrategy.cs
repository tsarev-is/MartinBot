using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Domain.Backtesting.Strategies;

/// <summary>
/// Trivial reference strategy: buy once when flat, then hold. Sanity check for the engine's math.
/// Retry-safe on gap-ups — if the first market order is dropped by the affordability guard on the
/// next candle's open, position stays 0 and the next <see cref="OnCandle"/> tries again.
/// </summary>
public sealed class BuyAndHoldStrategy : IStrategy
{
    private readonly decimal _feeBps;
    private readonly decimal _slippageBps;

    public BuyAndHoldStrategy(decimal feeBps, decimal slippageBps)
    {
        _feeBps = feeBps;
        _slippageBps = slippageBps;
    }

    public IEnumerable<OrderIntent> OnCandle(Candle candle, IPortfolioView portfolio)
    {
        if (portfolio.Position > 0m || portfolio.OpenLimitOrders.Count > 0)
            yield break;

        var assumedFill = candle.Close * (1m + _slippageBps / 10_000m);
        var costPerUnit = assumedFill * (1m + _feeBps / 10_000m);
        if (costPerUnit <= 0m)
            yield break;

        var quantity = decimal.Floor(portfolio.Cash / costPerUnit * 1_000_000m) / 1_000_000m;
        if (quantity <= 0m)
            yield break;

        yield return new OrderIntent(OrderSide.Buy, quantity, limitPrice: null);
    }
}
