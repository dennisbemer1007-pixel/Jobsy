using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CulturePersonalityScanTests
{
    [Fact]
    public void Catalog_Has_Eighteen_Questions_Across_Culture_And_Personality()
    {
        Assert.Equal(18, CulturePersonalityCatalog.QuestionCount);
        Assert.Equal(6, CulturePersonalityCatalog.CultureDimensionCodes.Length);
        Assert.Equal(5, CulturePersonalityCatalog.PersonalityFacetCodes.Length);
        Assert.Equal(18, CulturePersonalityCatalog.Questions.Count);
        Assert.Contains(CulturePersonalityCatalog.Questions, q => q.Reverse);
    }

    [Fact]
    public void Score_Complete_Answers_Yields_Complete_Profile()
    {
        var answers = Enumerable.Range(1, 18).ToDictionary(i => i, _ => 5);
        var scores = CulturePersonalityCatalog.Score(answers);
        Assert.NotNull(scores);
        Assert.True(scores!.IsComplete);
        Assert.True(scores.Autonomy is >= 0 and <= 100);
    }

    [Fact]
    public void DeepAnalysis_Supports_Culture_With_150_Items()
    {
        Assert.True(DeepAnalysisCatalog.SupportsDeepAnalysis(AssessmentKind.Culture));
        Assert.Equal(150, DeepAnalysisCatalog.CultureQuestionCount);
        var questions = DeepAnalysisCatalog.QuestionsFor(AssessmentKind.Culture);
        Assert.Equal(150, questions.Count);
        Assert.Equal(150, questions.Select(q => q.PromptNl).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(questions, q => Assert.Equal("Culture", q.Family));
        Assert.DoesNotContain(questions, q =>
            q.PromptNl.Contains("DISC", StringComparison.OrdinalIgnoreCase)
            || q.PromptNl.Contains("Big Five", StringComparison.OrdinalIgnoreCase)
            || q.PromptNl.Contains("OCEAN", StringComparison.OrdinalIgnoreCase));

        foreach (var domain in CulturePersonalityCatalog.CultureDimensionCodes)
        {
            Assert.Equal(15, questions.Count(q => q.Domain == domain));
            Assert.True(questions.Count(q => q.Domain == domain && q.Reverse) >= 3);
        }

        foreach (var domain in CulturePersonalityCatalog.PersonalityFacetCodes)
        {
            Assert.Equal(12, questions.Count(q => q.Domain == domain));
            Assert.True(questions.Count(q => q.Domain == domain && q.Reverse) >= 3);
        }

        var answers = Enumerable.Range(1, 150).ToDictionary(i => i, _ => 5);
        var scores = DeepAnalysisCatalog.ToCulturePersonalityScores(
            DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Culture));
        Assert.True(scores.IsComplete);
    }

    [Fact]
    public void PersonalityFit_Blends_With_Vacancy_Keywords()
    {
        var scores = new CulturePersonalityScores(
            Autonomy: 50, Informal: 50, Collaboration: 50, Flexibility: 50, Innovation: 50, PeopleFirst: 50,
            Openness: 40, Conscientiousness: 90, Extraversion: 40, Agreeableness: 50, EmotionalStability: 60);
        var fit = CulturePersonalityFitRules.PersonalityFit01(scores, "Administratief medewerker", "kwaliteit en controle");
        Assert.True(fit > 0.7);
    }

    [Fact]
    public void Kompas_Routes_Culture_Through_Tests_Tab()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("TestsOverviewPanel", kompas, StringComparison.Ordinal);
        Assert.Contains("Kompas.TabTests", kompas, StringComparison.Ordinal);
        Assert.DoesNotContain("DiscScorePanel", kompas, StringComparison.Ordinal);
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CultureScorePanel.razor"));
        Assert.Contains("CulturePersonalityCatalog", panel, StringComparison.Ordinal);
        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/TestDetail.razor"));
        Assert.Contains("/profiel/tests/{TestKey}", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void AssessmentKind_Parses_Legacy_Disc_Slug_As_Culture()
    {
        Assert.True(AssessmentKindLabels.TryParse("disc", out var kind));
        Assert.Equal(AssessmentKind.Culture, kind);
        Assert.Equal("culture", AssessmentKindLabels.ToSlug(AssessmentKind.Culture));
    }
}
