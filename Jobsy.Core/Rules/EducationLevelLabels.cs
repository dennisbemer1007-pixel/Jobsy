namespace Jobsy.Core.Rules;

public static class EducationLevelLabels
{
    public const string None = "Geen";

    /// <summary>Levels a candidate can indicate on their profile.</summary>
    public static readonly string[] ProfileAll =
    [
        None, "LBO", "VMBO", "MBO", "HBO", "WO"
    ];

    /// <summary>Levels an employer can require on a vacancy (optional hard requirement).</summary>
    public static readonly string[] VacancyAll =
    [
        "LBO", "VMBO", "MBO", "HBO", "WO"
    ];

    public static string? Combine(IEnumerable<string>? labels, bool forVacancy = false)
    {
        if (labels is null)
        {
            return null;
        }

        var allowed = forVacancy ? VacancyAll : ProfileAll;
        var selected = labels
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => allowed.Contains(x!, StringComparer.OrdinalIgnoreCase))
            .Select(x => allowed.First(a => string.Equals(a, x, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => Array.IndexOf(allowed, x))
            .ToArray();

        return selected.Length == 0 ? null : string.Join(", ", selected);
    }

    public static string[] Split(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return [];
        }

        var allowed = ProfileAll;
        return stored
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => allowed.Contains(x, StringComparer.OrdinalIgnoreCase))
            .Select(x => allowed.First(a => string.Equals(a, x, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool CandidateMeetsRequirement(IEnumerable<string>? candidateEducations, string? required)
    {
        var needed = Split(required)
            .Where(x => !string.Equals(x, None, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (needed.Length == 0)
        {
            return true;
        }

        var have = new HashSet<string>(
            (candidateEducations ?? []).Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        // Vacancy lists accepted levels: candidate needs at least one match.
        return needed.Any(have.Contains);
    }
}
