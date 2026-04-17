namespace MartinBot.Domain.Models;

public sealed class OpenOrder
{
    public OpenOrder(long orderId, string pair, OrderSide side, decimal price,
        decimal quantity, decimal remainingQuantity, DateTimeOffset createdAt)
    {
        OrderId = orderId;
        Pair = pair;
        Side = side;
        Price = price;
        Quantity = quantity;
        RemainingQuantity = remainingQuantity;
        CreatedAt = createdAt;
    }

    public long OrderId { get; }

    public string Pair { get; }

    public OrderSide Side { get; }

    public decimal Price { get; }

    public decimal Quantity { get; }

    public decimal RemainingQuantity { get; }

    public DateTimeOffset CreatedAt { get; }
}
