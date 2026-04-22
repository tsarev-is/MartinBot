using System.Text.Json;

namespace MartinBot.Domain.Backtesting;

/// <summary>
/// JSON encoding for the parameter grid stored on
/// <see cref="Entities.Models.ParameterSweepRunEntity.ParameterGridJson"/>.
/// Empty / null inputs round-trip to null so the column stays empty for a defaults-only sweep.
/// </summary>
public static class ParameterGridSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    public static string? Serialize(IReadOnlyDictionary<string, decimal[]>? grid)
    {
        if (grid is null || grid.Count == 0)
            return null;
        return JsonSerializer.Serialize(grid, Options);
    }

    public static IReadOnlyDictionary<string, decimal[]>? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        return JsonSerializer.Deserialize<Dictionary<string, decimal[]>>(json, Options);
    }
}
