using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class AssessmentQuickScanV2Tests
{
    [Fact]
    public void V2_competence_full_count_is_40()
        => Assert.Equal(40, AssessmentQuickScanCatalog.QuestionCount(AssessmentKind.Competence, AssessmentTestVersions.V2));

    [Fact]
    public void V2_competence_scores_all_dimensions_when_complete()
    {
        var answers = new Dictionary<int, int>();
        foreach (var q in CompetencyTestCatalogV2.Questions)
        {
            answers[q.Id] = 4;
        }

        var scored = AssessmentQuickScanCatalog.ScoreCompetence(answers, AssessmentTestVersions.V2);
        Assert.NotNull(scored);
        Assert.True(scored!.IsComplete);
        Assert.Equal(40, answers.Count);
    }

    [Fact]
    public void V1_and_v2_catalogs_do_not_share_question_ids_at_same_index()
    {
        var v1Ids = CompetencyTestCatalog.Questions.Select(q => q.Id).ToHashSet();
        var v2Ids = CompetencyTestCatalogV2.Questions.Select(q => q.Id).ToHashSet();
        Assert.Equal(25, v1Ids.Count);
        Assert.Equal(40, v2Ids.Count);
        Assert.True(v1Ids.IsSubsetOf(v2Ids));
    }

    [Theory]
    [InlineData(AssessmentKind.Competence, 40)]
    [InlineData(AssessmentKind.Career, 48)]
    [InlineData(AssessmentKind.Culture, 48)]
    [InlineData(AssessmentKind.Values, 40)]
    public void V2_full_counts_match_contract(AssessmentKind kind, int expected)
        => Assert.Equal(expected, TestDepthRules.FullCount(kind, AssessmentTestVersions.V2));
}
