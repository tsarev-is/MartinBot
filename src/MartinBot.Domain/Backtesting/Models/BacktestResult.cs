namespace MartinBot.Domain.Backtesting.Models;

public sealed class BacktestResult
{
    public BacktestResult(decimal initialCash, decimal finalEquity, decimal totalReturn, decimal maxDrawdown,
        decimal sharpe, int tradeCount, int droppedIntents, decimal winRate,
        IReadOnlyList<EquityPoint> equityCurve, IReadOnlyList<Fill> fills)
    {
        InitialCash = initialCash;
        FinalEquity = finalEquity;
        TotalReturn = totalReturn;
        MaxDrawdown = maxDrawdown;
        Sharpe = sharpe;
        TradeCount = tradeCount;
        DroppedIntents = droppedIntents;
        WinRate = winRate;
        EquityCurve = equityCurve;
        Fills = fills;
    }

    public decimal InitialCash { get; }

    public decimal FinalEquity { get; }

    public decimal TotalReturn { get; }

    public decimal MaxDrawdown { get; }

    public decimal Sharpe { get; }

    public int TradeCount { get; }

    /// <summary>
    /// Count of strategy intents the engine silently discarded because the portfolio could not
    /// afford them at the execution candle (insufficient cash for buy, insufficient position for sell).
    /// Non-zero values mean the observed fills do not fully reflect the strategy's output.
    /// </summary>
    public int DroppedIntents { get; }

    public decimal WinRate { get; }

    public IReadOnlyList<EquityPoint> EquityCurve { get; }

    public IReadOnlyList<Fill> Fills { get; }
}
