using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class DeepAnalysisItemBankQualityTests
{
    private static readonly AssessmentKind[] Kinds =
    [
        AssessmentKind.Competence,
        AssessmentKind.Career,
        AssessmentKind.Values,
        AssessmentKind.Culture
    ];

    [Theory]
    [MemberData(nameof(KindCases))]
    public void Deep_bank_has_expected_counts_and_unique_stems(AssessmentKind kind)
    {
        var questions = DeepAnalysisCatalog.QuestionsFor(kind);
        var expected = DeepAnalysisCatalog.QuestionCountFor(kind);
        Assert.Equal(expected, questions.Count);
        var prompts = questions.Select(q => q.PromptNl).ToList();
        Assert.Equal(expected, prompts.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(prompts, p => p.Contains("variant", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(KindCases))]
    public void Reverse_keying_per_domain_is_balanced(AssessmentKind kind)
    {
        foreach (var group in DeepAnalysisCatalog.QuestionsFor(kind).GroupBy(q => q.Domain, StringComparer.OrdinalIgnoreCase))
        {
            var count = group.Count();
            Assert.InRange(count, 8, 40);
            var reverse = group.Count(q => q.Reverse);
            var ratio = (double)reverse / count;
            Assert.InRange(ratio, 0.24, 0.46);
        }
    }

    [Theory]
    [MemberData(nameof(KindCases))]
    public void No_near_duplicate_stems_within_deep_bank(AssessmentKind kind)
    {
        var prompts = DeepAnalysisCatalog.QuestionsFor(kind).Select(q => q.PromptNl).ToList();
        var pairs = AssessmentStemQuality.FindNearDuplicatePairs(prompts, 0.82);
        Assert.True(
            pairs.Count == 0,
            $"{kind}: {pairs.Count} near-duplicate pairs; first: {(pairs.Count > 0 ? pairs[0] : default)}");
    }

    [Theory]
    [MemberData(nameof(KindCases))]
    public void Deep_stems_do_not_duplicate_free_quick_scan_prompts(AssessmentKind kind)
    {
        var free = FreeTierPrompts(kind);
        var deep = DeepAnalysisCatalog.QuestionsFor(kind).Select(q => q.PromptNl).ToList();
        foreach (var stem in deep)
        {
            var dup = free.FirstOrDefault(f => AssessmentStemQuality.IsNearDuplicate(f, stem, 0.78));
            Assert.True(string.IsNullOrEmpty(dup), $"Near-duplicate with free tier: {dup}");
        }
    }

    [Theory]
    [MemberData(nameof(KindCases))]
    public void Localized_prompts_match_language_heuristics(AssessmentKind kind)
    {
        var questions = DeepAnalysisCatalog.QuestionsFor(kind);
        foreach (var lang in new[] { "en", "pl", "ro", "ar" })
        {
            var failures = new List<int>();
            foreach (var q in questions)
            {
                var text = DeepItemLocalizations.Resolve(kind, q.Id, q.PromptNl, "ex", lang).Prompt;
                if (!AssessmentLanguageHeuristics.MatchesLanguage(lang, text))
                {
                    failures.Add(q.Id);
                }
            }

            Assert.True(
                failures.Count == 0,
                $"{kind}/{lang}: {failures.Count} prompts failed language heuristics (ids: {string.Join(", ", failures.Take(8))}…)");
        }
    }

    [Fact]
    public void Competence_deep_facets_have_five_items_each()
    {
        Assert.Equal(150, DeepAnalysisCompetenceFacets.ByIndex.Length);
        var groups = DeepAnalysisCompetenceFacets.ByIndex.GroupBy(c => c, StringComparer.OrdinalIgnoreCase);
        Assert.All(groups, g => Assert.Equal(5, g.Count()));
    }

    [Fact]
    public void Free_v3_anchors_same_scoring_domains_as_deep()
    {
        Assert.Equal(6, CareerTestCatalog.RiasecCodes.Length);
        Assert.Equal(5, new[] { SchwartzValuesCatalog.Autonomy, SchwartzValuesCatalog.Connection, SchwartzValuesCatalog.Achievement, SchwartzValuesCatalog.Stability, SchwartzValuesCatalog.Impact }.Length);
        Assert.Equal(5, DeepAnalysisCatalog.BigFiveDomains.Length);
        Assert.True(DeepAnalysisCatalog.SupportsDeepAnalysis(AssessmentKind.Culture));
    }

    public static IEnumerable<object[]> KindCases() => Kinds.Select(k => new object[] { k });

    private static IReadOnlyList<string> FreeTierPrompts(AssessmentKind kind)
    {
        static string Nl(string key) => UiStrings.Get(key, "nl");

        return kind switch
        {
            AssessmentKind.Competence => QuickScanFreeTierV3.CompetenceQuestions()
                .Select(q => Nl(q.TextKey)).ToList(),
            AssessmentKind.Career => QuickScanFreeTierV3.CareerQuestions()
                .Select(q => Nl(q.TextKey)).ToList(),
            AssessmentKind.Culture => QuickScanFreeTierV3.CultureQuestions()
                .Select(q => Nl(q.TextKey)).ToList(),
            AssessmentKind.Values => QuickScanFreeTierV3.ValuesQuestions()
                .Select(q => Nl(q.TextKey)).ToList(),
            _ => []
        };
    }
}
