using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class KompasDnaLandingTests
{
    [Fact]
    public void Template_sentences_cover_all_tests_and_levels()
    {
        var entries = DnaSummarySentences.AllEntries();
        Assert.True(entries.Count >= 38);
        Assert.Contains(entries, e => e.Key.StartsWith("competence.Samenwerken.", StringComparison.Ordinal));
        Assert.Contains(entries, e => e.Key.StartsWith("competence.Extraversie.", StringComparison.Ordinal));
        Assert.Contains(entries, e => e.Key == "career.Social");
        Assert.Contains(entries, e => e.Key == "culture.Autonomy.high");
        Assert.Contains(entries, e => e.Key == "values.Impact");
        Assert.All(entries, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Sentence));
            Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(e.Sentence));
        });

        Assert.Contains("sterke kant", DnaSummarySentences.Competence(CompetencyTestCatalog.Samenwerken, 80));
        Assert.Contains("handen", DnaSummarySentences.Career(CareerTestCatalog.Realistic));
        Assert.Contains("zelfstandig", DnaSummarySentences.Culture(CulturePersonalityCatalog.Autonomy, 75), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("zekerheid", DnaSummarySentences.Values(SchwartzValuesCatalog.Stability), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WhoAmI_story_summary_is_read_only_without_ai()
    {
        var empty = CandidateKompasService.BuildWhoAmIStory(
            null, null, null, null, false,
            profileFilled: false,
            competencyDone: false,
            careerDone: false,
            cultureDone: false,
            null, null, null, null);
        Assert.Equal(WhoAmIStoryStatuses.Empty, empty.Status);
        Assert.Null(empty.Story);

        var ready = CandidateKompasService.BuildWhoAmIStory(
            "Ik werk graag samen.",
            """["samenwerken"]""",
            DateTime.UtcNow.AddDays(-2),
            "other-fingerprint",
            fromOpenAi: true,
            profileFilled: false,
            competencyDone: false,
            careerDone: false,
            cultureDone: false,
            null, null, null, null);
        Assert.Equal(WhoAmIStoryStatuses.Ready, ready.Status);
        Assert.Equal("Ik werk graag samen.", ready.Story);
        Assert.Contains("samenwerken", ready.Keywords);

        var competency = new CompetencyScores(80, 70, 65, 55, 60);
        var career = new RiasecScores(40, 30, 20, 90, 35, 25);
        var culture = new CulturePersonalityScores(
            Autonomy: 70, Informal: 60, Collaboration: 80, Flexibility: 55, Innovation: 50, PeopleFirst: 65,
            Openness: 55, Conscientiousness: 70, Extraversion: 60, Agreeableness: 75, EmotionalStability: 70);
        var fingerprint = WhoAmICompleteness.Fingerprint(competency, career, culture, WhoAmIProfileHighlights.Empty);
        var updating = CandidateKompasService.BuildWhoAmIStory(
            "Ik werk graag samen.",
            "[]",
            DateTime.UtcNow.AddDays(-2),
            "stale",
            fromOpenAi: true,
            profileFilled: true,
            competencyDone: true,
            careerDone: true,
            cultureDone: true,
            competency,
            career,
            culture,
            null);
        Assert.Equal(WhoAmIStoryStatuses.Updating, updating.Status);
        Assert.False(string.IsNullOrWhiteSpace(updating.Story));

        var matched = CandidateKompasService.BuildWhoAmIStory(
            "Ik werk graag samen.",
            "[]",
            DateTime.UtcNow.AddDays(-2),
            fingerprint,
            fromOpenAi: true,
            profileFilled: true,
            competencyDone: true,
            careerDone: true,
            cultureDone: true,
            competency,
            career,
            culture,
            null);
        Assert.Equal(WhoAmIStoryStatuses.Ready, matched.Status);
    }

    [Fact]
    public void Kompas_dto_and_controller_expose_story_without_ai_rate_limit()
    {
        var root = RepoRoot.Find();
        var iface = File.ReadAllText(Path.Combine(root, "Jobsy.Core/Interfaces/ICandidateKompasService.cs"));
        Assert.Contains("WhoAmIStorySummaryDto", iface);
        Assert.Contains("ProfileCompletenessPercent", iface);

        var service = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/CandidateKompasService.cs"));
        Assert.Contains("CandidateWhoAmIProfiles", service);
        Assert.Contains("BuildWhoAmIStory", service);
        Assert.DoesNotContain("IWhoAmIGenerationService", service);
        Assert.DoesNotContain("WhoAmIGenerationService", service);
        Assert.DoesNotContain("_queue.TryEnqueue", service);

        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/CandidateKompasController.cs"));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", controller);
        Assert.DoesNotContain("EnableRateLimiting(\"ai\")", controller);

        var webModel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Models/CompetencyModels.cs"));
        Assert.Contains("WhoAmIStorySummary", webModel);
        Assert.Contains("ProfileCompletenessPercent", webModel);
    }

    [Fact]
    public void Profiel_redirects_to_candidate_profile_and_nav_points_there()
    {
        var root = RepoRoot.Find();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateProfile.razor"));
        Assert.Contains("@page \"/profiel\"", page);
        Assert.Contains("NavigateTo(\"/candidate/profile\"", page);
        Assert.DoesNotContain("profile-hub-kompas", page);

        var nav = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Navigation/RoleNavCatalog.cs"));
        Assert.Contains("new(\"Nav.Profile\", \"/candidate/profile\"", nav);
        Assert.Contains("\"/profiel\"", nav);

        Assert.Equal("/candidate/profile", RoleNavCatalog.Candidate.First(i => i.TitleKey == "Nav.Profile").Href);
    }

    [Fact]
    public void Dna_panel_has_story_and_summary_cards_without_old_list()
    {
        var root = RepoRoot.Find();
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DnaPanel.razor"));
        Assert.Contains("Dna.StoryTitle", panel);
        Assert.Contains("Dna.TestsTitle", panel);
        Assert.Contains("dna-tests__grid", panel);
        Assert.Contains("DnaSummarySentences", panel);
        Assert.Contains("Test.Status.Extended", panel);
        Assert.Contains("Dna.StartTest", panel);
        Assert.DoesNotContain("dna-summary__item", panel);
        Assert.DoesNotContain("Dna.ViewTests", panel);

        var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("Kompas.CompletenessLine", kompas);
        Assert.Contains("ShouldShowSide", kompas);
        Assert.Contains("jobsyViewport.isKompasWide", kompas);
    }

    [Fact]
    public void Completeness_percent_scales_with_steps()
    {
        Assert.Equal(0, KompasProfileCompleteness.Percent(false, false, false, false, false, false));
        Assert.Equal(100, KompasProfileCompleteness.Percent(true, true, true, true, true, true));
        Assert.Equal(50, KompasProfileCompleteness.Percent(true, true, true, false, false, false));
    }

    [Fact]
    public void Competence_domain_labels_use_plain_dutch()
    {
        Assert.Equal("Samen & aardig", DeepAnalysisQuestionHelp.DomainLabel(CompetencyTestCatalog.Samenwerken));
        Assert.Equal("Nieuwe dingen proberen", DeepAnalysisQuestionHelp.DomainLabel(CompetencyTestCatalog.Innovatie));
        Assert.Equal("Kalm blijven", DeepAnalysisQuestionHelp.DomainLabel(CompetencyTestCatalog.Stressbestendigheid));
    }
}
