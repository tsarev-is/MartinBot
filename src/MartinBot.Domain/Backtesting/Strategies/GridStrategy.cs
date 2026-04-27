using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Models;

namespace MartinBot.Domain.Backtesting.Strategies;

/// <summary>
/// Grid trading (docs/strategies-research.md §3.3 / §7.2, docs/strategies.md §3.1,
/// docs/strategies-roadmap-next.md §8b). Long-only spot. After a warmup of
/// <paramref name="channelLookback"/> closes, computes a quantile channel
/// [<paramref name="lowQuantile"/>, <paramref name="highQuantile"/>] and places
/// <paramref name="gridLevels"/> evenly-spaced limit-buy orders strictly below the
/// current price. On each buy fill, emits a paired sell one step above; on each sell
/// fill, re-arms a buy at the just-vacated level. On price escaping
/// range_{low,high} × (1 ± invalidationPct) at close, market-sells the position and
/// cancels every pending limit. V2: invalidation is transient — after
/// <paramref name="cooldownCandles"/> candles of dead time, a fresh warmup buffer is
/// rebuilt from post-invalidation closes and the channel re-initializes (see
/// docs/strategies.md §4.3 cooldown semantics, docs/phase6-experiments.md run 10/11
/// for the rationale: V1's absorbing _stopped was incompatible with walk-forward's
/// combined-run warmup-fix).
/// </summary>
public sealed class GridStrategy : IStrategy
{
    private enum LevelState
    {
        Empty,
        OpenBuy,
        Filled,
        OpenSell
    }

    private readonly decimal _feeBps;
    private readonly decimal _slippageBps;
    private readonly decimal _initialCash;
    private readonly int _channelLookback;
    private readonly int _gridLevels;
    private readonly decimal _gridBudgetFraction;
    private readonly decimal _invalidationPct;
    private readonly decimal _lowQuantile;
    private readonly decimal _highQuantile;
    private readonly int _cooldownCandles;

    private readonly Queue<decimal> _warmupBuffer;
    private readonly Dictionary<int, OrderIntent> _openByLevel = new();

    private bool _initialized;
    private int _cooldownRemaining;
    private decimal _rangeLow;
    private decimal _rangeHigh;
    private decimal _step;
    private decimal[] _levels = Array.Empty<decimal>();
    private decimal[] _qtyPerLevel = Array.Empty<decimal>();
    private LevelState[] _levelStates = Array.Empty<LevelState>();

    public GridStrategy(decimal feeBps, decimal slippageBps, decimal initialCash,
        int channelLookback, int gridLevels, decimal gridBudgetFraction, decimal invalidationPct,
        decimal lowQuantile, decimal highQuantile, int cooldownCandles)
    {
        if (channelLookback < 2)
            throw new ArgumentOutOfRangeException(nameof(channelLookback));
        if (gridLevels < 2)
            throw new ArgumentOutOfRangeException(nameof(gridLevels));
        if (gridBudgetFraction <= 0m || gridBudgetFraction > 1m)
            throw new ArgumentOutOfRangeException(nameof(gridBudgetFraction));
        if (invalidationPct <= 0m || invalidationPct >= 1m)
            throw new ArgumentOutOfRangeException(nameof(invalidationPct));
        if (lowQuantile <= 0m || highQuantile >= 1m || lowQuantile >= highQuantile)
            throw new ArgumentOutOfRangeException(nameof(lowQuantile));
        if (initialCash <= 0m)
            throw new ArgumentOutOfRangeException(nameof(initialCash));
        if (cooldownCandles < 0)
            throw new ArgumentOutOfRangeException(nameof(cooldownCandles));

        _feeBps = feeBps;
        _slippageBps = slippageBps;
        _initialCash = initialCash;
        _channelLookback = channelLookback;
        _gridLevels = gridLevels;
        _gridBudgetFraction = gridBudgetFraction;
        _invalidationPct = invalidationPct;
        _lowQuantile = lowQuantile;
        _highQuantile = highQuantile;
        _cooldownCandles = cooldownCandles;
        _warmupBuffer = new Queue<decimal>(channelLookback);
    }

    public IEnumerable<OrderIntent> OnCandle(Candle candle, IPortfolioView portfolio)
    {
        if (_cooldownRemaining > 0)
        {
            _cooldownRemaining--;
            yield break;
        }

        if (!_initialized)
        {
            _warmupBuffer.Enqueue(candle.Close);
            if (_warmupBuffer.Count < _channelLookback)
                yield break;

            // Init failed (degenerate range / profitability gate): drop oldest close and
            // retry on the next candle with a one-bar slide. This keeps the strategy
            // alive across regimes where a tighter channel only becomes profitable later.
            if (!TryInitializeGrid())
            {
                _warmupBuffer.Dequeue();
                yield break;
            }

            _initialized = true;
            for (var i = 0; i < _gridLevels; i++)
            {
                if (_levels[i] >= candle.Close)
                    continue;
                var intent = new OrderIntent(OrderSide.Buy, _qtyPerLevel[i], _levels[i]);
                _openByLevel[i] = intent;
                _levelStates[i] = LevelState.OpenBuy;
                yield return intent;
            }
            yield break;
        }

        // Detect fills: an intent we previously emitted that no longer appears in the
        // engine's open-limit book has been executed (BacktestEngine.FillTouchedLimits
        // removes filled intents before calling OnCandle on the same candle).
        var openSet = new HashSet<OrderIntent>(portfolio.OpenLimitOrders);
        var filledLevels = new List<int>(_openByLevel.Count);
        foreach (var (level, intent) in _openByLevel)
        {
            if (!openSet.Contains(intent))
                filledLevels.Add(level);
        }
        foreach (var level in filledLevels)
            _openByLevel.Remove(level);

        // Invalidation must run before pair emission: if we breach now, any newly-emitted
        // limits would be queued by the engine *after* our cancel call and slip past the
        // stop. So we yield-break and don't emit pairs once invalidated. V2: invalidation
        // is transient — we reset state and enter cooldown so a fresh channel can rebuild
        // from post-invalidation closes (docs/strategies.md §4.3).
        var breachDown = candle.Close < _rangeLow * (1m - _invalidationPct);
        var breachUp = candle.Close > _rangeHigh * (1m + _invalidationPct);
        if (breachDown || breachUp)
        {
            if (portfolio.Position > 0m)
                yield return new OrderIntent(OrderSide.Sell, portfolio.Position, limitPrice: null);
            var toCancel = _openByLevel.Values.ToArray();
            foreach (var pending in toCancel)
                portfolio.Cancel(pending);
            _openByLevel.Clear();
            _initialized = false;
            _warmupBuffer.Clear();
            _cooldownRemaining = _cooldownCandles;
            yield break;
        }

        foreach (var level in filledLevels)
        {
            var prevState = _levelStates[level];
            if (prevState == LevelState.OpenBuy)
            {
                _levelStates[level] = LevelState.Filled;
                if (level + 1 <= _gridLevels - 1)
                {
                    var sell = new OrderIntent(OrderSide.Sell, _qtyPerLevel[level], _levels[level + 1]);
                    _openByLevel[level] = sell;
                    _levelStates[level] = LevelState.OpenSell;
                    yield return sell;
                }
            }
            else if (prevState == LevelState.OpenSell)
            {
                _levelStates[level] = LevelState.Empty;
                var buy = new OrderIntent(OrderSide.Buy, _qtyPerLevel[level], _levels[level]);
                _openByLevel[level] = buy;
                _levelStates[level] = LevelState.OpenBuy;
                yield return buy;
            }
        }
    }

    private bool TryInitializeGrid()
    {
        var sorted = _warmupBuffer.OrderBy(x => x).ToArray();
        _rangeLow = Quantile(sorted, _lowQuantile);
        _rangeHigh = Quantile(sorted, _highQuantile);
        if (_rangeHigh <= _rangeLow)
            return false;

        var step = (_rangeHigh - _rangeLow) / (_gridLevels - 1);
        var midPrice = (_rangeLow + _rangeHigh) / 2m;

        // Profitability gate: a buy/sell pair across one step at midPrice clears
        // 2 × fee × midPrice in fees; pair gross = step − 2×fee×midPrice. Limits
        // execute exactly at LimitPrice with zero slippage (FillModel.TryFillLimit),
        // so slippage is not part of the gate. See docs/strategies-research.md §3.3.
        var pairFeeAtMid = 2m * (_feeBps / 10_000m) * midPrice;
        if (step <= pairFeeAtMid)
            return false;

        _step = step;
        _levels = new decimal[_gridLevels];
        _qtyPerLevel = new decimal[_gridLevels];
        _levelStates = new LevelState[_gridLevels];
        var perLevelBudget = _initialCash * _gridBudgetFraction / _gridLevels;
        for (var i = 0; i < _gridLevels; i++)
        {
            _levels[i] = _rangeLow + i * _step;
            _qtyPerLevel[i] = perLevelBudget / _levels[i];
            _levelStates[i] = LevelState.Empty;
        }
        return true;
    }

    private static decimal Quantile(decimal[] sorted, decimal q)
    {
        if (sorted.Length == 0)
            return 0m;
        if (sorted.Length == 1)
            return sorted[0];
        var rank = q * (sorted.Length - 1);
        var lower = (int)Math.Floor(rank);
        var upper = (int)Math.Ceiling(rank);
        if (lower == upper)
            return sorted[lower];
        var weight = rank - lower;
        return sorted[lower] * (1m - weight) + sorted[upper] * weight;
    }
}
