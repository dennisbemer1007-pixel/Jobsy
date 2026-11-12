using Jobsy.Core.Contracts.Scholen;
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

    public static IReadOnlyList<PupilG78GrowthCompetencyDto> Build(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<int> publishedStrongIds,
        IReadOnlySet<string>? alsoExcludedCategories = null,
        string language = "nl")
    {
        var averages = DimensionAverages(answers);
        var excludedCategories = CompetencyTestCatalog.QuickScanCategories
            .Where(cat => !PupilG78ItemThemes.GrowthCategoryAllowed(cat, publishedStrongIds, clauses))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (alsoExcludedCategories is not null)
        {
            excludedCategories.UnionWith(alsoExcludedCategories);
        }

        return BuildCore(answers, excludedCategories, publishedStrongIds, language);
    }

    public static IReadOnlyList<PupilG78GrowthCompetencyDto> Build(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string>? excludedCategories = null,
        string language = "nl")
        => BuildCore(answers, excludedCategories ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase), null, language);

    private static IReadOnlyList<PupilG78GrowthCompetencyDto> BuildCore(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string> excludedCategories,
        IReadOnlySet<int>? publishedStrongIds,
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
                WhyExplanation(stats, publishedStrongIds, language),
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
                    WhyExplanation(stats, publishedStrongIds, language),
                    PanelId(fallback)));
            }
        }

        return list;
    }

    public static Dictionary<string, double> DimensionAverages(IReadOnlyDictionary<string, int> answers)
    {
        var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            var scores = ScoresForCategory(cat, answers);
            if (scores.Count > 0)
            {
                dict[cat] = scores.Average();
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
        IReadOnlySet<int>? publishedStrongIds,
        string language)
    {
        _ = language;
        var picks = stats.ItemScores
            .Where(i => i.Effective <= 3
                        && (publishedStrongIds is null || !publishedStrongIds.Contains(i.ItemId)))
            .OrderBy(i => i.Effective)
            .ThenBy(i => i.ItemId)
            .Take(2)
            .ToList();
        if (picks.Count == 0)
        {
            picks = stats.ItemScores.OrderBy(i => i.Effective).ThenBy(i => i.ItemId).Take(1).ToList();
        }

        var parts = new List<string>();
        foreach (var pick in picks)
        {
            var stem = PupilQuestionBankNlStrings.Get($"LeerlingQ.{pick.ItemId}");
            var label = AnswerLabelNl.GetValueOrDefault(pick.Raw, "soms");
            parts.Add($"Bij \"{stem}\" koos je \"{label}\".");
        }

        return string.Join(' ', parts);
    }

    private static DimensionStats StatsForCategory(string category, IReadOnlyDictionary<string, int> answers)
    {
        var itemScores = new List<ItemScore>();
        foreach (var q in Bank.Questions.Where(q =>
                     q.Model == AssessmentKind.Competence
                     && q.Id <= PupilG78CompetencyNarrative.CompetenceItemMax
                     && string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)))
        {
            if (!answers.TryGetValue(q.Id.ToString(), out var raw))
            {
                continue;
            }

            var eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
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

    private static List<double> ScoresForCategory(string category, IReadOnlyDictionary<string, int> answers)
    {
        var list = new List<double>();
        foreach (var q in Bank.Questions.Where(q =>
                     q.Model == AssessmentKind.Competence
                     && q.Id <= PupilG78CompetencyNarrative.CompetenceItemMax
                     && string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)))
        {
            if (answers.TryGetValue(q.Id.ToString(), out var raw))
            {
                list.Add(PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse));
            }
        }

        return list;
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

    private sealed record ItemScore(int ItemId, int Raw, int Effective);

    private sealed record DimensionStats(
        double Average,
        bool HasLow,
        bool HasHigh,
        bool OnlyMid,
        IReadOnlyList<ItemScore> ItemScores);
}
