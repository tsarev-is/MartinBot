using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Mutable portfolio state held by the engine. Exposed to strategies only via <see cref="IPortfolioView"/>.
/// Spot-only: shorts are rejected (see docs/strategies-research.md §1 — EXMO has no futures).
/// </summary>
public sealed class Portfolio : IPortfolioView
{
    private readonly List<Fill> _fills = new();
    private readonly List<OrderIntent> _openLimits = new();
    private decimal _lastMarkPrice;

    public Portfolio(decimal initialCash)
    {
        Cash = initialCash;
        Position = 0m;
        _lastMarkPrice = 0m;
    }

    /// <summary>
    /// Quote-currency balance (e.g. USD for BTC_USD).
    /// </summary>
    public decimal Cash { get; private set; }

    /// <summary>
    /// Base-asset quantity currently held (e.g. BTC for BTC_USD). Spot-only, never negative.
    /// </summary>
    public decimal Position { get; private set; }

    public decimal Equity => Cash + Position * _lastMarkPrice;

    public IReadOnlyList<OrderIntent> OpenLimitOrders => _openLimits;

    public IReadOnlyList<Fill> Fills => _fills;

    public void Mark(decimal price) => _lastMarkPrice = price;

    public void QueueLimit(OrderIntent intent) => _openLimits.Add(intent);

    public void RemoveLimit(OrderIntent intent) => _openLimits.Remove(intent);

    public void ApplyFill(Fill fill)
    {
        var notional = fill.Price * fill.Quantity;
        if (fill.Side == OrderSide.Buy)
        {
            Cash -= notional + fill.Fee;
            Position += fill.Quantity;
        }
        else
        {
            Cash += notional - fill.Fee;
            Position -= fill.Quantity;
        }
        _fills.Add(fill);
    }

    /// <summary>
    /// Checks whether a proposed buy/sell is fundable given current cash/position.
    /// Spot-only — no shorts, no leverage.
    /// </summary>
    public bool CanAfford(OrderSide side, decimal quantity, decimal price, decimal feeBps)
    {
        if (quantity <= 0m)
            return false;
        if (side == OrderSide.Buy)
        {
            var cost = price * quantity * (1m + feeBps / 10_000m);
            return Cash >= cost;
        }
        return Position >= quantity;
    }
}
