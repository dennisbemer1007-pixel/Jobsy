using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Dna;
using Xunit;

namespace Jobsy.Tests;

public class TestDepthValueInsightTests
{
    [Theory]
    [InlineData(5, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 3)]
    [InlineData(24, 3)]
    public void MiniThemeCount_follows_milestones(int answered, int expected)
        => Assert.Equal(expected, TestDepthValueInsight.MiniThemeCount(answered));

    [Fact]
    public void ShowsPartialResultPage_only_with_flag_and_partial_competence()
    {
        Assert.False(TestDepthValueInsight.ShowsPartialResultPage(false, AssessmentKind.Competence, 10, false));
        Assert.True(TestDepthValueInsight.ShowsPartialResultPage(true, AssessmentKind.Competence, 10, false));
        Assert.False(TestDepthValueInsight.ShowsPartialResultPage(true, AssessmentKind.Competence, 25, true));
        Assert.False(TestDepthValueInsight.ShowsPartialResultPage(true, AssessmentKind.Career, 10, false));
    }

    [Fact]
    public void NearestUnfinishedRemaining_ignores_full_answered_draft()
    {
        var cards = new List<CandidateDnaViewBuilder.DnaCard>
        {
            new(
                AssessmentKind.Competence,
                "Competency.Title",
                Completed: false,
                Extended: false,
                DetailHref: "/x",
                StartHref: "/y",
                Answered: 25,
                QuestionCount: 25,
                MinutesApprox: 5,
                IsProvisional: true,
                TopLabel: "A",
                Summary: "s",
                Axes: [],
                Bars: [],
                Poles: [])
        };
        Assert.Equal(0, CandidateDnaViewBuilder.NearestUnfinishedRemaining(cards));
    }
}
