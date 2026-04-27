using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Backtesting.Models;
using MartinBot.Domain.Backtesting.Strategies;

namespace MartinBot.Backtesting;

/// <summary>
/// Resolves a strategy instance by the name persisted in <c>BacktestRunEntity.StrategyName</c>.
/// Accepts an optional per-run parameter override dictionary; missing keys fall back to the
/// per-strategy defaults declared below. Unknown keys are rejected so typos surface at request
/// time rather than silently using the default.
/// </summary>
public sealed class BacktestStrategyFactory
{
    public const string BuyAndHold = "buy_and_hold";
    public const string DcaMeanReversion = "dca_mr";
    public const string Grid = "grid";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, decimal>> DefaultsByStrategy =
        new Dictionary<string, IReadOnlyDictionary<string, decimal>>(StringComparer.Ordinal)
        {
            [BuyAndHold] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
            [DcaMeanReversion] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["emaPeriod"] = 200m,
                ["rsiPeriod"] = 14m,
                ["entryRsi"] = 30m,
                ["exitRsi"] = 50m,
                ["maxTranches"] = 3m,
                ["trancheFraction"] = 0.25m,
                ["dcaDropPct"] = 0.03m,
                ["stopLossPct"] = 0.10m
            },
            [Grid] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["channelLookback"] = 100m,
                ["gridLevels"] = 10m,
                ["gridBudgetFraction"] = 0.5m,
                ["invalidationPct"] = 0.02m,
                ["lowQuantile"] = 0.10m,
                ["highQuantile"] = 0.90m,
                ["cooldownCandles"] = 24m
            }
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> IntegerKeysByStrategy =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [BuyAndHold] = Array.Empty<string>(),
            [DcaMeanReversion] = new[] { "emaPeriod", "rsiPeriod", "maxTranches" },
            [Grid] = new[] { "channelLookback", "gridLevels", "cooldownCandles" }
        };

    public IReadOnlyDictionary<string, decimal> GetDefaults(string strategyName)
    {
        if (!DefaultsByStrategy.TryGetValue(strategyName, out var defaults))
            throw new ArgumentException($"Unknown strategy: {strategyName}");
        return defaults;
    }

    public void ValidateParameters(string strategyName, IReadOnlyDictionary<string, decimal>? parameters)
        => EnsureKnownKeys(GetDefaults(strategyName), parameters, strategyName);

    public IStrategy Create(string name, BacktestRequest request,
        IReadOnlyDictionary<string, decimal>? parameters = null)
    {
        var defaults = GetDefaults(name);
        EnsureKnownKeys(defaults, parameters, name);
        decimal Get(string key) =>
            parameters is not null && parameters.TryGetValue(key, out var v) ? v : defaults[key];

        return name switch
        {
            BuyAndHold => new BuyAndHoldStrategy(request.FeeBps, request.SlippageBps),
            DcaMeanReversion => new DcaMeanReversionStrategy(request.FeeBps, request.SlippageBps, request.InitialCash,
                emaPeriod: (int)Get("emaPeriod"), rsiPeriod: (int)Get("rsiPeriod"),
                entryRsi: Get("entryRsi"), exitRsi: Get("exitRsi"),
                maxTranches: (int)Get("maxTranches"), trancheFraction: Get("trancheFraction"),
                dcaDropPct: Get("dcaDropPct"), stopLossPct: Get("stopLossPct")),
            Grid => new GridStrategy(request.FeeBps, request.SlippageBps, request.InitialCash,
                channelLookback: (int)Get("channelLookback"), gridLevels: (int)Get("gridLevels"),
                gridBudgetFraction: Get("gridBudgetFraction"), invalidationPct: Get("invalidationPct"),
                lowQuantile: Get("lowQuantile"), highQuantile: Get("highQuantile"),
                cooldownCandles: (int)Get("cooldownCandles")),
            _ => throw new ArgumentException($"Unknown strategy: {name}")
        };
    }

    private static void EnsureKnownKeys(IReadOnlyDictionary<string, decimal> defaults,
        IReadOnlyDictionary<string, decimal>? parameters, string strategyName)
    {
        if (parameters is null)
            return;
        foreach (var key in parameters.Keys)
        {
            if (!defaults.ContainsKey(key))
                throw new ArgumentException($"Unknown parameter '{key}' for strategy '{strategyName}'");
        }
        if (IntegerKeysByStrategy.TryGetValue(strategyName, out var intKeys))
        {
            foreach (var key in intKeys)
            {
                if (parameters.TryGetValue(key, out var value) && value != decimal.Truncate(value))
                    throw new ArgumentException($"Parameter '{key}' for strategy '{strategyName}' must be an integer, got {value}");
            }
        }
    }
}
