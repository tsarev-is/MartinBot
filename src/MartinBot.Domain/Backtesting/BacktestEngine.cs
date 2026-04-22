using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Deterministic candle-by-candle backtest driver.
///
/// Invariant (no look-ahead, see docs/strategies-research.md §6.3):
///   strategy sees candle[i], and any intent it returns is executed against candle[i+1]
///   (market) or any subsequent candle whose range touches the limit price.
/// </summary>
public sealed class BacktestEngine
{
    public BacktestResult Run(BacktestRequest request, IReadOnlyList<Candle> candles, IStrategy strategy)
    {
        var portfolio = new Portfolio(request.InitialCash);
        var equityCurve = new List<EquityPoint>(candles.Count);
        var pendingMarket = new List<OrderIntent>();
        var dropped = 0;

        for (var i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            portfolio.Mark(candle.Open);

            dropped += FillPendingMarkets(pendingMarket, candle, portfolio, request.FeeBps, request.SlippageBps);
            FillTouchedLimits(portfolio, candle, request.FeeBps);

            portfolio.Mark(candle.Close);
            equityCurve.Add(new EquityPoint(candle.Timestamp, portfolio.Equity));

            foreach (var intent in strategy.OnCandle(candle, portfolio))
            {
                if (!Queue(intent, portfolio, candle.Close, request.FeeBps, pendingMarket))
                    dropped++;
            }
        }

        var metrics = Metrics.Compute(equityCurve, portfolio.Fills, request.InitialCash, request.Timeframe);
        return new BacktestResult(
            initialCash: request.InitialCash,
            finalEquity: portfolio.Equity,
            totalReturn: metrics.TotalReturn,
            maxDrawdown: metrics.MaxDrawdown,
            sharpe: metrics.Sharpe,
            tradeCount: portfolio.Fills.Count,
            droppedIntents: dropped,
            winRate: metrics.WinRate,
            equityCurve: equityCurve,
            fills: portfolio.Fills);
    }

    private static int FillPendingMarkets(List<OrderIntent> pending, Candle candle, Portfolio portfolio,
        decimal feeBps, decimal slippageBps)
    {
        var dropped = 0;
        foreach (var intent in pending)
        {
            if (!portfolio.CanAfford(intent.Side, intent.Quantity, candle.Open, feeBps))
            {
                dropped++;
                continue;
            }
            var fill = FillModel.ExecuteMarket(intent, candle, feeBps, slippageBps);
            portfolio.ApplyFill(fill);
        }
        pending.Clear();
        return dropped;
    }

    private static void FillTouchedLimits(Portfolio portfolio, Candle candle, decimal feeBps)
    {
        var touched = new List<OrderIntent>();
        foreach (var intent in portfolio.OpenLimitOrders)
        {
            var fill = FillModel.TryFillLimit(intent, candle, feeBps);
            if (fill is null)
                continue;
            if (!portfolio.CanAfford(intent.Side, intent.Quantity, fill.Price, feeBps))
                continue;
            portfolio.ApplyFill(fill);
            touched.Add(intent);
        }
        foreach (var intent in touched)
            portfolio.RemoveLimit(intent);
    }

    private static bool Queue(OrderIntent intent, Portfolio portfolio, decimal markPrice, decimal feeBps,
        List<OrderIntent> pendingMarket)
    {
        if (intent.Quantity <= 0m)
            return false;
        var checkPrice = intent.LimitPrice ?? markPrice;
        if (!portfolio.CanAfford(intent.Side, intent.Quantity, checkPrice, feeBps))
            return false;

        if (intent.IsMarket)
            pendingMarket.Add(intent);
        else
            portfolio.QueueLimit(intent);
        return true;
    }
}
