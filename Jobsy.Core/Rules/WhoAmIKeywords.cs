namespace Jobsy.Core.Rules;

public static class WhoAmIKeywords
{
    public const int MaxCount = 8;

    public static IReadOnlyList<string> FromScores(
        CompetencyScores competency,
        RiasecScores career,
        DiscScores disc)
    {
        var items = new List<(string Label, int Percent)>();
        foreach (var code in CompetencyTestCatalog.CategoryCodes)
        {
            items.Add((EverydayCompetency(code), competency.Get(code)));
        }

        foreach (var code in DiscTestCatalog.CategoryCodes)
        {
            items.Add((DiscTestCatalog.EverydayLabel(code), disc.Get(code)));
        }

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            items.Add((CareerCompassBuilder.TypeLabel(code), career.Get(code)));
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

    public static string EverydayCompetency(string code) => code switch
    {
        CompetencyTestCatalog.Samenwerken => "samenwerken",
        CompetencyTestCatalog.Resultaatgerichtheid => "resultaat halen",
        CompetencyTestCatalog.Stressbestendigheid => "rust onder druk",
        CompetencyTestCatalog.Innovatie => "nieuwe wegen zoeken",
        _ => "werksterkte"
    };
}
