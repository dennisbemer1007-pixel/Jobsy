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

        return ranked.Count == 0 ? null : $"Sterkst: {ranked[0].Label}";
    }

    public static string? Career(
        int? realistic,
        int? investigative,
        int? artistic,
        int? social,
        int? enterprising,
        int? conventional)
    {
        var ranked = new (string Code, int? Value)[]
        {
            (CareerTestCatalog.Realistic, realistic),
            (CareerTestCatalog.Investigative, investigative),
            (CareerTestCatalog.Artistic, artistic),
            (CareerTestCatalog.Social, social),
            (CareerTestCatalog.Enterprising, enterprising),
            (CareerTestCatalog.Conventional, conventional)
        }
            .Where(x => x.Value is not null)
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToList();

        return ranked.Count == 0
            ? null
            : $"Richting: {CareerCompassBuilder.TypeLabel(ranked[0].Code).ToLowerInvariant()}";
    }

    public static string? Culture(
        int? autonomy,
        int? informal,
        int? collaboration,
        int? flexibility,
        int? innovation,
        int? peopleFirst)
    {
        var culture = new (string Code, int? Value)[]
        {
            (CulturePersonalityCatalog.Autonomy, autonomy),
            (CulturePersonalityCatalog.Informal, informal),
            (CulturePersonalityCatalog.Collaboration, collaboration),
            (CulturePersonalityCatalog.Flexibility, flexibility),
            (CulturePersonalityCatalog.Innovation, innovation),
            (CulturePersonalityCatalog.PeopleFirst, peopleFirst)
        }
            .Where(x => x.Value is not null)
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
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
        var ranked = new (string Code, int? Value)[]
        {
            (SchwartzValuesCatalog.Autonomy, autonomy),
            (SchwartzValuesCatalog.Connection, connection),
            (SchwartzValuesCatalog.Achievement, achievement),
            (SchwartzValuesCatalog.Stability, stability),
            (SchwartzValuesCatalog.Impact, impact)
        }
            .Where(x => x.Value is not null)
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToList();

        return ranked.Count == 0
            ? null
            : $"Prioriteit: {SchwartzValuesCatalog.EverydayLabel(ranked[0].Code)}";
    }
}
