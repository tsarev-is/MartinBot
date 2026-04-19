namespace MartinBot.Domain.Backtesting.Models;

public sealed class EquityPoint
{
    public EquityPoint(DateTimeOffset timestamp, decimal equity)
    {
        Timestamp = timestamp;
        Equity = equity;
    }

    public DateTimeOffset Timestamp { get; }

    public decimal Equity { get; }
}
