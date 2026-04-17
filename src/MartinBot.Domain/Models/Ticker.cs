namespace MartinBot.Domain.Models;

public sealed class Ticker
{
    public Ticker(string pair, decimal bid, decimal ask, decimal last, DateTimeOffset updatedAt)
    {
        Pair = pair;
        Bid = bid;
        Ask = ask;
        Last = last;
        UpdatedAt = updatedAt;
    }

    public string Pair { get; }

    public decimal Bid { get; }

    public decimal Ask { get; }

    public decimal Last { get; }

    public DateTimeOffset UpdatedAt { get; }
}
