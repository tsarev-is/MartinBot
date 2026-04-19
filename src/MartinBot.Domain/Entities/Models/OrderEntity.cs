using MartinBot.Domain.Models;

namespace MartinBot.Domain.Entities.Models;

/// <summary>
/// Local mirror of orders placed on EXMO — enables restart reconciliation and partial-fill tracking.
/// </summary>
public sealed class OrderEntity
{
    public OrderEntity(long id, string clientId, long? exmoOrderId, string pair, OrderSide side,
        decimal price, decimal quantity, decimal filledQuantity, OrderStatus status,
        DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        ClientId = clientId;
        ExmoOrderId = exmoOrderId;
        Pair = pair;
        Side = side;
        Price = price;
        Quantity = quantity;
        FilledQuantity = filledQuantity;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public long Id { get; private set; }

    public string ClientId { get; private set; }

    public long? ExmoOrderId { get; private set; }

    public string Pair { get; private set; }

    public OrderSide Side { get; private set; }

    public decimal Price { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal FilledQuantity { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
