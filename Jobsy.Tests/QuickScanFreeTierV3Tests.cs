using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public sealed class QuickScanFreeTierV3Tests
{
    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void Free_tier_has_25_unique_v2_ids(AssessmentKind kind)
    {
        var ids = QuickScanFreeTierV3.QuestionIds(kind);
        Assert.Equal(QuickScanFreeTierV3.QuestionCount, ids.Length);
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Equal(QuickScanFreeTierV3.QuestionCount, TestDepthRules.FullCount(kind, AssessmentTestVersions.V3));
    }

    [Fact]
    public void QuickScanVersionPolicy_assigns_v3_when_flag_on()
    {
        Assert.Equal(AssessmentTestVersions.V3, QuickScanVersionPolicy.ResolveVersionForNew(null, testsV2Enabled: true));
        Assert.Equal(AssessmentTestVersions.V2, QuickScanVersionPolicy.ResolveVersionForNew(AssessmentTestVersions.V2, testsV2Enabled: true));
        Assert.Equal(AssessmentTestVersions.V3, QuickScanVersionPolicy.ResolveVersionForNew(AssessmentTestVersions.V3, testsV2Enabled: true));
    }

    [Fact]
    public void V3_competence_scores_all_dimensions_when_complete()
    {
        var answers = QuickScanFreeTierV3.CompetenceQuestionIds.ToDictionary(id => id, _ => 4);
        var scored = AssessmentQuickScanCatalog.ScoreCompetence(answers, AssessmentTestVersions.V3);
        Assert.NotNull(scored);
        Assert.True(scored!.IsComplete);
    }

    [Fact]
    public void V3_career_includes_artistic_in_free_set()
    {
        var ids = QuickScanFreeTierV3.CareerQuestionIds;
        Assert.Contains(17, ids);
        Assert.Contains(21, ids);
    }

    [Fact]
    public void V3_competence_five_per_scale_balanced_reverse()
    {
        var questions = QuickScanFreeTierV3.CompetenceQuestions();
        foreach (var group in questions.GroupBy(q => q.Category, StringComparer.OrdinalIgnoreCase))
        {
            Assert.Equal(5, group.Count());
            Assert.Equal(3, group.Count(q => !q.Reverse));
            Assert.Equal(2, group.Count(q => q.Reverse));
        }
    }

    [Fact]
    public void Legacy_v2_full_counts_unchanged()
    {
        Assert.Equal(40, AssessmentQuickScanCatalog.QuestionCount(AssessmentKind.Competence, AssessmentTestVersions.V2));
        Assert.Equal(48, AssessmentQuickScanCatalog.QuestionCount(AssessmentKind.Career, AssessmentTestVersions.V2));
        Assert.Equal(48, AssessmentQuickScanCatalog.QuestionCount(AssessmentKind.Culture, AssessmentTestVersions.V2));
        Assert.Equal(40, AssessmentQuickScanCatalog.QuestionCount(AssessmentKind.Values, AssessmentTestVersions.V2));
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void QuestionIdsFor_full_v3_covers_free_tier_once(AssessmentKind kind)
    {
        var expected = QuickScanFreeTierV3.QuestionIds(kind);
        var actual = TestDepthRules.QuestionIdsFor(kind, TestDepthLevel.Full, AssessmentTestVersions.V3);
        Assert.Equal(QuickScanFreeTierV3.QuestionCount, actual.Count);
        Assert.True(expected.ToHashSet().SetEquals(actual));
    }
}
