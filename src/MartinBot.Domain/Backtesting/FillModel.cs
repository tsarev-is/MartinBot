using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Pure simulation of order execution against a candle. MVP rules:
///   - Market orders fill at next-candle open shifted by slippageBps.
///   - Limit orders fill fully at their limit price if the candle's [low, high] touches it.
/// TODO: model partial fills and non-fill probability for limits (docs/strategies-research.md §6.3).
/// </summary>
public static class FillModel
{
    public static Fill ExecuteMarket(OrderIntent intent, Candle nextCandle, decimal feeBps, decimal slippageBps)
    {
        var slipFactor = slippageBps / 10_000m;
        var price = intent.Side == OrderSide.Buy
            ? nextCandle.Open * (1m + slipFactor)
            : nextCandle.Open * (1m - slipFactor);
        var fee = price * intent.Quantity * (feeBps / 10_000m);
        return new Fill(nextCandle.Timestamp, intent.Side, price, intent.Quantity, fee);
    }

    public static Fill? TryFillLimit(OrderIntent intent, Candle candle, decimal feeBps)
    {
        if (intent.LimitPrice is not { } limit)
            return null;
        var touched = candle.Low <= limit && limit <= candle.High;
        if (!touched)
            return null;
        var fee = limit * intent.Quantity * (feeBps / 10_000m);
        return new Fill(candle.Timestamp, intent.Side, limit, intent.Quantity, fee);
    }
}
