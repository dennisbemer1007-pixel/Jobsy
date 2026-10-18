namespace Jobsy.Core.Careers;

/// <summary>Short duty phrases from ESCO occupation descriptions for onboarding chips.</summary>
public static class OccupationDutySuggestions
{
    public static IReadOnlyList<string> ForRole(string? role, int max = 4)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return [];
        }

        var occupation = OccupationCatalog.Shared.Resolve(role.Trim())
            ?? ResolveFromSearch(role.Trim());
        if (occupation is null || string.IsNullOrWhiteSpace(occupation.Desc))
        {
            return [];
        }

        return ParseChips(occupation.Desc, Math.Clamp(max, 1, 6));
    }

    private static Occupation? ResolveFromSearch(string role)
    {
        var hits = OccupationCatalog.Shared.Search(role, 1);
        if (hits.Count == 0)
        {
            return null;
        }

        return OccupationCatalog.Shared.Get(hits[0].EscoId);
    }

    private static IReadOnlyList<string> ParseChips(string description, int max)
    {
        var parts = description
            .Split([',', ';', '.', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeChip)
            .Where(part => part.Length is >= 10 and <= 56)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();

        return parts;
    }

    private static string NormalizeChip(string raw)
    {
        var trimmed = raw.Trim().TrimEnd(')', '(', '.', ';', ',');
        if (trimmed.Length > 56)
        {
            trimmed = trimmed[..53].TrimEnd() + "…";
        }

        return trimmed;
    }
}
