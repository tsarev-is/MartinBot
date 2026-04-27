using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Snapshot of the portfolio passed to a strategy. The state is read-only with one
/// narrow exception: <see cref="Cancel"/> lets a strategy retract one of its own
/// previously-emitted limit orders. Use only when actively withdrawing an intent
/// (e.g. grid invalidation, see docs/gridstrategy-plan.md).
/// </summary>
public interface IPortfolioView
{
    decimal Cash { get; }

    decimal Position { get; }

    decimal Equity { get; }

    IReadOnlyList<OrderIntent> OpenLimitOrders { get; }

    /// <summary>
    /// Removes <paramref name="intent"/> from the open-limit book. Idempotent — a no-op
    /// if the intent has already filled or was never queued. Caller must snapshot
    /// (e.g. <c>OpenLimitOrders.ToArray()</c>) before iterating + cancelling, since
    /// <see cref="OpenLimitOrders"/> is backed by the same list and concurrent modification
    /// throws <see cref="InvalidOperationException"/>.
    /// </summary>
    void Cancel(OrderIntent intent);
}
