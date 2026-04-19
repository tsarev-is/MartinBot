namespace MartinBot.Domain.Backtesting.Models;

public sealed class BacktestRequest
{
    public BacktestRequest(string pair, string timeframe, DateTimeOffset from, DateTimeOffset to,
        decimal initialCash, decimal feeBps, decimal slippageBps)
    {
        Pair = pair;
        Timeframe = timeframe;
        From = from;
        To = to;
        InitialCash = initialCash;
        FeeBps = feeBps;
        SlippageBps = slippageBps;
    }

    public string Pair { get; }

    public string Timeframe { get; }

    public DateTimeOffset From { get; }

    public DateTimeOffset To { get; }

    public decimal InitialCash { get; }

    public decimal FeeBps { get; }

    public decimal SlippageBps { get; }
}
