using System.Text.Json;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// JSON encoding for strategy parameter overrides stored on
/// <see cref="Entities.Models.BacktestRunEntity.StrategyParametersJson"/>.
/// Empty / null inputs round-trip to null so the column stays empty when no overrides are supplied.
/// </summary>
public static class StrategyParametersSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    public static string? Serialize(IReadOnlyDictionary<string, decimal>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
            return null;
        return JsonSerializer.Serialize(parameters, Options);
    }

    public static IReadOnlyDictionary<string, decimal>? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        return JsonSerializer.Deserialize<Dictionary<string, decimal>>(json, Options);
    }
}
