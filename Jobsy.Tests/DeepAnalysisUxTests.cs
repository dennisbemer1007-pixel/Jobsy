using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class DeepAnalysisUxTests
{
    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Disc)]
    public void Every_deep_question_has_a_concrete_practice_example(AssessmentKind kind)
    {
        var questions = DeepAnalysisCatalog.QuestionsFor(kind);
        Assert.Equal(150, questions.Count);
        Assert.All(questions, q =>
        {
            var example = DeepAnalysisQuestionHelp.ExampleFor(q);
            Assert.False(string.IsNullOrWhiteSpace(example));
            Assert.Contains("Voorbeeld", example, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RIASEC", example, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("OCEAN", example, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Big Five", example, StringComparison.OrdinalIgnoreCase);
            Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(example));
        });
    }

    [Fact]
    public void Domain_labels_are_plain_language()
    {
        Assert.Equal("Nieuwe dingen proberen", DeepAnalysisQuestionHelp.DomainLabel("Openheid"));
        Assert.Equal("Voortouw nemen", DeepAnalysisQuestionHelp.DomainLabel(DiscTestCatalog.Dominant));
        Assert.False(string.IsNullOrWhiteSpace(
            DeepAnalysisQuestionHelp.DomainLabel(CareerTestCatalog.Social)));
    }

    [Fact]
    public void Boosters_fire_every_25_questions_with_friendly_copy()
    {
        Assert.Equal(25, DeepAnalysisBoosters.Interval);
        Assert.Equal([25, 50, 75, 100, 125], DeepAnalysisBoosters.Milestones);
        Assert.Null(DeepAnalysisBoosters.TryMessage(24));
        Assert.Null(DeepAnalysisBoosters.TryMessage(26));
        foreach (var milestone in DeepAnalysisBoosters.Milestones)
        {
            var message = DeepAnalysisBoosters.TryMessage(milestone);
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(message!));
            Assert.False(string.IsNullOrWhiteSpace(DeepAnalysisBoosters.TitleFor(milestone)));
        }
    }

    [Fact]
    public void Deep_analysis_page_exposes_progress_topics_info_and_boosters()
    {
        var root = RepoRoot.Find();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor"));
        Assert.Contains("deep-analysis-chrome", page, StringComparison.Ordinal);
        Assert.Contains("Deep.ProgressPercent", page, StringComparison.Ordinal);
        Assert.Contains("deep-analysis-topics", page, StringComparison.Ordinal);
        Assert.Contains("OpenExample", page, StringComparison.Ordinal);
        Assert.Contains("DeepAnalysisBoosters", page, StringComparison.Ordinal);
        Assert.Contains("LobsyDialogMood.Celebrate", page, StringComparison.Ordinal);
        Assert.Contains("competency-test__meter", page, StringComparison.Ordinal);

        Assert.Equal("{0}% voltooid", Jobsy.Web.Localization.UiStrings.Get("Deep.ProgressPercent", "nl"));
        Assert.Equal("Voorbeeld uit de praktijk", Jobsy.Web.Localization.UiStrings.Get("Deep.InfoTitle", "nl"));

        var dto = File.ReadAllText(Path.Combine(root, "Jobsy.Core/Interfaces/IDeepAnalysisService.cs"));
        Assert.Contains("ExampleNl", dto, StringComparison.Ordinal);
        Assert.Contains("DomainLabel", dto, StringComparison.Ordinal);
    }
}
