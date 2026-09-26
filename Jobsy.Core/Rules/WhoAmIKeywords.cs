namespace Jobsy.Core.Rules;

public static class WhoAmIKeywords
{
    public const int MaxCount = 8;

    public static IReadOnlyList<string> FromScores(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        SchwartzValuesScores? values = null)
    {
        var items = new List<(string Label, int Percent)>();
        foreach (var code in CompetencyTestCatalog.CategoryCodes)
        {
            items.Add((EverydayCompetency(code), competency.Get(code)));
        }

        foreach (var code in CulturePersonalityCatalog.CategoryCodes)
        {
            items.Add((CulturePersonalityCatalog.EverydayLabel(code), culture.Get(code)));
        }

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            items.Add((CareerCompassBuilder.TypeLabel(code), career.Get(code)));
        }

        if (values is { IsComplete: true })
        {
            foreach (var code in SchwartzValuesCatalog.CategoryCodes)
            {
                items.Add((SchwartzValuesCatalog.EverydayLabel(code), values.Get(code)));
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

    public static string EverydayCompetency(string code)
        => CompetencyTestCatalog.CategoryCodes.Contains(code, StringComparer.OrdinalIgnoreCase)
            ? DimensionLabels.For(code)
            : "werksterkte";
}
