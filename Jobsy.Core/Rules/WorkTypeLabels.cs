using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class WorkTypeLabels
{
    public const int MaxPerVacancy = 2;

    public const string Horeca = "Horeca";
    public const string Winkel = "Winkel";
    public const string Logistiek = "Logistiek";
    public const string Tuinbouw = "Tuinbouw";
    public const string Zorg = "Zorg";
    public const string Kantoor = "Kantoor";
    public const string Bouw = "Bouw";
    public const string Schoonmaak = "Schoonmaak";
    public const string Productie = "Productie";

    public static readonly string[] All =
    [
        Horeca, Winkel, Logistiek, Tuinbouw, Zorg, Kantoor, Bouw, Schoonmaak, Productie
    ];

    public static WorkType Parse(string? label) => label?.Trim() switch
    {
        Horeca or "horeca" => WorkType.Horeca,
        Winkel or "winkel" or "retail" => WorkType.Winkel,
        Logistiek or "logistiek" => WorkType.Logistiek,
        Tuinbouw or "tuinbouw" => WorkType.Tuinbouw,
        Zorg or "zorg" => WorkType.Zorg,
        Kantoor or "kantoor" => WorkType.Kantoor,
        Bouw or "bouw" => WorkType.Bouw,
        Schoonmaak or "schoonmaak" => WorkType.Schoonmaak,
        Productie or "productie" => WorkType.Productie,
        _ => WorkType.None
    };

    public static string[] Expand(WorkType types)
    {
        var labels = new List<string>();
        if (types.HasFlag(WorkType.Horeca)) labels.Add(Horeca);
        if (types.HasFlag(WorkType.Winkel)) labels.Add(Winkel);
        if (types.HasFlag(WorkType.Logistiek)) labels.Add(Logistiek);
        if (types.HasFlag(WorkType.Tuinbouw)) labels.Add(Tuinbouw);
        if (types.HasFlag(WorkType.Zorg)) labels.Add(Zorg);
        if (types.HasFlag(WorkType.Kantoor)) labels.Add(Kantoor);
        if (types.HasFlag(WorkType.Bouw)) labels.Add(Bouw);
        if (types.HasFlag(WorkType.Schoonmaak)) labels.Add(Schoonmaak);
        if (types.HasFlag(WorkType.Productie)) labels.Add(Productie);
        return labels.ToArray();
    }

    public static WorkType Combine(IEnumerable<string>? labels)
    {
        var combined = WorkType.None;
        if (labels is null)
        {
            return combined;
        }

        foreach (var label in labels)
        {
            combined |= Parse(label);
        }

        return combined;
    }

    public static int CountFlags(WorkType types)
    {
        var n = (uint)types;
        var count = 0;
        while (n != 0)
        {
            n &= n - 1;
            count++;
        }

        return count;
    }

    public static bool IsValidSelection(WorkType types)
        => types != WorkType.None && CountFlags(types) <= MaxPerVacancy;

    public static bool MatchesFilter(WorkType vacancyTypes, string? selectedLabel)
        => MatchesFilter(vacancyTypes, storedLabels: null, selectedLabels: WrapLabel(selectedLabel));

    public static bool MatchesFilter(WorkType vacancyTypes, string? storedLabels, string? selectedLabel)
        => MatchesFilter(vacancyTypes, storedLabels, selectedLabels: WrapLabel(selectedLabel));

    private static IEnumerable<string>? WrapLabel(string? selectedLabel)
        => string.IsNullOrWhiteSpace(selectedLabel) ? null : [selectedLabel];

    /// <summary>
    /// True when no filter is set, or the vacancy matches any selected branch (OR).
    /// Accepts a single label, repeated query values, or comma-separated lists.
    /// </summary>
    public static bool MatchesFilter(
        WorkType vacancyTypes,
        string? storedLabels,
        IEnumerable<string>? selectedLabels)
    {
        var selected = NormalizeFilterLabels(selectedLabels);
        if (selected.Length == 0)
        {
            return true;
        }

        return selected.Any(label => MatchesSingleFilter(vacancyTypes, storedLabels, label));
    }

    public static string[] NormalizeFilterLabels(IEnumerable<string>? selectedLabels)
    {
        if (selectedLabels is null)
        {
            return [];
        }

        return selectedLabels
            .SelectMany(SplitStored)
            .Select(NormalizeKnownLabel)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? NormalizeKnownLabel(string raw)
    {
        var fromFlags = Expand(Parse(raw)).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fromFlags))
        {
            return fromFlags;
        }

        return All.FirstOrDefault(l => l.Equals(raw.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesSingleFilter(WorkType vacancyTypes, string? storedLabels, string selectedLabel)
    {
        var labels = ResolveLabels(vacancyTypes, storedLabels);
        if (labels.Any(l => string.Equals(l, selectedLabel, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var required = Parse(selectedLabel);
        if (required == WorkType.None)
        {
            return false;
        }

        return vacancyTypes.HasFlag(required);
    }

    public static string[] ResolveLabels(WorkType vacancyTypes, string? storedLabels)
    {
        var fromStore = SplitStored(storedLabels);
        if (fromStore.Length > 0)
        {
            return fromStore;
        }

        return Expand(vacancyTypes);
    }

    public static string? CombineStored(IEnumerable<string>? labels)
    {
        if (labels is null)
        {
            return null;
        }

        var selected = labels
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxPerVacancy)
            .ToArray();

        return selected.Length == 0 ? null : string.Join(", ", selected!);
    }

    public static string[] SplitStored(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return [];
        }

        return stored
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
