namespace Jobsy.Core.Rules;

public static class WhoAmIKeywords
{
    public const int MaxCount = 8;

    public static IReadOnlyList<string> FromScores(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        SchwartzValuesScores? values = null,
        string? language = null)
    {
        return Codes(competency, career, culture, values)
            .Select(code => LabelFor(code, language))
            .Where(label => !CareerCompassBuilder.ContainsForbiddenJargon(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxCount)
            .ToList();
    }

    /// <summary>
    /// Same codes in every language. Order is score, then the stable code.
    /// A score under 50 is never a keyword.
    /// </summary>
    public static IReadOnlyList<string> Codes(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        SchwartzValuesScores? values = null)
    {
        var items = new List<(string Code, int Percent)>();
        foreach (var code in CompetencyTestCatalog.QuickScanCategories)
        {
            items.Add((code, competency.Get(code)));
        }

        foreach (var code in CulturePersonalityCatalog.CategoryCodes)
        {
            items.Add((code, culture.Get(code)));
        }

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            items.Add((code, career.Get(code)));
        }

        if (values is { IsComplete: true })
        {
            foreach (var code in SchwartzValuesCatalog.CategoryCodes)
            {
                items.Add((code, values.Get(code)));
            }
        }

        return items
            .Where(x => x.Percent >= 50)
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .Select(x => x.Code)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxCount)
            .ToList();
    }

    private static string LabelFor(string code, string? language)
        => CompetencyTestCatalog.QuickScanCategories.Contains(code, StringComparer.OrdinalIgnoreCase)
            ? EverydayCompetency(code, language)
            : DimensionLabels.For(code, language);

    public static string EverydayCompetency(string code, string? language = null)
        => CompetencyTestCatalog.QuickScanCategories.Contains(code, StringComparer.OrdinalIgnoreCase)
            ? DimensionLabels.For(code, language)
            : "werksterkte";
}
