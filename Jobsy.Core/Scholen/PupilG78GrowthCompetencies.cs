using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Low/mid competency dimensions for the G78 growth block (fact-only).</summary>
public static class PupilG78GrowthCompetencies
{
    private static readonly PupilQuestionBank Bank = new();

    private static readonly Dictionary<int, string> AnswerLabelNl = new()
    {
        [1] = "nee",
        [2] = "niet echt",
        [3] = "soms",
        [4] = "best wel",
        [5] = "ja!",
    };

    public static IReadOnlyList<string> TopStrongCategories(IReadOnlyDictionary<string, double> averages, int count = 3)
        => averages
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(count)
            .Select(kv => kv.Key)
            .ToList();

    /// <summary>Lowest competency dimensions for growth (mean &lt; 3.0 or any 1–2 answer), max 5.</summary>
    public static IReadOnlyList<string> SelectGrowthCategories(
        IReadOnlyDictionary<string, int> answers,
        int maxCount = 5,
        bool contrastV2 = false)
        => CompetencyTestCatalog.QuickScanCategories
            .Select(cat => (Category: cat, Stats: StatsForCategory(cat, answers, contrastV2)))
            .Where(x => x.Stats.Average < 3.0 || (x.Stats.HasLow && x.Stats.Average < 3.35))
            .OrderBy(x => x.Stats.Average)
            .ThenBy(x => x.Category, StringComparer.Ordinal)
            .Take(maxCount)
            .Select(x => x.Category)
            .ToList();

    /// <summary>g78-2: when threshold growth is empty and the story has no strengths block, pick the lowest blended dimensions.</summary>
    public static IReadOnlyList<string> SelectGrowthCategoriesWithFallback(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlyList<G78AnswerClause> clauses,
        PupilResult result,
        int maxCount,
        bool contrastV2)
    {
        var selected = SelectGrowthCategories(answers, maxCount, contrastV2).ToList();
        if (selected.Count > 0 || !contrastV2 || clauses.Count == 0)
        {
            return selected;
        }

        return DimensionScoresContrastV2(clauses, result, answers)
            .OrderBy(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(maxCount)
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>Drop growth rows when story or tiles already show strong evidence for that competency.</summary>
    public static IReadOnlyList<string> FilterGrowthWithoutStrongPageEvidence(
        IReadOnlyList<string> growthCategories,
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<int> pageItemIds,
        bool contrastV2)
        => growthCategories
            .Where(cat => !CategoryHasStrongEvidenceOnPage(cat, clauses, pageItemIds, contrastV2))
            .ToList();

    public static bool CategoryHasStrongEvidenceOnPage(
        string category,
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<int> pageItemIds,
        bool contrastV2)
    {
        foreach (var clause in clauses)
        {
            if (!pageItemIds.Contains(clause.ItemId) || clause.EffectiveScore < 4)
            {
                continue;
            }

            if (contrastV2)
            {
                if (PupilG78CompetencyExampleItems.ClauseSupportsCategory(clause, category, contrastV2: true))
                {
                    return true;
                }

                continue;
            }

            if (string.Equals(clause.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Diagnostics: blended v2 dimension scores used for strength intro selection.</summary>
    public static IReadOnlyDictionary<string, double> ContrastDimensionScoresForDiagnostics(
        IReadOnlyList<G78AnswerClause> clauses,
        PupilResult result,
        IReadOnlyDictionary<string, int> answers,
        bool contrastV2)
        => contrastV2
            ? DimensionScoresContrastV2(clauses, result, answers)
            : DimensionAverages(answers, contrastV2);

    public static IReadOnlyList<PupilG78GrowthCompetencyDto> BuildForCategories(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlyList<string> growthCategories,
        IReadOnlySet<int>? pageItemIds,
        string language = "nl",
        bool contrastV2 = false)
    {
        var list = new List<PupilG78GrowthCompetencyDto>();
        foreach (var category in growthCategories)
        {
            var stats = StatsForCategory(category, answers, contrastV2);
            var why = WhyExplanation(stats, pageItemIds, category, language, contrastV2);
            if (contrastV2 && string.IsNullOrWhiteSpace(why))
            {
                var kid = PupilG78CompetencyNarrative.KidLabelTitle(category);
                why = $"{kid} kwam minder naar voren in je antwoorden.";
            }

            list.Add(new PupilG78GrowthCompetencyDto(
                category,
                PupilG78CompetencyNarrative.KidLabelTitle(category),
                why,
                PanelId(category)));
        }

        return list;
    }

    public static IReadOnlyList<PupilG78GrowthCompetencyDto> Build(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string>? excludedCategories = null,
        string language = "nl")
        => BuildCore(answers, excludedCategories ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase), null, language);

    private static IReadOnlyList<PupilG78GrowthCompetencyDto> BuildCore(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string> excludedCategories,
        IReadOnlySet<int>? pageItemIds,
        string language)
    {
        var averages = DimensionAverages(answers);
        if (averages.Count == 0)
        {
            return [];
        }

        var strong = TopStrongCategories(averages).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lowest = averages.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).First().Key;
        var list = new List<PupilG78GrowthCompetencyDto>();

        foreach (var (category, avg) in averages.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (list.Count >= 5)
            {
                break;
            }

            if (excludedCategories.Contains(category))
            {
                continue;
            }

            var stats = StatsForCategory(category, answers);
            if (!ShouldInclude(stats, strong, lowest, category, excludedCategories))
            {
                continue;
            }

            var panelId = PanelId(category);
            list.Add(new PupilG78GrowthCompetencyDto(
                category,
                PupilG78CompetencyNarrative.KidLabelTitle(category),
                WhyExplanation(stats, pageItemIds, category, language),
                panelId));
        }

        if (list.Count == 0)
        {
            var fallback = averages
                .Where(kv => !excludedCategories.Contains(kv.Key))
                .OrderBy(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => kv.Key)
                .FirstOrDefault();
            if (fallback is not null)
            {
                var stats = StatsForCategory(fallback, answers);
                list.Add(new PupilG78GrowthCompetencyDto(
                    fallback,
                    PupilG78CompetencyNarrative.KidLabelTitle(fallback),
                    WhyExplanation(stats, pageItemIds, fallback, language),
                    PanelId(fallback)));
            }
        }

        return list;
    }

    /// <summary>g78-2: strength per competency dimension from chosen-pole facts (all worlds).</summary>
    public static Dictionary<string, double> DimensionAveragesFromChosenClauses(IReadOnlyList<G78AnswerClause> clauses)
    {
        var scores = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in clauses.Where(c => c.EffectiveScore >= 4))
        {
            void Add(string? dim)
            {
                if (string.IsNullOrEmpty(dim)
                    || !CompetencyTestCatalog.QuickScanCategories.Contains(dim, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }

                if (!scores.TryGetValue(dim, out var list))
                {
                    list = [];
                    scores[dim] = list;
                }

                list.Add(c.EffectiveScore);
            }

            if (!string.IsNullOrWhiteSpace(c.Category))
            {
                Add(c.Category);
            }
        }

        return scores.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Average(),
            StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> CompetencyCategoriesFromHolland(string? hollandCode, int count = 3)
    {
        if (string.IsNullOrWhiteSpace(hollandCode))
        {
            return [];
        }

        var list = new List<string>();
        foreach (var ch in hollandCode.Trim().ToUpperInvariant())
        {
            var cat = ch switch
            {
                'R' => CompetencyTestCatalog.Resultaatgerichtheid,
                'I' => CompetencyTestCatalog.Innovatie,
                'A' => CompetencyTestCatalog.Innovatie,
                'S' => CompetencyTestCatalog.Samenwerken,
                'E' => CompetencyTestCatalog.Extraversie,
                'C' => CompetencyTestCatalog.Stressbestendigheid,
                _ => null,
            };
            if (cat is null || list.Contains(cat, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            list.Add(cat);
            if (list.Count >= count)
            {
                break;
            }
        }

        return list;
    }

    private static readonly Dictionary<char, string[]> HollandLetterToCompetency = new()
    {
        ['R'] = [CompetencyTestCatalog.Resultaatgerichtheid, CompetencyTestCatalog.Stressbestendigheid],
        ['I'] = [CompetencyTestCatalog.Innovatie],
        ['A'] = [CompetencyTestCatalog.Innovatie],
        ['S'] = [CompetencyTestCatalog.Samenwerken, CompetencyTestCatalog.Extraversie],
        ['E'] = [CompetencyTestCatalog.Extraversie, CompetencyTestCatalog.Resultaatgerichtheid],
        ['C'] = [CompetencyTestCatalog.Resultaatgerichtheid, CompetencyTestCatalog.Stressbestendigheid],
    };

    /// <summary>g78-2: contrast Likert per dimension, lightly blended with RIASEC career profile.</summary>
    public static Dictionary<string, double> DimensionScoresContrastV2(
        IReadOnlyList<G78AnswerClause> clauses,
        PupilResult result,
        IReadOnlyDictionary<string, int> answers)
    {
        var fromClauses = DimensionAveragesFromChosenClauses(clauses);
        var merged = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            var stats = StatsForCategory(cat, answers, contrastV2: true);
            var baseAvg = stats.ItemScores.Count > 0 ? stats.Average : 0;
            fromClauses.TryGetValue(cat, out var clauseAvg);
            var bonus = RiasecCompetencyBonus(cat, result);
            merged[cat] = baseAvg * 0.55 + clauseAvg * 0.35 + bonus;
        }

        return merged;
    }

    private static double RiasecCompetencyBonus(string competencyCategory, PupilResult result)
    {
        var riasec = ParseRiasec(result.RiasecScoresJson);
        if (riasec is null)
        {
            return 0;
        }

        double Bonus(char letter, int? score)
        {
            if (score is null or 0)
            {
                return 0;
            }

            return HollandLetterToCompetency.TryGetValue(letter, out var comps)
                   && comps.Contains(competencyCategory, StringComparer.OrdinalIgnoreCase)
                ? score.Value / 15.0
                : 0;
        }

        return Bonus('R', riasec.Realistic)
               + Bonus('I', riasec.Investigative)
               + Bonus('A', riasec.Artistic)
               + Bonus('S', riasec.Social)
               + Bonus('E', riasec.Enterprising)
               + Bonus('C', riasec.Conventional);
    }

    private static RiasecScores? ParseRiasec(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<RiasecScores>(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public static Dictionary<string, double> DimensionAverages(
        IReadOnlyDictionary<string, int> answers,
        bool contrastV2 = false)
    {
        var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            var stats = StatsForCategory(cat, answers, contrastV2);
            if (stats.ItemScores.Count > 0)
            {
                dict[cat] = stats.Average;
            }
        }

        return dict;
    }

    private static bool ShouldInclude(
        DimensionStats stats,
        HashSet<string> strong,
        string lowest,
        string category,
        IReadOnlySet<string> excludedCategories)
    {
        if (excludedCategories.Contains(category))
        {
            return false;
        }

        var isLowest = string.Equals(category, lowest, StringComparison.OrdinalIgnoreCase);
        if (isLowest)
        {
            return true;
        }

        if (strong.Contains(category))
        {
            return false;
        }

        if (stats.Average >= 4.25 && stats.HasHigh && !stats.HasLow)
        {
            return false;
        }

        return stats.HasLow || stats.OnlyMid || stats.Average <= 3.25;
    }

    private static string WhyExplanation(
        DimensionStats stats,
        IReadOnlySet<int>? pageItemIds,
        string growthCategory,
        string language,
        bool contrastV2 = false)
    {
        _ = language;
        _ = growthCategory;
        var picks = stats.ItemScores
            .Where(i => i.Effective <= 3
                        && (pageItemIds is null || !pageItemIds.Contains(i.ItemId)))
            .OrderBy(i => i.Effective)
            .ThenBy(i => i.ItemId)
            .Take(2)
            .ToList();
        if (picks.Count == 0)
        {
            picks = stats.ItemScores.OrderBy(i => i.Effective).ThenBy(i => i.ItemId).Take(1).ToList();
        }

        if (contrastV2)
        {
            if (stats.Average >= 3.0 && !stats.HasLow)
            {
                return "";
            }

            var kid = PupilG78CompetencyNarrative.KidLabelTitle(growthCategory);
            return $"{kid} kwam minder naar voren in je antwoorden.";
        }

        var parts = new List<string>();
        foreach (var pick in picks)
        {
            var stem = PupilQuestionBankNlStrings.Get($"LeerlingQ.{pick.ItemId}");
            stem = stem.Replace("geen zin hebt", "geen zin heb", StringComparison.Ordinal);
            var label = AnswerLabelNl.GetValueOrDefault(pick.Raw, "soms");
            parts.Add($"Bij \"{stem}\" koos je \"{label}\".");
        }

        return string.Join(' ', parts);
    }

    private static DimensionStats StatsForCategory(
        string category,
        IReadOnlyDictionary<string, int> answers,
        bool contrastV2 = false)
    {
        var bank = contrastV2 ? (IPupilQuestionBank)new PupilQuestionBankG78V2() : Bank;
        var itemScores = new List<ItemScore>();
        foreach (var q in bank.Questions.Where(q =>
                     q.Model == AssessmentKind.Competence
                     && q.Id <= PupilG78CompetencyNarrative.CompetenceItemMax))
        {
            if (!answers.TryGetValue(q.Id.ToString(), out var raw))
            {
                continue;
            }

            int eff;
            if (contrastV2 && q.IsContrast)
            {
                var countsForCategory =
                    string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(q.CategoryB)
                        && string.Equals(q.CategoryB, category, StringComparison.OrdinalIgnoreCase));
                if (!countsForCategory)
                {
                    continue;
                }

                eff = ContrastEffectiveForCategory(q, category, raw);
                if (eff < 1)
                {
                    continue;
                }
            }
            else
            {
                if (!string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
            }

            itemScores.Add(new ItemScore(q.Id, raw, eff));
        }

        if (itemScores.Count == 0)
        {
            return new DimensionStats(0, false, false, false, itemScores);
        }

        var avg = itemScores.Average(i => i.Effective);
        var hasLow = itemScores.Any(i => i.Effective <= 2);
        var hasHigh = itemScores.Any(i => i.Effective >= 4);
        var onlyMid = itemScores.All(i => i.Effective == 3);
        return new DimensionStats(avg, hasLow, hasHigh, onlyMid, itemScores);
    }

    internal static string PanelId(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => "ll-grow-samenwerken",
        CompetencyTestCatalog.Resultaatgerichtheid => "ll-grow-plannen",
        CompetencyTestCatalog.Stressbestendigheid => "ll-grow-doorzetten",
        CompetencyTestCatalog.Innovatie => "ll-grow-creativiteit",
        CompetencyTestCatalog.Extraversie => "ll-grow-communiceren",
        _ => "ll-grow-" + category.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal)
    };

    /// <summary>Effective 1–5 for a competency dimension on contrast items (only the chosen pole counts).</summary>
    private static int ContrastEffectiveForCategory(PupilQuestion q, string category, int raw)
    {
        if (raw is < 1 or > 5 || raw == 3)
        {
            return -1;
        }

        if (string.IsNullOrEmpty(q.CategoryB))
        {
            if (!string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            return raw is 1 or 2 ? PupilG78ContrastLikertScorer.TowardA(raw) : -1;
        }

        if (string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase) && raw is 1 or 2)
        {
            return PupilG78ContrastLikertScorer.TowardA(raw);
        }

        if (string.Equals(q.CategoryB, category, StringComparison.OrdinalIgnoreCase) && raw is 4 or 5)
        {
            return PupilG78ContrastLikertScorer.TowardB(raw);
        }

        return -1;
    }

    private sealed record ItemScore(int ItemId, int Raw, int Effective);

    private sealed record DimensionStats(
        double Average,
        bool HasLow,
        bool HasHigh,
        bool OnlyMid,
        IReadOnlyList<ItemScore> ItemScores);
}
