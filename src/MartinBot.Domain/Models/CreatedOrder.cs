namespace MartinBot.Domain.Models;

public sealed class CreatedOrder
{
    public CreatedOrder(long orderId)
    {
        OrderId = orderId;
    }

    public long OrderId { get; }
}
