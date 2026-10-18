using Jobsy.Core.Careers;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Template-first neutral job titles and skill chips for sensitive work history editing.</summary>
public static class NeutralRoleSuggestions
{
    public const int MaxSuggestions = 3;
    public const int MaxSkills = 12;

    public static IReadOnlyList<string> SuggestTitles(string? role, string? description, string? employerName)
    {
        var seed = string.Join(' ',
            new[] { role, description, employerName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(seed))
        {
            return Array.Empty<string>();
        }

        var hits = OccupationCatalog.Shared.Search(seed, 5)
            .Select(h => h.Title)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(role))
        {
            hits.Insert(0, role.Trim());
        }

        var templates = TemplateTitles(seed);
        foreach (var t in templates)
        {
            if (!hits.Any(h => string.Equals(h, t, StringComparison.OrdinalIgnoreCase)))
            {
                hits.Add(t);
            }
        }

        return hits
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)
            .ToList();
    }

    public static IReadOnlyList<string> ExtractSkills(string? role, string? description)
    {
        var fromRole = OccupationDutySuggestions.ForRole(role ?? string.Empty, "nl");
        var merged = new List<string>(fromRole);
        if (!string.IsNullOrWhiteSpace(description))
        {
            foreach (var part in description.Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Length >= 3 && part.Length <= 40)
                {
                    merged.Add(part);
                }
            }
        }

        return merged
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSkills)
            .ToList();
    }

    public static CandidateEmployerHistoryDto ApplySkills(CandidateEmployerHistoryDto entry)
    {
        var skills = entry.Skills is { Count: > 0 }
            ? entry.Skills
            : ExtractSkills(entry.Role, entry.Description);
        return entry with { Skills = skills };
    }

    private static IEnumerable<string> TemplateTitles(string seed)
    {
        var fold = seed.ToLowerInvariant();
        if (fold.Contains("ondernemer", StringComparison.Ordinal)
            || fold.Contains("eigen bedrijf", StringComparison.Ordinal)
            || fold.Contains("zzp", StringComparison.Ordinal))
        {
            yield return "Zelfstandig ondernemer, persoonlijke dienstverlening";
            yield return "Zelfstandig ondernemer";
            yield return "Eigen onderneming in dienstverlening";
        }

        if (fold.Contains("vrijwillig", StringComparison.Ordinal))
        {
            yield return "Vrijwilligerswerk";
        }

        if (fold.Contains("zorg", StringComparison.Ordinal) && fold.Contains("familie", StringComparison.Ordinal))
        {
            yield return "Zorg voor familie";
        }
    }
}
