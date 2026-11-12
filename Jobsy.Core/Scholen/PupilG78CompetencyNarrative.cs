using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Kid-friendly competency labels and neutral phrasing for G78 fact stories.</summary>
public static class PupilG78CompetencyNarrative
{
    public const int CompetenceItemMax = 9015;

    /// <summary>Non-reverse competence items only — safe for positive-only tiles.</summary>
    public const int PositiveTileCompetenceMax = 9010;

    public static string KidLabel(string competencyCategory) => competencyCategory switch
    {
        CompetencyTestCatalog.Samenwerken => "samenwerken",
        CompetencyTestCatalog.Resultaatgerichtheid => "plannen en organiseren",
        CompetencyTestCatalog.Stressbestendigheid => "doorzetten",
        CompetencyTestCatalog.Innovatie => "creativiteit",
        CompetencyTestCatalog.Extraversie => "communiceren",
        _ => competencyCategory.ToLowerInvariant()
    };

    public static string KidLabelTitle(string competencyCategory) =>
        char.ToUpperInvariant(KidLabel(competencyCategory)[0]) + KidLabel(competencyCategory)[1..];

    public static string MinderNaarVoren(string competencyCategory) =>
        $"{KidLabelTitle(competencyCategory)} komt in je antwoorden minder naar voren.";

    public static string StoryLowLine(string competencyCategory) =>
        $"Wat betreft {KidLabel(competencyCategory)}: dat komt in je antwoorden minder naar voren.";

    public static Dictionary<string, double> DimensionAverages(IReadOnlyList<G78AnswerClause> clauses)
    {
        var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            var items = clauses.Where(c =>
                c.Model == AssessmentKind.Competence
                && c.ItemId is >= 9001 and <= CompetenceItemMax
                && string.Equals(c.Category, cat, StringComparison.OrdinalIgnoreCase)).ToList();
            if (items.Count > 0)
            {
                dict[cat] = items.Average(c => c.EffectiveScore);
            }
        }

        return dict;
    }
}
