using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting.RegimeSelector;

/// <summary>
/// Inspects a slice of candle history (strictly causal, no look-ahead) and returns a
/// decision that the runner uses to either run the configured strategy or pause it.
/// Per docs/strategies.md §6, the selector must be deterministic — same input, same output.
/// </summary>
public interface IRegimeSelector
{
    RegimeDecision Decide(IReadOnlyList<Candle> history);
}

public enum Regime
{
    /// <summary>Insufficient history to evaluate the rule. Caller should not pause.</summary>
    InsufficientHistory,

    /// <summary>Conditions for a Pause regime not met; run the configured strategy.</summary>
    Active,

    /// <summary>Bear regime detected (EMA50 &lt; EMA200 AND ADX &gt; threshold).</summary>
    TrendDown
}

public sealed class RegimeDecision
{
    public RegimeDecision(Regime regime, bool shouldPause, string reason)
    {
        Regime = regime;
        ShouldPause = shouldPause;
        Reason = reason;
    }

    public Regime Regime { get; }

    public bool ShouldPause { get; }

    public string Reason { get; }
}
