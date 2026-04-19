using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Read-only snapshot of the portfolio passed to a strategy so it cannot mutate engine state.
/// </summary>
public interface IPortfolioView
{
    decimal Cash { get; }

    decimal Position { get; }

    decimal Equity { get; }

    IReadOnlyList<OrderIntent> OpenLimitOrders { get; }
}
