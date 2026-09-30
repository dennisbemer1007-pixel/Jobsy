using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Single source for free-test depth levels, counts, minutes and uitgebreide-test parts.
/// Used by full test pages, OnboardingMiniTest / ontdekkingsreis 07–08, and DeepAnalysis.
/// </summary>
public enum TestDepthLevel
{
    First = 0,
    Deeper = 1,
    Full = 2,
    Bottom = 3
}

public sealed record TestDepthBand(TestDepthLevel Level, int Count);

public sealed record TestDepthPart(int Index, int Size, IReadOnlyList<int> QuestionIds, IReadOnlyList<string> Domains);

public static class TestDepthRules
{
    public const int FirstCount = 5;
    public const int DeeperCount = 10;

    public static int FullCount(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Culture => CulturePersonalityCatalog.QuestionCount,
        AssessmentKind.Competence => CompetencyTestCatalog.QuestionCount,
        AssessmentKind.Career => CareerTestCatalog.QuestionCount,
        AssessmentKind.Values => SchwartzValuesCatalog.QuestionCount,
        _ => CompetencyTestCatalog.QuestionCount
    };

    public static int Bottom(AssessmentKind kind)
        => DeepAnalysisCatalog.QuestionCountFor(kind);

    /// <summary>Free levels only: First(5), Deeper(10), Full(18/25).</summary>
    public static IReadOnlyList<TestDepthBand> Levels(AssessmentKind kind)
        =>
        [
            new(TestDepthLevel.First, FirstCount),
            new(TestDepthLevel.Deeper, DeeperCount),
            new(TestDepthLevel.Full, FullCount(kind))
        ];

    public static int CountFor(AssessmentKind kind, TestDepthLevel level) => level switch
    {
        TestDepthLevel.First => FirstCount,
        TestDepthLevel.Deeper => DeeperCount,
        TestDepthLevel.Full => FullCount(kind),
        TestDepthLevel.Bottom => Bottom(kind),
        _ => FirstCount
    };

    /// <summary>Highest free level reached by answered count; null below 5.</summary>
    public static TestDepthLevel? Reached(AssessmentKind kind, int answeredCount)
    {
        if (answeredCount < FirstCount) return null;
        if (answeredCount >= FullCount(kind)) return TestDepthLevel.Full;
        if (answeredCount >= DeeperCount) return TestDepthLevel.Deeper;
        return TestDepthLevel.First;
    }

    /// <summary>Next free level after the current answered count, or null when Full is done.</summary>
    public static TestDepthLevel? Next(AssessmentKind kind, int answeredCount)
    {
        if (answeredCount < FirstCount) return TestDepthLevel.First;
        if (answeredCount < DeeperCount) return TestDepthLevel.Deeper;
        if (answeredCount < FullCount(kind)) return TestDepthLevel.Full;
        return null;
    }

    /// <summary>
    /// Incremental minutes for a free level (08.3), or rounded bottom estimate for Deep.
    /// First ≈ 1; Deeper +2; Full +6 (Culture +4); Bottom = ceil(n×12s/60) rounded up to 5.
    /// </summary>
    public static int MinutesFor(AssessmentKind kind, TestDepthLevel level) => level switch
    {
        TestDepthLevel.First => 1,
        TestDepthLevel.Deeper => 2,
        TestDepthLevel.Full => kind == AssessmentKind.Culture ? 4 : 6,
        TestDepthLevel.Bottom => RoundUpToFive(CeilingMinutes(Bottom(kind), secondsPerQuestion: 12)),
        _ => 1
    };

    /// <summary>
    /// Ordered question ids for a free level: mini, then DeeperQuestionIds, then remaining catalog ids.
    /// </summary>
    public static IReadOnlyList<int> QuestionIdsFor(AssessmentKind kind, TestDepthLevel level)
    {
        if (level == TestDepthLevel.Bottom)
        {
            return DeepAnalysisCatalog.QuestionsFor(kind).Select(q => q.Id).ToList();
        }

        var target = CountFor(kind, level);
        return QuestionIdsUpTo(kind, target);
    }

    public static IReadOnlyList<int> QuestionIdsUpTo(AssessmentKind kind, int targetCount)
    {
        var full = FullCount(kind);
        var target = Math.Clamp(targetCount, FirstCount, full);
        var ordered = new List<int>(MiniIds(kind));
        if (target <= FirstCount)
        {
            return ordered;
        }

        foreach (var id in DeeperIds(kind))
        {
            if (!ordered.Contains(id)) ordered.Add(id);
        }

        if (target <= DeeperCount)
        {
            return ordered.Take(DeeperCount).ToList();
        }

        foreach (var id in AllCatalogIds(kind))
        {
            if (!ordered.Contains(id)) ordered.Add(id);
        }

        return ordered.Take(full).ToList();
    }

    /// <summary>
    /// Five parts for the uitgebreide test. Competence/values = 5×30 domains.
    /// Career ≈ 5×40 and culture ≈ 5×30 by grouping whole domains (never split unless a domain alone exceeds the part size).
    /// </summary>
    public static IReadOnlyList<TestDepthPart> Parts(AssessmentKind kind)
    {
        var questions = DeepAnalysisScreenOrder.For(kind);
        var byDomain = questions
            .GroupBy(q => q.Domain, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Domain: g.Key, Ids: g.Select(q => q.Id).ToList()))
            .ToList();

        var targetSize = kind switch
        {
            AssessmentKind.Career => 40,
            AssessmentKind.Culture => 30,
            _ => 30
        };

        var parts = new List<TestDepthPart>(5);
        var bucketDomains = new List<string>();
        var bucketIds = new List<int>();

        void Flush()
        {
            if (bucketIds.Count == 0) return;
            parts.Add(new TestDepthPart(parts.Count + 1, bucketIds.Count, bucketIds.ToList(), bucketDomains.ToList()));
            bucketDomains = [];
            bucketIds = [];
        }

        foreach (var (domain, ids) in byDomain)
        {
            if (ids.Count > targetSize)
            {
                // Domain alone exceeds part size: flush current, then emit chunked parts for this domain.
                Flush();
                for (var i = 0; i < ids.Count; i += targetSize)
                {
                    var chunk = ids.Skip(i).Take(targetSize).ToList();
                    parts.Add(new TestDepthPart(parts.Count + 1, chunk.Count, chunk, [domain]));
                }

                continue;
            }

            if (bucketIds.Count > 0 && bucketIds.Count + ids.Count > targetSize)
            {
                Flush();
            }

            bucketDomains.Add(domain);
            bucketIds.AddRange(ids);
        }

        Flush();

        // Normalize to exactly 5 parts when possible by merging tiny trailing leftovers forward/back.
        while (parts.Count > 5)
        {
            var last = parts[^1];
            var prev = parts[^2];
            var mergedIds = prev.QuestionIds.Concat(last.QuestionIds).ToList();
            var mergedDomains = prev.Domains.Concat(last.Domains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            parts.RemoveAt(parts.Count - 1);
            parts[^1] = new TestDepthPart(parts.Count, mergedIds.Count, mergedIds, mergedDomains);
        }

        while (parts.Count < 5 && parts.Count > 0)
        {
            // Split the largest part to fill up to 5 (rare; career domains already land near 5).
            var largestIdx = 0;
            for (var i = 1; i < parts.Count; i++)
            {
                if (parts[i].Size > parts[largestIdx].Size) largestIdx = i;
            }

            var big = parts[largestIdx];
            if (big.Size < 2) break;
            var half = big.Size / 2;
            var aIds = big.QuestionIds.Take(half).ToList();
            var bIds = big.QuestionIds.Skip(half).ToList();
            parts[largestIdx] = new TestDepthPart(0, aIds.Count, aIds, big.Domains);
            parts.Insert(largestIdx + 1, new TestDepthPart(0, bIds.Count, bIds, big.Domains));
        }

        for (var i = 0; i < parts.Count; i++)
        {
            parts[i] = parts[i] with { Index = i + 1 };
        }

        return parts;
    }

    public static AssessmentKind ToAssessmentKind(OnboardingWizardCatalog.OnboardingTestKind kind) => kind switch
    {
        OnboardingWizardCatalog.OnboardingTestKind.Career => AssessmentKind.Career,
        OnboardingWizardCatalog.OnboardingTestKind.Culture => AssessmentKind.Culture,
        OnboardingWizardCatalog.OnboardingTestKind.Values => AssessmentKind.Values,
        _ => AssessmentKind.Competence
    };

    public static OnboardingWizardCatalog.OnboardingTestKind ToOnboardingKind(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => OnboardingWizardCatalog.OnboardingTestKind.Career,
        AssessmentKind.Culture => OnboardingWizardCatalog.OnboardingTestKind.Culture,
        AssessmentKind.Values => OnboardingWizardCatalog.OnboardingTestKind.Values,
        _ => OnboardingWizardCatalog.OnboardingTestKind.Competency
    };

    public static int[] MiniIds(AssessmentKind kind)
        => OnboardingWizardCatalog.MiniIds(ToOnboardingKind(kind));

    public static int[] DeeperIds(AssessmentKind kind)
        => OnboardingWizardCatalog.DeeperIds(ToOnboardingKind(kind));

    public static IReadOnlyList<int> AllCatalogIds(AssessmentKind kind)
        => OnboardingWizardCatalog.AllCatalogIds(ToOnboardingKind(kind));

    /// <summary>Scene depth token: 8 at First, 9 at Deeper/Full, 10 at Bottom.</summary>
    public static int SceneDepth(TestDepthLevel level) => level switch
    {
        TestDepthLevel.First => 8,
        TestDepthLevel.Deeper => 9,
        TestDepthLevel.Full => 9,
        TestDepthLevel.Bottom => 10,
        _ => 8
    };

    public static string NameKey(TestDepthLevel level) => level switch
    {
        TestDepthLevel.First => "TestDepth.First",
        TestDepthLevel.Deeper => "TestDepth.Deeper",
        TestDepthLevel.Full => "TestDepth.Full",
        TestDepthLevel.Bottom => "TestDepth.Bottom",
        _ => "TestDepth.First"
    };

    private static int CeilingMinutes(int questions, int secondsPerQuestion)
        => (int)Math.Ceiling(questions * secondsPerQuestion / 60.0);

    private static int RoundUpToFive(int minutes)
    {
        if (minutes <= 0) return 5;
        var rem = minutes % 5;
        return rem == 0 ? minutes : minutes + (5 - rem);
    }
}
