namespace Jobsy.Core.Rules;

public static class DrivingLicenseLabels
{
    public static readonly string[] All =
    [
        "B", "A", "AM", "T", "Heftruck", "BE", "C", "CE", "D"
    ];

    public static string? Combine(IEnumerable<string>? labels)
    {
        if (labels is null)
        {
            return null;
        }

        var selected = labels
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => All.Contains(x!, StringComparer.OrdinalIgnoreCase))
            .Select(x => All.First(a => string.Equals(a, x, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => Array.IndexOf(All, x))
            .ToArray();

        return selected.Length == 0 ? null : string.Join(", ", selected);
    }

    public static string[] Split(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return [];
        }

        return stored
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => All.Contains(x, StringComparer.OrdinalIgnoreCase))
            .Select(x => All.First(a => string.Equals(a, x, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool CandidateMeetsRequirement(IEnumerable<string>? candidateLicenses, string? required)
    {
        var needed = Split(required);
        if (needed.Length == 0)
        {
            return true;
        }

        var have = new HashSet<string>(
            (candidateLicenses ?? []).Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        return needed.All(have.Contains);
    }
}
