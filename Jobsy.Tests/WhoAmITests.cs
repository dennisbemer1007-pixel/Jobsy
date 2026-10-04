using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using Jobsy.Web.Navigation;
using Microsoft.EntityFrameworkCore;

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
        foreach (var sentence in story.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries))
        {
            var words = sentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(words.Length <= 20, sentence);
        }
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
        Assert.Contains("Samen", user, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Zelf kiezen", user, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", user, StringComparison.Ordinal);
        Assert.DoesNotContain("ada", user, StringComparison.OrdinalIgnoreCase);
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon(WhoAmIPrompt.System));
        Assert.Contains("ik-vorm", WhoAmIPrompt.System, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Kompas_surfaces_dna_and_tests_tabs()
    {
        var root = RepoRoot.Find();
        var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/CandidateKompas.razor"));
        Assert.Contains("Kompas.TabDna", kompas, StringComparison.Ordinal);
        Assert.Contains("DnaPanel", kompas, StringComparison.Ordinal);
        Assert.Contains("TestsOverviewPanel", kompas, StringComparison.Ordinal);
        Assert.Equal("Wie ik ben", Jobsy.Web.Localization.UiStrings.Get("Kompas.TabDna", "nl"));
        Assert.Equal(CandidateKompasTabs.Dna, CandidateKompasTabs.All[0]);
        Assert.Equal(CandidateKompasTabs.Dna, CandidateKompasTabs.Neighbor(CandidateKompasTabs.Fit, 1));
        Assert.Equal(4, CandidateKompasTabs.All.Length);
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/DnaPanel.razor"));
        Assert.Contains("Dna.StoryEmpty", panel, StringComparison.Ordinal);
        Assert.Contains("Dna.TestsTitle", panel, StringComparison.Ordinal);
        Assert.Contains("GetMyKompasDnaResultAsync", panel, StringComparison.Ordinal);
        Assert.Contains("OnParametersSetAsync", panel, StringComparison.Ordinal);
        Assert.Contains("WhoAmIStoryStatuses", panel, StringComparison.Ordinal);
        Assert.Contains("Active=", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/CandidateKompas.razor")), StringComparison.Ordinal);
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

    [Fact]
    public async Task Unlocked_profile_gets_a_local_story_instead_of_staying_pending()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "sanne@test.nl",
            FullName = "Sanne Test",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = """{"maxTravelMinutes":30,"preferredTransport":"Fiets","aboutMe":"Ik werk graag in het magazijn."}"""
        });
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            SamenwerkenPercent = 70,
            ResultaatgerichtheidPercent = 80,
            StressbestendigheidPercent = 60,
            InnovatiePercent = 55
        });
        db.CandidateCareerInterests.Add(new CandidateCareerInterest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            RealisticPercent = 40,
            InvestigativePercent = 30,
            ArtisticPercent = 20,
            SocialPercent = 55,
            EnterprisingPercent = 81,
            ConventionalPercent = 70
        });
        db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AutonomyPercent = 50,
            InformalPercent = 60,
            CollaborationPercent = 70,
            FlexibilityPercent = 55,
            InnovationPercent = 40,
            PeopleFirstPercent = 65,
            OpennessPercent = 50,
            ConscientiousnessPercent = 72,
            ExtraversionPercent = 48,
            AgreeablenessPercent = 66,
            EmotionalStabilityPercent = 58
        });
        await db.SaveChangesAsync();

        var sut = new WhoAmIService(db, new NoopInsightsQueue());
        var state = await sut.GetAsync(userId);

        Assert.False(string.IsNullOrWhiteSpace(state.Story));
        Assert.Equal(InsightsStatuses.Ready, state.InsightsStatus);
        Assert.Contains("Ik", state.Story, StringComparison.Ordinal);
    }

    private sealed class NoopInsightsQueue : ICandidateInsightsQueue
    {
        public bool TryEnqueue(Guid userId) => true;

        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(Guid.Empty);

        public void MarkCompleted(Guid userId)
        {
        }
    }
}
