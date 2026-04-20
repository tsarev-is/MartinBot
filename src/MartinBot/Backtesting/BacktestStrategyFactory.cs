using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.Strategies;

namespace MartinBot.Backtesting;

/// <summary>
/// Resolves a strategy instance by the name persisted in <c>BacktestRunEntity.StrategyName</c>.
/// Add new strategies here once the Domain layer gains them.
/// </summary>
public sealed class BacktestStrategyFactory
{
    public const string BuyAndHold = "buy_and_hold";
    public const string DcaMeanReversion = "dca_mr";

    public IStrategy Create(string name, BacktestRequest request)
    {
        return name switch
        {
            BuyAndHold => new BuyAndHoldStrategy(request.FeeBps, request.SlippageBps),
            DcaMeanReversion => new DcaMeanReversionStrategy(request.FeeBps, request.SlippageBps, request.InitialCash,
                emaPeriod: 200, rsiPeriod: 14, entryRsi: 30m, exitRsi: 50m,
                maxTranches: 3, trancheFraction: 0.25m, dcaDropPct: 0.03m, stopLossPct: 0.10m),
            _ => throw new ArgumentException($"Unknown strategy: {name}")
        };
    }
}
