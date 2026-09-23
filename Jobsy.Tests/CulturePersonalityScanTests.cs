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
    public void DeepAnalysis_Does_Not_Support_Culture()
    {
        Assert.False(DeepAnalysisCatalog.SupportsDeepAnalysis(AssessmentKind.Culture));
        Assert.Throws<InvalidOperationException>(() => DeepAnalysisCatalog.QuestionsFor(AssessmentKind.Culture));
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
    public void Kompas_Uses_Culture_Score_Panel()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("CultureScorePanel", kompas, StringComparison.Ordinal);
        Assert.DoesNotContain("DiscScorePanel", kompas, StringComparison.Ordinal);
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CultureScorePanel.razor"));
        Assert.Contains("CulturePersonalityCatalog", panel, StringComparison.Ordinal);
    }

    [Fact]
    public void AssessmentKind_Parses_Legacy_Disc_Slug_As_Culture()
    {
        Assert.True(AssessmentKindLabels.TryParse("disc", out var kind));
        Assert.Equal(AssessmentKind.Culture, kind);
        Assert.Equal("culture", AssessmentKindLabels.ToSlug(AssessmentKind.Culture));
    }
}
