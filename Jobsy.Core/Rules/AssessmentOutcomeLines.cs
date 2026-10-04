namespace Jobsy.Core.Rules;

/// <summary>One-line Dutch outcome summaries for completed free/deep tests (tile + DNA).</summary>
public static class AssessmentOutcomeLines
{
    public static string? Competence(
        int? samenwerken,
        int? resultaatgerichtheid,
        int? stressbestendigheid,
        int? innovatie,
        int? extraversie)
    {
        var ranked = new (string Label, int? Value)[]
        {
            ("samenwerken", samenwerken),
            ("resultaatgerichtheid", resultaatgerichtheid),
            ("stressbestendigheid", stressbestendigheid),
            ("innovatie", innovatie),
            ("extraversie", extraversie)
        }
            .Where(x => x.Value is not null)
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Label, StringComparer.Ordinal)
            .ToList();

        if (ranked.Count == 0)
        {
            return null;
        }

        if (ranked.Count > 1 && ranked[0].Value == ranked[1].Value)
        {
            return null;
        }

        return $"Sterkst: {FriendlyCompetence(ranked[0].Label)}";
    }

    public static string? Career(
        int? realistic,
        int? investigative,
        int? artistic,
        int? social,
        int? enterprising,
        int? conventional)
    {
        var line = RiasecRanking.FormatCareerOutcomeLine(
            realistic, investigative, artistic, social, enterprising, conventional);
        return string.IsNullOrWhiteSpace(line) ? null : line;
    }

    public static string? Culture(
        int? autonomy,
        int? informal,
        int? collaboration,
        int? flexibility,
        int? innovation,
        int? peopleFirst)
    {
        var culture = DimensionRanking.Rank(
            [
                (CulturePersonalityCatalog.Autonomy, autonomy),
                (CulturePersonalityCatalog.Informal, informal),
                (CulturePersonalityCatalog.Collaboration, collaboration),
                (CulturePersonalityCatalog.Flexibility, flexibility),
                (CulturePersonalityCatalog.Innovation, innovation),
                (CulturePersonalityCatalog.PeopleFirst, peopleFirst)
            ],
            DimensionRanking.CultureTieBreak)
            .Take(2)
            .Select(x => CulturePersonalityCatalog.EverydayLabel(x.Code))
            .ToList();

        return culture.Count == 0
            ? null
            : string.Join(", ", culture);
    }

    public static string? Values(
        int? autonomy,
        int? connection,
        int? achievement,
        int? stability,
        int? impact)
    {
        var ranked = DimensionRanking.Rank(
            [
                (SchwartzValuesCatalog.Autonomy, autonomy),
                (SchwartzValuesCatalog.Connection, connection),
                (SchwartzValuesCatalog.Achievement, achievement),
                (SchwartzValuesCatalog.Stability, stability),
                (SchwartzValuesCatalog.Impact, impact)
            ],
            DimensionRanking.ValueTieBreak).ToList();

        if (ranked.Count == 0)
        {
            return null;
        }

        if (ranked.Count > 1 && ranked[0].Score == ranked[1].Score)
        {
            return null;
        }

        return $"Prioriteit: {DimensionLabels.For(ranked[0].Code)}";
    }

    private static string FriendlyCompetence(string label) => label switch
    {
        "extraversie" => "Energie van mensen",
        "samenwerken" => "Samenwerken",
        "resultaatgerichtheid" => "Afronden",
        "stressbestendigheid" => "Rust onder druk",
        "innovatie" => "Nieuwe ideeën",
        _ => label
    };
}
