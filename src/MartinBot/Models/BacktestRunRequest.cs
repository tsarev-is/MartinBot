namespace MartinBot.Models;

public sealed class BacktestRunRequest
{
    public BacktestRunRequest(string pair, string timeframe, DateTimeOffset from, DateTimeOffset to,
        decimal initialCash, decimal feeBps, decimal slippageBps, string? strategyName)
    {
        Pair = pair;
        Timeframe = timeframe;
        From = from;
        To = to;
        InitialCash = initialCash;
        FeeBps = feeBps;
        SlippageBps = slippageBps;
        StrategyName = strategyName;
    }

    public string Pair { get; }

    public string Timeframe { get; }

    public DateTimeOffset From { get; }

    public DateTimeOffset To { get; }

    public decimal InitialCash { get; }

    public decimal FeeBps { get; }

    public decimal SlippageBps { get; }

    public string? StrategyName { get; }
}
