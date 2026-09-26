namespace Jobsy.Core.Rules;

public static class EducationLevelLabels
{
    public const string None = "Geen";

    /// <summary>Levels a candidate can indicate on their profile / wizard.</summary>
    public static readonly string[] ProfileAll =
    [
        None,
        "Basisschool",
        "LBO",
        "VMBO",
        "HAVO",
        "VWO",
        "MBO 1",
        "MBO 2",
        "MBO 3",
        "MBO 4",
        "MBO",
        "HBO",
        "WO"
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
            .Select(x => NormalizeToken(x!, forVacancy))
            .Where(x => x is not null && allowed.Contains(x, StringComparer.OrdinalIgnoreCase))
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
        var parts = stored.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<string>();
        foreach (var part in parts)
        {
            var token = ExtractLevel(part);
            if (token is null)
            {
                continue;
            }

            var match = allowed.FirstOrDefault(a => string.Equals(a, token, StringComparison.OrdinalIgnoreCase));
            if (match is not null && !result.Contains(match, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(match);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Extracts a known level from a free-text education line such as "HAVO – E&amp;M".
    /// </summary>
    public static string? ExtractLevel(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var trimmed = line.Trim();
        // Prefer longer tokens first (MBO 1 before MBO).
        foreach (var level in ProfileAll.OrderByDescending(l => l.Length))
        {
            if (trimmed.Equals(level, StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(level + " ", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(level + "–", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(level + "-", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(level + " –", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(level + " -", StringComparison.OrdinalIgnoreCase))
            {
                return level;
            }
        }

        return null;
    }

    /// <summary>Maps candidate levels onto vacancy requirement buckets (MBO 1–4 → MBO).</summary>
    public static string ToVacancyBucket(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return "";
        }

        var extracted = ExtractLevel(level) ?? level.Trim();
        if (extracted.StartsWith("MBO ", StringComparison.OrdinalIgnoreCase))
        {
            return "MBO";
        }

        return extracted;
    }

    public static bool CandidateMeetsRequirement(IEnumerable<string>? candidateEducations, string? required)
    {
        var needed = Split(required)
            .Where(x => !string.Equals(x, None, StringComparison.OrdinalIgnoreCase))
            .Select(ToVacancyBucket)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (needed.Length == 0)
        {
            return true;
        }

        var have = new HashSet<string>(
            (candidateEducations ?? [])
                .Select(ToVacancyBucket)
                .Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        // Vacancy lists accepted levels: candidate needs at least one match.
        return needed.Any(have.Contains);
    }

    private static string? NormalizeToken(string value, bool forVacancy)
    {
        var extracted = ExtractLevel(value) ?? value.Trim();
        return forVacancy ? ToVacancyBucket(extracted) : extracted;
    }
}
