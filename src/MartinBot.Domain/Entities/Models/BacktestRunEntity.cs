namespace MartinBot.Domain.Entities.Models;

/// <summary>
/// Record of a single backtest execution — request parameters plus summary metrics.
/// Full equity curve and trade log are kept in memory only; this table is for run history and UI listing.
/// </summary>
public sealed class BacktestRunEntity
{
    public BacktestRunEntity(long id, string pair, string timeframe, DateTimeOffset from, DateTimeOffset to,
        decimal initialCash, decimal feeBps, decimal slippageBps, string strategyName,
        BacktestRunStatus status, decimal? finalEquity, decimal? totalReturn, decimal? maxDrawdown,
        decimal? sharpe, int? tradeCount, decimal? winRate, string? errorMessage,
        DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Pair = pair;
        Timeframe = timeframe;
        From = from;
        To = to;
        InitialCash = initialCash;
        FeeBps = feeBps;
        SlippageBps = slippageBps;
        StrategyName = strategyName;
        Status = status;
        FinalEquity = finalEquity;
        TotalReturn = totalReturn;
        MaxDrawdown = maxDrawdown;
        Sharpe = sharpe;
        TradeCount = tradeCount;
        WinRate = winRate;
        ErrorMessage = errorMessage;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public long Id { get; private set; }

    public string Pair { get; private set; }

    public string Timeframe { get; private set; }

    public DateTimeOffset From { get; private set; }

    public DateTimeOffset To { get; private set; }

    public decimal InitialCash { get; private set; }

    public decimal FeeBps { get; private set; }

    public decimal SlippageBps { get; private set; }

    public string StrategyName { get; private set; }

    public BacktestRunStatus Status { get; private set; }

    public decimal? FinalEquity { get; private set; }

    public decimal? TotalReturn { get; private set; }

    public decimal? MaxDrawdown { get; private set; }

    public decimal? Sharpe { get; private set; }

    public int? TradeCount { get; private set; }

    public decimal? WinRate { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkRunning(DateTimeOffset now)
    {
        Status = BacktestRunStatus.Running;
        UpdatedAt = now;
    }

    public void MarkSucceeded(decimal finalEquity, decimal totalReturn, decimal maxDrawdown, decimal sharpe,
        int tradeCount, decimal winRate, DateTimeOffset now)
    {
        Status = BacktestRunStatus.Succeeded;
        FinalEquity = finalEquity;
        TotalReturn = totalReturn;
        MaxDrawdown = maxDrawdown;
        Sharpe = sharpe;
        TradeCount = tradeCount;
        WinRate = winRate;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = BacktestRunStatus.Failed;
        ErrorMessage = error;
        UpdatedAt = now;
    }
}
