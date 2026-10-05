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
        var items = new List<(string Label, int Percent)>();
        foreach (var code in CompetencyTestCatalog.QuickScanCategories)
        {
            items.Add((EverydayCompetency(code, language), competency.Get(code)));
        }

        foreach (var code in CulturePersonalityCatalog.CategoryCodes)
        {
            items.Add((DimensionLabels.For(code, language), culture.Get(code)));
        }

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            items.Add((DimensionLabels.For(code, language), career.Get(code)));
        }

        if (values is { IsComplete: true })
        {
            foreach (var code in SchwartzValuesCatalog.CategoryCodes)
            {
                items.Add((DimensionLabels.For(code, language), values.Get(code)));
            }
        }

        return items
            .Where(x => x.Percent >= 55 && !CareerCompassBuilder.ContainsForbiddenJargon(x.Label))
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Label)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxCount)
            .ToList();
    }

    public static string EverydayCompetency(string code, string? language = null)
        => CompetencyTestCatalog.QuickScanCategories.Contains(code, StringComparer.OrdinalIgnoreCase)
            ? DimensionLabels.For(code, language)
            : "werksterkte";
}
