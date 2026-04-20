using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Domain.Backtesting.Strategies;

/// <summary>
/// DCA + mean-reversion (docs/strategies-research.md §7.1 Variant B).
/// Long-only spot. Enters when price is above a slow EMA (trend filter) AND RSI is below
/// <paramref name="entryRsi"/> (oversold). Adds up to <paramref name="maxTranches"/> tranches,
/// each gated by a further <paramref name="dcaDropPct"/> drop from the previous entry.
/// Liquidates fully when RSI rises above <paramref name="exitRsi"/> or a hard stop-loss on the
/// average entry is hit. All orders are market — fill tracking uses the current candle's open
/// (see <see cref="FillModel.ExecuteMarket"/>).
/// </summary>
public sealed class DcaMeanReversionStrategy : IStrategy
{
    private readonly decimal _feeBps;
    private readonly decimal _slippageBps;
    private readonly decimal _initialCash;
    private readonly int _emaPeriod;
    private readonly int _rsiPeriod;
    private readonly decimal _entryRsi;
    private readonly decimal _exitRsi;
    private readonly int _maxTranches;
    private readonly decimal _trancheFraction;
    private readonly decimal _dcaDropPct;
    private readonly decimal _stopLossPct;

    private decimal? _ema;
    private decimal _emaSeedSum;
    private int _emaSamples;
    private decimal? _rsi;
    private decimal _avgGain;
    private decimal _avgLoss;
    private decimal _gainSeedSum;
    private decimal _lossSeedSum;
    private int _rsiDeltas;
    private decimal _prevClose;
    private int _candleIndex;

    private decimal _previousPosition;
    private decimal _pendingBuyQty;
    private decimal _pendingSellQty;
    private int _tranchesFilled;
    private decimal _costBasisSum;
    private decimal _totalBoughtQty;
    private decimal _lastEntryPrice;

    public DcaMeanReversionStrategy(decimal feeBps, decimal slippageBps, decimal initialCash,
        int emaPeriod, int rsiPeriod, decimal entryRsi, decimal exitRsi,
        int maxTranches, decimal trancheFraction, decimal dcaDropPct, decimal stopLossPct)
    {
        if (emaPeriod < 2)
            throw new ArgumentOutOfRangeException(nameof(emaPeriod));
        if (rsiPeriod < 2)
            throw new ArgumentOutOfRangeException(nameof(rsiPeriod));
        if (maxTranches < 1)
            throw new ArgumentOutOfRangeException(nameof(maxTranches));
        if (trancheFraction <= 0m || trancheFraction > 1m)
            throw new ArgumentOutOfRangeException(nameof(trancheFraction));

        _feeBps = feeBps;
        _slippageBps = slippageBps;
        _initialCash = initialCash;
        _emaPeriod = emaPeriod;
        _rsiPeriod = rsiPeriod;
        _entryRsi = entryRsi;
        _exitRsi = exitRsi;
        _maxTranches = maxTranches;
        _trancheFraction = trancheFraction;
        _dcaDropPct = dcaDropPct;
        _stopLossPct = stopLossPct;
    }

    public IEnumerable<OrderIntent> OnCandle(Candle candle, IPortfolioView portfolio)
    {
        DetectFills(candle, portfolio);
        UpdateIndicators(candle);
        _candleIndex++;

        if (_ema is not { } ema || _rsi is not { } rsi)
            yield break;

        if (portfolio.Position > 0m)
        {
            var avgEntry = _totalBoughtQty > 0m ? _costBasisSum / _totalBoughtQty : candle.Close;
            var stopHit = candle.Close <= avgEntry * (1m - _stopLossPct);
            var exitSignal = rsi > _exitRsi;
            if (stopHit || exitSignal)
            {
                _pendingSellQty = portfolio.Position;
                yield return new OrderIntent(OrderSide.Sell, portfolio.Position, limitPrice: null);
                yield break;
            }
        }

        if (_tranchesFilled >= _maxTranches)
            yield break;
        if (candle.Close <= ema)
            yield break;
        if (rsi >= _entryRsi)
            yield break;
        if (_tranchesFilled > 0 && candle.Close > _lastEntryPrice * (1m - _dcaDropPct))
            yield break;

        var cashForTranche = _initialCash * _trancheFraction;
        var assumedFill = candle.Close * (1m + _slippageBps / 10_000m);
        var costPerUnit = assumedFill * (1m + _feeBps / 10_000m);
        if (costPerUnit <= 0m)
            yield break;

        var quantity = decimal.Floor(cashForTranche / costPerUnit * 1_000_000m) / 1_000_000m;
        if (quantity <= 0m)
            yield break;

        _pendingBuyQty = quantity;
        yield return new OrderIntent(OrderSide.Buy, quantity, limitPrice: null);
    }

    private void DetectFills(Candle candle, IPortfolioView portfolio)
    {
        var position = portfolio.Position;
        if (position > _previousPosition && _pendingBuyQty > 0m)
        {
            var fillPrice = candle.Open * (1m + _slippageBps / 10_000m);
            var filledQty = position - _previousPosition;
            _costBasisSum += fillPrice * filledQty;
            _totalBoughtQty += filledQty;
            _tranchesFilled++;
            _lastEntryPrice = fillPrice;
            _pendingBuyQty = 0m;
        }
        else if (position < _previousPosition && _pendingSellQty > 0m)
        {
            if (position == 0m)
            {
                _tranchesFilled = 0;
                _costBasisSum = 0m;
                _totalBoughtQty = 0m;
                _lastEntryPrice = 0m;
            }
            _pendingSellQty = 0m;
        }
        _previousPosition = position;
    }

    private void UpdateIndicators(Candle candle)
    {
        var close = candle.Close;

        if (_emaSamples < _emaPeriod)
        {
            _emaSeedSum += close;
            _emaSamples++;
            if (_emaSamples == _emaPeriod)
                _ema = _emaSeedSum / _emaPeriod;
        }
        else
        {
            var alpha = 2m / (_emaPeriod + 1m);
            _ema = alpha * close + (1m - alpha) * _ema!.Value;
        }

        if (_candleIndex == 0)
        {
            _prevClose = close;
            return;
        }

        var delta = close - _prevClose;
        var gain = delta > 0m ? delta : 0m;
        var loss = delta < 0m ? -delta : 0m;

        if (_rsiDeltas < _rsiPeriod)
        {
            _gainSeedSum += gain;
            _lossSeedSum += loss;
            _rsiDeltas++;
            if (_rsiDeltas == _rsiPeriod)
            {
                _avgGain = _gainSeedSum / _rsiPeriod;
                _avgLoss = _lossSeedSum / _rsiPeriod;
                _rsi = ComputeRsi(_avgGain, _avgLoss);
            }
        }
        else
        {
            _avgGain = (_avgGain * (_rsiPeriod - 1) + gain) / _rsiPeriod;
            _avgLoss = (_avgLoss * (_rsiPeriod - 1) + loss) / _rsiPeriod;
            _rsi = ComputeRsi(_avgGain, _avgLoss);
        }
        _prevClose = close;
    }

    private static decimal ComputeRsi(decimal avgGain, decimal avgLoss)
    {
        if (avgLoss == 0m)
            return 100m;
        var rs = avgGain / avgLoss;
        return 100m - 100m / (1m + rs);
    }
}
