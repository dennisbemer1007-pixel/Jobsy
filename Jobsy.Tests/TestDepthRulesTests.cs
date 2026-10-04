using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class TestDepthRulesTests
{
    [Theory]
    [InlineData(AssessmentKind.Competence, 25)]
    [InlineData(AssessmentKind.Career, 25)]
    [InlineData(AssessmentKind.Values, 25)]
    [InlineData(AssessmentKind.Culture, 18)]
    public void FullCount_MatchesCatalog(AssessmentKind kind, int expected)
        => Assert.Equal(expected, TestDepthRules.FullCount(kind));

    [Fact]
    public void ResumeQuestionNumber_uses_the_first_gap_not_the_answer_count()
    {
        var ordered = TestDepthRules.QuestionIdsUpTo(AssessmentKind.Competence, 25);
        var answers = new Dictionary<int, int>();
        for (var i = 0; i < 11; i++)
        {
            if (i == 4)
            {
                continue;
            }

            answers[ordered[i]] = 3;
        }

        Assert.Equal(10, answers.Count);
        Assert.Equal(5, TestDepthRules.ResumeQuestionNumber(AssessmentKind.Competence, answers));
        Assert.NotEqual(answers.Count + 1, TestDepthRules.ResumeQuestionNumber(AssessmentKind.Competence, answers));
    }

    [Fact]
    public void Bottom_Career_Is200()
        => Assert.Equal(200, TestDepthRules.Bottom(AssessmentKind.Career));

    [Theory]
    [InlineData(4, null)]
    [InlineData(5, TestDepthLevel.First)]
    [InlineData(9, TestDepthLevel.First)]
    [InlineData(10, TestDepthLevel.Deeper)]
    [InlineData(24, TestDepthLevel.Deeper)]
    [InlineData(25, TestDepthLevel.Full)]
    public void Reached_Competence(int answered, TestDepthLevel? expected)
        => Assert.Equal(expected, TestDepthRules.Reached(AssessmentKind.Competence, answered));

    [Theory]
    [InlineData(17, TestDepthLevel.Deeper)]
    [InlineData(18, TestDepthLevel.Full)]
    public void Reached_Culture(int answered, TestDepthLevel? expected)
        => Assert.Equal(expected, TestDepthRules.Reached(AssessmentKind.Culture, answered));

    [Fact]
    public void QuestionIds_Deeper_NoDuplicates_Valid_CareerIncludesArtistic()
    {
        var ids = TestDepthRules.QuestionIdsFor(AssessmentKind.Career, TestDepthLevel.Deeper);
        Assert.Equal(10, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
        var catalog = CareerTestCatalog.Questions.Select(q => q.Id).ToHashSet();
        Assert.All(ids, id => Assert.Contains(id, catalog));
        Assert.Contains(10, ids); // Artistic
    }

    [Fact]
    public void MinutesFor_MatchesContract()
    {
        Assert.Equal(1, TestDepthRules.MinutesFor(AssessmentKind.Competence, TestDepthLevel.First));
        Assert.Equal(2, TestDepthRules.MinutesFor(AssessmentKind.Competence, TestDepthLevel.Deeper));
        Assert.Equal(6, TestDepthRules.MinutesFor(AssessmentKind.Competence, TestDepthLevel.Full));
        Assert.Equal(4, TestDepthRules.MinutesFor(AssessmentKind.Culture, TestDepthLevel.Full));
        Assert.Equal(30, TestDepthRules.MinutesFor(AssessmentKind.Competence, TestDepthLevel.Bottom));
        Assert.Equal(40, TestDepthRules.MinutesFor(AssessmentKind.Career, TestDepthLevel.Bottom));
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void Parts_AreFive_CoverAllIdsOnce(AssessmentKind kind)
    {
        var parts = TestDepthRules.Parts(kind);
        Assert.Equal(5, parts.Count);
        var all = parts.SelectMany(p => p.QuestionIds).ToList();
        var expected = DeepAnalysisCatalog.QuestionsFor(kind).Select(q => q.Id).ToList();
        Assert.Equal(expected.Count, all.Count);
        Assert.Equal(expected.Count, all.Distinct().Count());
        Assert.True(all.ToHashSet().SetEquals(expected));
        if (kind is AssessmentKind.Competence or AssessmentKind.Values)
        {
            Assert.All(parts, p => Assert.Equal(30, p.Size));
        }
        else if (kind == AssessmentKind.Career)
        {
            // 6 RIASEC domains of ~33–34 into 5 parts without splitting ⇒ one part holds two domains.
            Assert.Equal(200, parts.Sum(p => p.Size));
            Assert.All(parts, p => Assert.InRange(p.Size, 30, 80));
        }
        else
        {
            Assert.Equal(150, parts.Sum(p => p.Size));
        }
    }

    [Fact]
    public void StripIdPrefix_RemovesLeadingNumber()
    {
        Assert.Equal(
            "Ik werk graag samen",
            Jobsy.Web.Components.Shared.Questionnaire.TestQuestionFlow.StripIdPrefix("1. Ik werk graag samen"));
        Assert.Equal(
            "Geen prefix",
            Jobsy.Web.Components.Shared.Questionnaire.TestQuestionFlow.StripIdPrefix("Geen prefix"));
    }
}
