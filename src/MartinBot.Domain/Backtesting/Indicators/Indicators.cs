using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting.Indicators;

/// <summary>
/// One-shot batch indicator math used by the regime selector. Returns full-length series
/// so callers can index by candle position; positions before the indicator is well-defined
/// hold 0m. Strategies that need streaming/incremental updates keep their own state
/// (see DcaMeanReversionStrategy).
/// </summary>
public static class Indicators
{
    /// <summary>
    /// Exponential moving average. Seed = SMA of first <paramref name="period"/> values;
    /// thereafter <c>alpha = 2 / (period + 1)</c> smoothing. Indices &lt; period − 1 hold 0m.
    /// </summary>
    public static decimal[] ComputeEma(IReadOnlyList<decimal> values, int period)
    {
        if (period < 2)
            throw new ArgumentOutOfRangeException(nameof(period));
        var result = new decimal[values.Count];
        if (values.Count < period)
            return result;

        decimal seed = 0m;
        for (var i = 0; i < period; i++)
            seed += values[i];
        var ema = seed / period;
        result[period - 1] = ema;

        var alpha = 2m / (period + 1m);
        for (var i = period; i < values.Count; i++)
        {
            ema = alpha * values[i] + (1m - alpha) * ema;
            result[i] = ema;
        }
        return result;
    }

    /// <summary>
    /// Welles Wilder ADX. Wilder-smooths +DM, −DM, TR with α = 1/period; computes
    /// +DI, −DI, DX; final ADX is Wilder-smoothed DX. First valid index is
    /// <c>2 × period − 1</c>; earlier positions hold 0m.
    /// </summary>
    public static decimal[] ComputeAdx(IReadOnlyList<Candle> candles, int period)
    {
        if (period < 2)
            throw new ArgumentOutOfRangeException(nameof(period));
        var n = candles.Count;
        var adx = new decimal[n];
        if (n < 2 * period)
            return adx;

        var plusDm = new decimal[n];
        var minusDm = new decimal[n];
        var tr = new decimal[n];

        for (var i = 1; i < n; i++)
        {
            var upMove = candles[i].High - candles[i - 1].High;
            var downMove = candles[i - 1].Low - candles[i].Low;
            plusDm[i] = upMove > downMove && upMove > 0m ? upMove : 0m;
            minusDm[i] = downMove > upMove && downMove > 0m ? downMove : 0m;
            var hl = candles[i].High - candles[i].Low;
            var hc = Math.Abs(candles[i].High - candles[i - 1].Close);
            var lc = Math.Abs(candles[i].Low - candles[i - 1].Close);
            tr[i] = Math.Max(hl, Math.Max(hc, lc));
        }

        var smPlusDm = 0m;
        var smMinusDm = 0m;
        var smTr = 0m;
        for (var i = 1; i <= period; i++)
        {
            smPlusDm += plusDm[i];
            smMinusDm += minusDm[i];
            smTr += tr[i];
        }

        var dx = new decimal[n];
        if (smTr > 0m)
        {
            var plusDi = 100m * smPlusDm / smTr;
            var minusDi = 100m * smMinusDm / smTr;
            var sum = plusDi + minusDi;
            dx[period] = sum > 0m ? 100m * Math.Abs(plusDi - minusDi) / sum : 0m;
        }

        for (var i = period + 1; i < n; i++)
        {
            smPlusDm = smPlusDm - smPlusDm / period + plusDm[i];
            smMinusDm = smMinusDm - smMinusDm / period + minusDm[i];
            smTr = smTr - smTr / period + tr[i];
            if (smTr <= 0m)
            {
                dx[i] = 0m;
                continue;
            }
            var plusDi = 100m * smPlusDm / smTr;
            var minusDi = 100m * smMinusDm / smTr;
            var sum = plusDi + minusDi;
            dx[i] = sum > 0m ? 100m * Math.Abs(plusDi - minusDi) / sum : 0m;
        }

        decimal adxSeed = 0m;
        for (var i = period; i < 2 * period; i++)
            adxSeed += dx[i];
        adx[2 * period - 1] = adxSeed / period;
        for (var i = 2 * period; i < n; i++)
            adx[i] = (adx[i - 1] * (period - 1) + dx[i]) / period;

        return adx;
    }
}
