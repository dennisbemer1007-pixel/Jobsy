using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Canonical competence examples per dimension (Bijvoorbeeld must match the label).</summary>
public static class PupilG78CompetencyExampleItems
{
    private static readonly IReadOnlyDictionary<string, int[]> Preferred = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
    {
        [CompetencyTestCatalog.Samenwerken] = [9006, 9001],
        [CompetencyTestCatalog.Resultaatgerichtheid] = [9007, 9021],
        [CompetencyTestCatalog.Stressbestendigheid] = [9002, 9008, 9003],
        [CompetencyTestCatalog.Innovatie] = [9023, 9004, 9009, 9018, 9029],
        [CompetencyTestCatalog.Extraversie] = [9005, 9010],
    };

    public static IReadOnlyList<int> ForCategory(string competencyCategory)
        => Preferred.TryGetValue(competencyCategory, out var ids) ? ids : [];

    public static bool ItemMatchesCategory(int itemId, string competencyCategory)
    {
        if (ForCategory(competencyCategory).Contains(itemId))
        {
            return true;
        }

        foreach (var dim in PupilG78ItemDimensionMap.DimensionsFor(itemId))
        {
            if (string.Equals(dim, competencyCategory, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (itemId > PupilG78CompetencyNarrative.PositiveTileCompetenceMax)
        {
            return false;
        }

        var bankCat = PupilG78ItemThemes.CompetenceCategory(itemId);
        return bankCat is not null
               && string.Equals(bankCat, competencyCategory, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>g78-2: chosen-pole sentence must read as the kid label we name in paragraph 2.</summary>
    public static bool ChosenFactSupportsKidLabel(string competencyCategory, string mainClauseJeForm)
    {
        var text = mainClauseJeForm.ToLowerInvariant();
        return competencyCategory switch
        {
            CompetencyTestCatalog.Innovatie => SupportsAny(text, "bedenk", "nieuw", "mooi", "verhaal", "teken", "muziek", "verzin", "idee", "outfit", "dans")
                && !SupportsAny(text, "handleiding", "stappenplan", "lijstjes", "mappen", "volgorde", "altijd doen"),
            CompetencyTestCatalog.Extraversie => SupportsAny(text, "groep", "praat", "vertel", "voor groep", "nieuwe mensen", "opstap"),
            CompetencyTestCatalog.Samenwerken => SupportsAny(text, "samen", "groep", "samenwer", "help", "team"),
            CompetencyTestCatalog.Stressbestendigheid => SupportsAny(text, "verder", "doorwerk", "rustig", "volhou", "tegenzit", "opnieuw", "doorwerk")
                && !SupportsAny(text, "wil je stoppen", "wil ik stoppen", "iets leuks", "later wel", "even stoppen"),
            CompetencyTestCatalog.Resultaatgerichtheid => SupportsAny(text, "plan", "organis", "afmaken", "volgorde", "handleiding", "stappen", "op tijd", "lijst"),
            _ => text.Length > 0,
        };
    }

    public static bool ClauseSupportsCategory(
        G78AnswerClause clause,
        string competencyCategory,
        bool contrastV2)
    {
        if (contrastV2)
        {
            return !string.IsNullOrWhiteSpace(clause.Category)
                   && string.Equals(clause.Category, competencyCategory, StringComparison.OrdinalIgnoreCase)
                   && ChosenFactSupportsKidLabel(competencyCategory, clause.Text.Trim().TrimEnd('.'));
        }

        return ItemMatchesCategory(clause.ItemId, competencyCategory);
    }

    public static IEnumerable<int> ExampleCandidateIds(
        string competencyCategory,
        IReadOnlyList<int> reservedExampleIds,
        IReadOnlyList<G78AnswerClause> clauses,
        bool contrastV2)
    {
        var ordered = new List<int>();
        void TryAdd(int id)
        {
            if (id != 0 && ItemMatchesCategory(id, competencyCategory) && !ordered.Contains(id))
            {
                ordered.Add(id);
            }
        }

        foreach (var id in reservedExampleIds)
        {
            TryAdd(id);
        }

        foreach (var id in ForCategory(competencyCategory))
        {
            TryAdd(id);
        }

        foreach (var c in clauses
                     .Where(x => x.EffectiveScore >= 4 && ClauseSupportsCategory(x, competencyCategory, contrastV2))
                     .OrderByDescending(x => contrastV2 && x.Model == AssessmentKind.Competence ? 1 : 0)
                     .ThenByDescending(x => x.EffectiveScore)
                     .ThenBy(x => x.ItemId))
        {
            TryAdd(c.ItemId);
        }

        return ordered;
    }

    private static bool SupportsAny(string text, params string[] needles)
    {
        foreach (var n in needles)
        {
            if (text.Contains(n, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
