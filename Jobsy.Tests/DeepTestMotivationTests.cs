using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class DeepTestMotivationTests
{
    [Theory]
    [InlineData(AssessmentKind.Competence, 75, 150)]
    [InlineData(AssessmentKind.Career, 100, 200)]
    public void Halfway_At_Half_Of_Total(AssessmentKind kind, int half, int total)
    {
        var parts = TestDepthRules.Parts(kind);
        var partIndex = DeepTestMotivation.PartIndexForGlobalIndex(kind, half);
        Assert.Equal(DeepTestMotivation.Halfway, DeepTestMotivation.ForProgress(kind, half, total, partIndex));
        Assert.Equal(5, parts.Count);
    }

    [Fact]
    public void PartStart_On_First_Of_Part()
    {
        Assert.Equal(
            DeepTestMotivation.PartStart,
            DeepTestMotivation.ForProgress(AssessmentKind.Competence, 0, 150, 1));
    }

    [Fact]
    public void LastPart_Cue_On_First_Of_Part_5()
    {
        var parts = TestDepthRules.Parts(AssessmentKind.Competence);
        var beforeLast = parts.Take(4).Sum(p => p.Size);
        Assert.Equal(
            DeepTestMotivation.LastPart,
            DeepTestMotivation.ForProgress(AssessmentKind.Competence, beforeLast, 150, 5));
    }

    [Fact]
    public void BeforePause_On_Last_Question_Of_Part_1()
    {
        var part1 = TestDepthRules.Parts(AssessmentKind.Competence)[0];
        var answered = part1.Size - 1;
        Assert.Equal(
            DeepTestMotivation.BeforePause,
            DeepTestMotivation.ForProgress(AssessmentKind.Competence, answered, 150, 1));
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void Pause_After_Parts_1_To_4(AssessmentKind kind)
    {
        var parts = TestDepthRules.Parts(kind);
        var running = 0;
        for (var i = 0; i < 4; i++)
        {
            running += parts[i].Size;
            Assert.True(DeepTestMotivation.IsPausePoint(kind, running));
        }

        Assert.False(DeepTestMotivation.IsPausePoint(kind, parts.Sum(p => p.Size)));
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void Parts_Cover_Every_Id_Once(AssessmentKind kind)
    {
        var parts = TestDepthRules.Parts(kind);
        var all = parts.SelectMany(p => p.QuestionIds).ToList();
        var expected = DeepAnalysisCatalog.QuestionsFor(kind).Select(q => q.Id).ToList();
        Assert.Equal(expected.Count, all.Count);
        Assert.Equal(expected.Count, all.Distinct().Count());
        Assert.True(all.ToHashSet().SetEquals(expected));
    }
}
