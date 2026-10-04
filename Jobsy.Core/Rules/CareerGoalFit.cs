namespace Jobsy.Core.Rules;

/// <summary>
/// Candidate-only hint above the RIASEC list. It does not change match percents
/// and is never sent to employers or partners.
/// </summary>
public static class CareerGoalFit
{
    public sealed record Hit(string Title, bool EducationAbove);

    private static readonly string[] HigherEducationTitles =
    [
        "boekhouder",
        "software",
        "docent",
        "verpleegkundige",
        "ict-beheerder",
        "hr-medewerker",
        "lab- of meet",
        "marketingmedewerker"
    ];

    public static IReadOnlyList<Hit> Pick(
        IEnumerable<string> titles,
        string? dream,
        string? evidence,
        string? education,
        int take = 3)
    {
        var needles = Tokens($"{dream} {evidence}");
        if (needles.Count == 0)
        {
            return [];
        }

        var rank = EducationRank(education);
        var hits = new List<Hit>();
        foreach (var title in titles)
        {
            if (string.IsNullOrWhiteSpace(title) || hits.Any(h => string.Equals(h.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var folded = CareerOccupationKeys.Fold(title);
            if (!needles.Any(n => folded.Contains(n, StringComparison.Ordinal)))
            {
                continue;
            }

            hits.Add(new Hit(title.Trim(), EducationAbove(folded, rank)));
            if (hits.Count >= take)
            {
                break;
            }
        }

        return hits;
    }

    public static IReadOnlyList<string> AboveEducation(IEnumerable<string> titles, string? education)
    {
        var rank = EducationRank(education);
        if (rank is 0 or >= 5)
        {
            return [];
        }

        return titles
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Where(t => EducationAbove(CareerOccupationKeys.Fold(t), rank))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static int EducationRank(string? education)
    {
        var tokens = Tokens(education);
        if (tokens.Contains("wo") || tokens.Any(t => t.StartsWith("universit", StringComparison.Ordinal)))
        {
            return 6;
        }

        if (tokens.Contains("hbo"))
        {
            return 5;
        }

        if (tokens.Any(t => t.StartsWith("mbo", StringComparison.Ordinal)))
        {
            var text = string.Join(' ', tokens);
            if (text.Contains("mbo 4", StringComparison.Ordinal) || text.Contains("mbo4", StringComparison.Ordinal))
            {
                return 4;
            }

            if (text.Contains("mbo 3", StringComparison.Ordinal) || text.Contains("mbo3", StringComparison.Ordinal))
            {
                return 3;
            }

            return 2;
        }

        return 0;
    }

    private static bool EducationAbove(string foldedTitle, int rank)
        => rank is > 0 and <= 4
           && HigherEducationTitles.Any(h => foldedTitle.Contains(h, StringComparison.Ordinal));

    private static List<string> Tokens(string? text)
    {
        var folded = CareerOccupationKeys.Fold(text ?? "");
        return folded.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
