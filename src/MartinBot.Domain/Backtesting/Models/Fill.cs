using MartinBot.Domain.Models;

namespace MartinBot.Domain.Backtesting.Models;

public sealed class Fill
{
    public Fill(DateTimeOffset timestamp, OrderSide side, decimal price, decimal quantity, decimal fee)
    {
        Timestamp = timestamp;
        Side = side;
        Price = price;
        Quantity = quantity;
        Fee = fee;
    }

    public DateTimeOffset Timestamp { get; }

    public OrderSide Side { get; }

    public decimal Price { get; }

    public decimal Quantity { get; }

    public decimal Fee { get; }
}
