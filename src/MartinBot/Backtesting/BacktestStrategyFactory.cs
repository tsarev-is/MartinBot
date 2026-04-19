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

    public IStrategy Create(string name, BacktestRequest request)
    {
        return name switch
        {
            BuyAndHold => new BuyAndHoldStrategy(request.FeeBps, request.SlippageBps),
            _ => throw new ArgumentException($"Unknown strategy: {name}")
        };
    }
}
