using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Theme tokens for cross-block strong/weak checks (competence + career/values/culture).</summary>
public static class PupilG78ItemThemes
{
    public static IReadOnlySet<string> ThemesForItem(int itemId)
    {
        if (ThemeByItem.TryGetValue(itemId, out var themes))
        {
            return themes;
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public static string? CompetenceCategory(int itemId)
    {
        if (itemId is < 9001 or > 9015)
        {
            return null;
        }

        return ItemCategory.TryGetValue(itemId, out var cat) ? cat : null;
    }

    public static IReadOnlySet<string> StrongCategoriesFromPublished(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<int> publishedItemIds)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in publishedItemIds)
        {
            var cat = CompetenceCategory(id);
            if (cat is not null)
            {
                set.Add(cat);
            }
        }

        foreach (var c in clauses.Where(c => publishedItemIds.Contains(c.ItemId) && c.EffectiveScore >= 4))
        {
            foreach (var t in ThemesForItem(c.ItemId))
            {
                set.Add("theme:" + t);
            }
        }

        return set;
    }

    public static bool ContrastConflictsStrength(
        string contrastLine,
        IReadOnlySet<int> strongItemIds,
        IReadOnlyList<G78AnswerClause> clauses)
    {
        var c = contrastLine.ToLowerInvariant();
        foreach (var id in strongItemIds)
        {
            var je = PupilG78QuestionJeForms.ForItem(id);
            if (je is null)
            {
                continue;
            }

            var t = je.ToLowerInvariant();
            foreach (var (weak, strong) in PupilG78AnswerConflicts.ThemeClashTokens)
            {
                if (c.Contains(weak, StringComparison.Ordinal) && t.Contains(strong, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            foreach (var theme in ThemesForItem(id))
            {
                if (ThemeLineClash(c, theme))
                {
                    return true;
                }
            }
        }

        foreach (var id in strongItemIds)
        {
            var cat = CompetenceCategory(id);
            if (cat is null)
            {
                continue;
            }

            var kid = PupilG78CompetencyNarrative.KidLabel(cat);
            if (c.Contains(kid, StringComparison.OrdinalIgnoreCase) && c.Contains("minder", StringComparison.Ordinal))
            {
                return true;
            }
        }

        _ = clauses;
        return false;
    }

    public static bool GrowthCategoryAllowed(
        string category,
        IReadOnlySet<int> publishedStrongIds,
        IReadOnlyList<G78AnswerClause> clauses)
        => !clauses.Any(c =>
            publishedStrongIds.Contains(c.ItemId)
            && string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase)
            && c.Model == AssessmentKind.Competence
            && c.EffectiveScore >= 4);

    private static bool ThemeLineClash(string contrastLower, string theme) => theme switch
    {
        "creativiteit" => contrastLower.Contains("creativ", StringComparison.Ordinal)
                           || contrastLower.Contains("maken", StringComparison.Ordinal)
                           || contrastLower.Contains("repareren", StringComparison.Ordinal),
        "maken" => contrastLower.Contains("maken", StringComparison.Ordinal)
                   || contrastLower.Contains("repareren", StringComparison.Ordinal),
        "communiceren" => contrastLower.Contains("communic", StringComparison.Ordinal)
                          || contrastLower.Contains("uitleg", StringComparison.Ordinal),
        "doorzetten" => contrastLower.Contains("doorzet", StringComparison.Ordinal)
                        || contrastLower.Contains("paniek", StringComparison.Ordinal)
                        || contrastLower.Contains("rustig", StringComparison.Ordinal),
        "druk" => contrastLower.Contains("druk", StringComparison.Ordinal)
                  || contrastLower.Contains("gezellig", StringComparison.Ordinal),
        _ => contrastLower.Contains(theme, StringComparison.Ordinal)
    };

    private static readonly Dictionary<int, string> ItemCategory = new()
    {
        [9001] = CompetencyTestCatalog.Samenwerken,
        [9002] = CompetencyTestCatalog.Resultaatgerichtheid,
        [9003] = CompetencyTestCatalog.Stressbestendigheid,
        [9004] = CompetencyTestCatalog.Innovatie,
        [9005] = CompetencyTestCatalog.Extraversie,
        [9006] = CompetencyTestCatalog.Samenwerken,
        [9007] = CompetencyTestCatalog.Resultaatgerichtheid,
        [9008] = CompetencyTestCatalog.Stressbestendigheid,
        [9009] = CompetencyTestCatalog.Innovatie,
        [9010] = CompetencyTestCatalog.Extraversie,
        [9011] = CompetencyTestCatalog.Samenwerken,
        [9012] = CompetencyTestCatalog.Resultaatgerichtheid,
        [9013] = CompetencyTestCatalog.Stressbestendigheid,
        [9014] = CompetencyTestCatalog.Innovatie,
        [9015] = CompetencyTestCatalog.Extraversie,
    };

    private static readonly Dictionary<int, HashSet<string>> ThemeByItem = BuildThemes();

    private static Dictionary<int, HashSet<string>> BuildThemes()
    {
        var d = new Dictionary<int, HashSet<string>>();
        void Add(int id, params string[] themes)
        {
            d[id] = themes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        Add(9001, "samen", "help");
        Add(9002, "plan");
        Add(9003, "doorzetten", "rustig");
        Add(9004, "creativiteit", "nieuw");
        Add(9005, "communiceren");
        Add(9006, "samen", "help");
        Add(9007, "plan");
        Add(9008, "doorzetten");
        Add(9009, "creativiteit", "nieuw");
        Add(9010, "communiceren", "druk", "groep");
        Add(9016, "maken");
        Add(9018, "creativiteit", "maken");
        Add(9019, "help", "samen");
        Add(9020, "leiding");
        Add(9023, "creativiteit");
        Add(9025, "maken", "nieuw");
        Add(9028, "maken");
        Add(9029, "creativiteit");
        Add(9030, "samen");
        Add(9051, "creativiteit", "nieuw");
        Add(9047, "samen", "team");
        Add(9049, "druk", "gezellig");
        Add(9050, "nieuw");
        Add(9053, "samen", "team");
        Add(9058, "samen", "groep");
        Add(9059, "samen");
        return d;
    }
}
