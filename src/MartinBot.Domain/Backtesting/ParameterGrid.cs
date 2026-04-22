namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Cartesian product over a parameter grid: a map from parameter name to candidate values.
/// Empty / null grid yields a single empty combo (so a "defaults-only" sweep still runs once
/// through the normal per-combo path). Enumeration order follows the grid's key order.
/// </summary>
public static class ParameterGrid
{
    public static int CountCombinations(IReadOnlyDictionary<string, decimal[]>? grid)
    {
        if (grid is null || grid.Count == 0)
            return 1;
        var total = 1;
        foreach (var values in grid.Values)
        {
            if (values.Length == 0)
                return 0;
            total = checked(total * values.Length);
        }
        return total;
    }

    public static IEnumerable<IReadOnlyDictionary<string, decimal>> Cartesian(IReadOnlyDictionary<string, decimal[]>? grid)
    {
        if (grid is null || grid.Count == 0)
        {
            yield return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            yield break;
        }

        var keys = grid.Keys.ToArray();
        var axes = keys.Select(k => grid[k]).ToArray();
        foreach (var axis in axes)
        {
            if (axis.Length == 0)
                yield break;
        }

        var indices = new int[keys.Length];
        while (true)
        {
            var combo = new Dictionary<string, decimal>(keys.Length, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < keys.Length; i++)
                combo[keys[i]] = axes[i][indices[i]];
            yield return combo;

            var axis = keys.Length - 1;
            while (axis >= 0)
            {
                indices[axis]++;
                if (indices[axis] < axes[axis].Length)
                    break;
                indices[axis] = 0;
                axis--;
            }
            if (axis < 0)
                yield break;
        }
    }
}
