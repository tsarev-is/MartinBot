using MartinBot.Domain.Models;

namespace MartinBot.Domain;

public interface IExmoService
{
    /// <summary>
    /// Retrieves the latest ticker (best bid, best ask, last trade) for the specified trading pair.
    /// </summary>
    Task<Ticker> GetTickerAsync(string pair, CancellationToken ct = default);

    /// <summary>
    /// Returns the authenticated user's available balances keyed by currency code.
    /// </summary>
    Task<IReadOnlyDictionary<string, decimal>> GetBalancesAsync(CancellationToken ct = default);

    /// <summary>
    /// Places a limit order on the specified pair at the given price and quantity and returns its identifier.
    /// </summary>
    Task<CreatedOrder> CreateLimitOrderAsync(string pair, OrderSide side, decimal quantity, decimal price, CancellationToken ct = default);

    /// <summary>
    /// Cancels a previously placed order by its identifier.
    /// </summary>
    Task CancelOrderAsync(long orderId, CancellationToken ct = default);

    /// <summary>
    /// Returns all currently open orders belonging to the authenticated user across every trading pair.
    /// </summary>
    Task<IReadOnlyList<OpenOrder>> GetOpenOrdersAsync(CancellationToken ct = default);
}
