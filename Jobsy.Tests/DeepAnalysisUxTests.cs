using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class DeepAnalysisUxTests
{
    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Values)]
    [InlineData(AssessmentKind.Culture)]
    public void Every_deep_question_has_a_concrete_practice_example(AssessmentKind kind)
    {
        var questions = DeepAnalysisCatalog.QuestionsFor(kind);
        Assert.Equal(DeepAnalysisCatalog.QuestionCountFor(kind), questions.Count);
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
        Assert.False(string.IsNullOrWhiteSpace(
            DeepAnalysisQuestionHelp.DomainLabel(CareerTestCatalog.Social)));
    }

    [Fact]
    public void Motivation_replaces_modal_boosters()
    {
        Assert.Equal(DeepTestMotivation.Halfway,
            DeepTestMotivation.ForProgress(AssessmentKind.Competence, 75, 150,
                DeepTestMotivation.PartIndexForGlobalIndex(AssessmentKind.Competence, 75)));
        Assert.True(DeepTestMotivation.IsPausePoint(AssessmentKind.Competence, 30));
    }

    [Fact]
    public void Deep_analysis_page_uses_question_flow_shell_not_modals()
    {
        var root = RepoRoot.Find();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor"));
        Assert.Contains("TestQuestionFlow", page, StringComparison.Ordinal);
        Assert.Contains("TestPageShell", page, StringComparison.Ordinal);
        Assert.Contains("DeepTestMotivation", page, StringComparison.Ordinal);
        Assert.Contains("QuestionnaireAutosave", page, StringComparison.Ordinal);
        Assert.DoesNotContain("DeepAnalysisBoosters", page, StringComparison.Ordinal);
        Assert.DoesNotContain("LobsyDialogMood.Celebrate", page, StringComparison.Ordinal);

        Assert.Equal("Voorbeeld uit de praktijk", Jobsy.Web.Localization.UiStrings.Get("TestFlow.Example", "nl"));
        Assert.Equal("Duik tot de bodem", Jobsy.Web.Localization.UiStrings.Get("Deep.Offer.Title", "nl"));

        var dto = File.ReadAllText(Path.Combine(root, "Jobsy.Core/Interfaces/IDeepAnalysisService.cs"));
        Assert.Contains("ExampleNl", dto, StringComparison.Ordinal);
        Assert.Contains("DomainLabel", dto, StringComparison.Ordinal);
    }
}
