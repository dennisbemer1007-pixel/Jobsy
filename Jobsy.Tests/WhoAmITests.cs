using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class WhoAmITests
{
    [Fact]
    public void Profile_needs_name_background_and_travel()
    {
        var empty = new CandidatePreferencesDto([], null, null);
        Assert.False(WhoAmICompleteness.IsProfileFilled("Ada", empty));
        var travelOnly = new CandidatePreferencesDto([], 30, "Fiets");
        Assert.False(WhoAmICompleteness.IsProfileFilled("Ada", travelOnly));
        var filled = new CandidatePreferencesDto([], 30, "Fiets", AboutMe: "Ik werk graag in de kas.");
        Assert.True(WhoAmICompleteness.IsProfileFilled("Ada", filled));
        Assert.False(WhoAmICompleteness.IsProfileFilled(" ", filled));

        var motivation = new CandidatePreferencesDto([], 30, "Fiets", DefaultMotivation: "Ik wil graag in de kas werken.");
        Assert.True(WhoAmICompleteness.IsProfileFilled("Ada", motivation));
        var certificate = new CandidatePreferencesDto([], 30, "Fiets", Certificates: [new CandidateCertificateDto("VCA")]);
        Assert.True(WhoAmICompleteness.IsProfileFilled("Ada", certificate));
        Assert.True(WhoAmICompleteness.IsProfileFilled(null, "Ada", "Lovelace", travelOnly, hasUploadedCv: true));
    }

    [Fact]
    public void Profile_json_keeps_background_when_a_sibling_field_is_malformed()
    {
        var json = """{"maxTravelMinutes":30,"preferredTransport":"Fiets","aboutMe":"Ik werk graag in de kas.","availability":{"Ma":"Ochtend"}}""";
        var strict = MatchingProfileMapper.DeserializePrefs(json);
        Assert.True(string.IsNullOrWhiteSpace(strict.AboutMe));
        Assert.True(WhoAmICompleteness.IsProfileFilled("Ada", null, null, json));
        Assert.False(WhoAmICompleteness.IsProfileFilled("Ada", null, null, """{"maxTravelMinutes":30,"preferredTransport":"Fiets"}"""));
    }

    [Fact]
    public void Story_is_first_person_without_jargon_or_pii()
    {
        var story = WhoAmIStoryBuilder.Build(
            new CompetencyScores(88, 70, 72, 40),
            new RiasecScores(20, 30, 25, 95, 40, 35),
            new CulturePersonalityScores(
            Autonomy: 70, Informal: 60, Collaboration: 80, Flexibility: 55, Innovation: 50, PeopleFirst: 65,
            Openness: 55, Conscientiousness: 70, Extraversion: 60, Agreeableness: 75, EmotionalStability: 70));
        Assert.Contains("Ik", story, StringComparison.Ordinal);
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(story));
        Assert.DoesNotContain("@", story, StringComparison.Ordinal);
        Assert.Null(WhoAmIStoryBuilder.Sanitize("Mail me op ada@test.local alsjeblieft"));
        Assert.Null(WhoAmIStoryBuilder.Sanitize("Mijn DISC-profiel is rood."));
    }

    [Fact]
    public void Prompt_has_everyday_labels_and_no_contact_fields()
    {
        var user = WhoAmIPrompt.User(
            new CompetencyScores(88, 70, 72, 40),
            new RiasecScores(20, 30, 25, 95, 40, 35),
            new CulturePersonalityScores(
            Autonomy: 70, Informal: 60, Collaboration: 80, Flexibility: 55, Innovation: 50, PeopleFirst: 65,
            Openness: 55, Conscientiousness: 70, Extraversion: 60, Agreeableness: 75, EmotionalStability: 70));
        Assert.Contains("samenwerken", user, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("zelfstandig", user, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", user, StringComparison.Ordinal);
        Assert.DoesNotContain("ada", user, StringComparison.OrdinalIgnoreCase);
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon(WhoAmIPrompt.System));
        Assert.Contains("ik-vorm", WhoAmIPrompt.System, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Kompas_surfaces_whoami_tab_and_cv_checkbox()
    {
        var root = RepoRoot.Find();
        var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("Kompas.TabWhoAmI", kompas, StringComparison.Ordinal);
        Assert.Contains("WhoAmIPanel", kompas, StringComparison.Ordinal);
        Assert.Equal("Wie ben ik?", Jobsy.Web.Localization.UiStrings.Get("Kompas.TabWhoAmI", "nl"));
        Assert.Equal(CandidateKompasTabs.WhoAmI, CandidateKompasTabs.All[0]);
        Assert.Equal(CandidateKompasTabs.WhoAmI, CandidateKompasTabs.Neighbor(CandidateKompasTabs.Fit, 1));
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/WhoAmIPanel.razor"));
        Assert.Contains("WhoAmI.AttachCv", panel, StringComparison.Ordinal);
        Assert.Contains("CompetencyScorePanel", panel, StringComparison.Ordinal);
        Assert.Contains("CultureScorePanel", panel, StringComparison.Ordinal);
        Assert.Contains("RiasecScorePanel", panel, StringComparison.Ordinal);
        Assert.Contains("Career.ScienceNote", panel, StringComparison.Ordinal);
        Assert.Contains("Competency.ScienceNote", panel, StringComparison.Ordinal);
        Assert.Contains("CultureScan.ScienceNote", panel, StringComparison.Ordinal);
        Assert.Contains("WhoAmI.WorkExperience", panel, StringComparison.Ordinal);
        Assert.Contains("OnParametersSetAsync", panel, StringComparison.Ordinal);
        Assert.Contains("Common.Retry", panel, StringComparison.Ordinal);
        Assert.Contains("HomeDashboardLoad.IsTransient", panel, StringComparison.Ordinal);
        Assert.Contains("Active=", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor")), StringComparison.Ordinal);
    }

    [Fact]
    public void WhoAmI_get_uses_read_rate_limit_not_ai()
    {
        var root = RepoRoot.Find();
        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/CandidateWhoAmIController.cs"));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain(
            """
            [HttpGet]
                [EnableRateLimiting("ai")]
            """,
            controller.Replace("\r\n", "\n"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void WhoAmI_openai_client_stays_under_api_client_budget()
    {
        var root = RepoRoot.Find();
        var di = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/DependencyInjection.cs"));
        Assert.Contains("WhoAmIGenerationService.HttpClientName", di, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromSeconds(8)", di, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "WhoAmIGenerationService.HttpClientName, client =>\n        {\n            client.Timeout = TimeSpan.FromSeconds(25);",
            di.Replace("\r\n", "\n"),
            StringComparison.Ordinal);

        var generation = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/WhoAmIGenerationService.cs"));
        Assert.Contains("CancelAfter(TimeSpan.FromSeconds(7))", generation, StringComparison.Ordinal);
    }

    [Fact]
    public void Profile_paints_matches_before_reverse_geocode_finishes()
    {
        var root = RepoRoot.Find();
        var profile = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("LoadMatchedVacanciesAsync()", profile, StringComparison.Ordinal);
        Assert.Contains("await InvokeAsync(StateHasChanged)", profile, StringComparison.Ordinal);
        var matchesIdx = profile.IndexOf("await LoadMatchedVacanciesAsync();", StringComparison.Ordinal);
        var paintIdx = profile.IndexOf("await InvokeAsync(StateHasChanged);", matchesIdx, StringComparison.Ordinal);
        var geoIdx = profile.IndexOf("Geocoder.ReverseAsync", matchesIdx, StringComparison.Ordinal);
        Assert.True(matchesIdx >= 0 && paintIdx > matchesIdx);
        Assert.True(geoIdx < 0 || geoIdx > paintIdx);
        Assert.Contains("_matchesLoading = false", profile, StringComparison.Ordinal);
    }
}
