namespace MartinBot.Domain.Backtesting.Models;

public sealed class Candle
{
    public Candle(DateTimeOffset timestamp, decimal open, decimal high, decimal low, decimal close, decimal volume)
    {
        Timestamp = timestamp;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
    }

    public DateTimeOffset Timestamp { get; }

    public decimal Open { get; }

    public decimal High { get; }

    public decimal Low { get; }

    public decimal Close { get; }

    public decimal Volume { get; }
}
