using MartinBot.Domain.Models;

namespace MartinBot.Domain.Backtesting.Models;

/// <summary>
/// What a strategy asks the engine to do on the next candle.
/// <see cref="LimitPrice"/> null means a market order executed on the next candle's open.
/// </summary>
public sealed class OrderIntent
{
    public OrderIntent(OrderSide side, decimal quantity, decimal? limitPrice)
    {
        Side = side;
        Quantity = quantity;
        LimitPrice = limitPrice;
    }

    public OrderSide Side { get; }

    public decimal Quantity { get; }

    public decimal? LimitPrice { get; }

    public bool IsMarket => LimitPrice is null;
}
