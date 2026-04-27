using MartinBot.Domain.Backtesting.Indicators;
using MartinBot.Domain.Backtesting.Models;

namespace MartinBot.Domain.Backtesting.RegimeSelector;

/// <summary>
/// MVP rule-based selector implementing the bear-régime guard mandated by Phase 6
/// (docs/phase6-experiments.md line 954): Pause when EMA(short) &lt; EMA(long) AND
/// ADX &gt; threshold. Defaults track docs/strategies.md §2 (50/200, ADX(14) &gt; 25).
/// Active-on-uncertainty: if there is not enough history to compute the indicators we
/// return Active (do not pause), so a config typo cannot silently sideline trading.
/// </summary>
public sealed class TrendDownPauseSelector : IRegimeSelector
{
    private readonly int _emaShortPeriod;
    private readonly int _emaLongPeriod;
    private readonly int _adxPeriod;
    private readonly decimal _adxThreshold;

    public TrendDownPauseSelector(int emaShortPeriod = 50, int emaLongPeriod = 200,
        int adxPeriod = 14, decimal adxThreshold = 25m)
    {
        if (emaShortPeriod < 2)
            throw new ArgumentOutOfRangeException(nameof(emaShortPeriod));
        if (emaLongPeriod <= emaShortPeriod)
            throw new ArgumentOutOfRangeException(nameof(emaLongPeriod));
        if (adxPeriod < 2)
            throw new ArgumentOutOfRangeException(nameof(adxPeriod));
        if (adxThreshold <= 0m)
            throw new ArgumentOutOfRangeException(nameof(adxThreshold));

        _emaShortPeriod = emaShortPeriod;
        _emaLongPeriod = emaLongPeriod;
        _adxPeriod = adxPeriod;
        _adxThreshold = adxThreshold;
    }

    public RegimeDecision Decide(IReadOnlyList<Candle> history)
    {
        var minHistory = Math.Max(_emaLongPeriod, 2 * _adxPeriod);
        if (history.Count < minHistory)
        {
            return new RegimeDecision(Regime.InsufficientHistory, shouldPause: false,
                $"insufficient history: have {history.Count} candles, need {minHistory}");
        }

        var closes = new decimal[history.Count];
        for (var i = 0; i < history.Count; i++)
            closes[i] = history[i].Close;

        var emaShortSeries = Indicators.Indicators.ComputeEma(closes, _emaShortPeriod);
        var emaLongSeries = Indicators.Indicators.ComputeEma(closes, _emaLongPeriod);
        var adxSeries = Indicators.Indicators.ComputeAdx(history, _adxPeriod);

        var emaShort = emaShortSeries[^1];
        var emaLong = emaLongSeries[^1];
        var adx = adxSeries[^1];

        if (emaShort < emaLong && adx > _adxThreshold)
        {
            return new RegimeDecision(Regime.TrendDown, shouldPause: true,
                $"TrendDown: EMA{_emaShortPeriod}={emaShort:F2} < EMA{_emaLongPeriod}={emaLong:F2}, ADX{_adxPeriod}={adx:F2} > {_adxThreshold}");
        }

        return new RegimeDecision(Regime.Active, shouldPause: false,
            $"Active: EMA{_emaShortPeriod}={emaShort:F2}, EMA{_emaLongPeriod}={emaLong:F2}, ADX{_adxPeriod}={adx:F2}");
    }
}
