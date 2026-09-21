using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class DiscAnalysisTests
{
    [Fact]
    public void Quick_scan_has_25_items_across_four_styles()
    {
        Assert.Equal(25, DiscTestCatalog.QuestionCount);
        Assert.Equal(4, DiscTestCatalog.CategoryCodes.Length);
        Assert.Equal(7, DiscTestCatalog.Questions.Count(q => q.Category == DiscTestCatalog.Dominant));
        foreach (var category in new[] { DiscTestCatalog.Invloed, DiscTestCatalog.Stabiel, DiscTestCatalog.Nauwkeurig })
        {
            Assert.Equal(6, DiscTestCatalog.Questions.Count(q => q.Category == category));
        }

        Assert.Contains(DiscTestCatalog.Questions, q => q.Reverse);
    }

    [Fact]
    public void Score_maps_likert_and_reverse_items()
    {
        var high = Enumerable.Range(1, 25).ToDictionary(
            i => i,
            i => DiscTestCatalog.Questions.First(q => q.Id == i).Reverse ? 1 : 5);
        var scores = DiscTestCatalog.Score(high);
        Assert.NotNull(scores);
        Assert.True(scores!.IsComplete);
        Assert.Equal(100, scores.Dominant);
        Assert.Equal(100, scores.Invloed);
        Assert.Equal(100, scores.Stabiel);
        Assert.Equal(100, scores.Nauwkeurig);
    }

    [Fact]
    public void Deep_disc_catalog_is_150_unique_workplace_items()
    {
        var questions = DeepAnalysisCatalog.QuestionsFor(AssessmentKind.Disc);
        Assert.Equal(150, questions.Count);
        Assert.Equal(150, questions.Select(q => q.PromptNl).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(questions, q => q.PromptNl.Contains("variant", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, questions.Select(q => q.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Role_fit_adds_plain_language_disc_lines_without_jargon()
    {
        var without = RoleFitCheckBuilder.Build(
            "Verpleegkundige",
            new CompetencyScores(88, 70, 72, 40),
            new RiasecScores(20, 30, 25, 95, 40, 35),
            fromDeepAnalysis: false);
        var withDisc = RoleFitCheckBuilder.Build(
            "Verpleegkundige",
            new CompetencyScores(88, 70, 72, 40),
            new RiasecScores(20, 30, 25, 95, 40, 35),
            fromDeepAnalysis: false,
            new DiscScores(40, 88, 80, 50));
        Assert.NotEqual(without.MatchPercent, withDisc.MatchPercent);
        Assert.Contains(withDisc.Strengths, s => s.Contains("mensen meenemen", StringComparison.OrdinalIgnoreCase)
                                                || s.Contains("rust en ritme", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(withDisc.Strengths, CareerCompassBuilder.ContainsForbiddenJargon);
        Assert.DoesNotContain(withDisc.Gaps, CareerCompassBuilder.ContainsForbiddenJargon);
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon("DISC-profiel"));
    }

    [Fact]
    public void Kompas_and_training_surface_the_disc_tab()
    {
        var root = RepoRoot.Find();
        var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("Kompas.TabDisc", kompas, StringComparison.Ordinal);
        Assert.Contains("DiscScorePanel", kompas, StringComparison.Ordinal);
        Assert.DoesNotContain("Talent.CandidateTitle", kompas, StringComparison.Ordinal);
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DiscScorePanel.razor"));
        Assert.Contains("TrainingOffersBlock", panel, StringComparison.Ordinal);
        Assert.Contains("CampaignDisc", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("OCEAN", panel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RIASEC", panel, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("DISC-Analyse", Jobsy.Web.Localization.UiStrings.Get("Kompas.TabDisc", "nl"));
        Assert.Equal(CandidateKompasTabs.Disc, CandidateKompasTabs.Neighbor(CandidateKompasTabs.Competencies, 1));
        Assert.Contains("samenwerken", DiscTrainingCatalog.SearchBlob(DiscTestCatalog.Stabiel), StringComparison.Ordinal);
    }
}
