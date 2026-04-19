namespace MartinBot.Domain.Entities.Models;

/// <summary>
/// Lifecycle of a locally-tracked order; <c>Pending</c> is the pre-ack window before EXMO returns an id.
/// </summary>
public enum OrderStatus
{
    Pending,
    Open,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected
}
