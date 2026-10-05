using System.Globalization;
using System.Net;
using System.Security.Claims;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

/// <summary>Candidate run 13: culture isolation, honest match, compass, story and coach.</summary>
public class CandidateRun13Tests
{
    private static readonly RiasecScores Profile = new(66, 38, 37, 64, 43, 62);

    private static readonly CompetencyScores Competency = new(70, 45, 40, 30, 60);

    private static readonly CulturePersonalityScores Culture = new(
        Autonomy: 40, Informal: 55, Collaboration: 48, Flexibility: 42, Innovation: 30, PeopleFirst: 44,
        Openness: 40, Conscientiousness: 72, Extraversion: 48, Agreeableness: 46, EmotionalStability: 40);

    [Fact]
    public async Task Arabic_and_dutch_format_the_same_instant_on_separate_threads()
    {
        var beforeCulture = CultureInfo.DefaultThreadCurrentCulture;
        var beforeUi = CultureInfo.DefaultThreadCurrentUICulture;
        var gate = new Barrier(2);
        var results = new string[2];
        var calendarYears = new int[2];

        var arabic = Task.Run(() =>
        {
            JobsyCultures.ApplyToCurrentThread("ar");
            gate.SignalAndWait();
            var moment = new DateTime(2026, 10, 5, 4, 54, 0);
            results[0] = moment.ToString("dd-MM-yyyy HH:mm", CultureInfo.CurrentCulture);
            calendarYears[0] = CultureInfo.CurrentCulture.DateTimeFormat.Calendar.GetYear(moment);
        });
        var dutch = Task.Run(() =>
        {
            JobsyCultures.ApplyToCurrentThread("nl");
            gate.SignalAndWait();
            var moment = new DateTime(2026, 10, 5, 4, 54, 0);
            results[1] = moment.ToString("dd-MM-yyyy HH:mm", CultureInfo.CurrentCulture);
            calendarYears[1] = CultureInfo.CurrentCulture.DateTimeFormat.Calendar.GetYear(moment);
        });

        await Task.WhenAll(arabic, dutch);
        Assert.Equal("05-10-2026 04:54", results[0]);
        Assert.Equal("05-10-2026 04:54", results[1]);
        Assert.Equal(2026, calendarYears[0]);
        Assert.Equal(2026, calendarYears[1]);
        Assert.IsType<GregorianCalendar>(JobsyCultures.For("ar").DateTimeFormat.Calendar);
        Assert.Equal(beforeCulture, CultureInfo.DefaultThreadCurrentCulture);
        Assert.Equal(beforeUi, CultureInfo.DefaultThreadCurrentUICulture);
    }

    [Fact]
    public void Shown_match_never_exceeds_the_letters_the_job_uses()
    {
        Assert.Equal(64, CareerCompassBuilder.CatalogueFit("Chauffeur", Profile, "MBO"));
        Assert.Equal(66, CareerCompassBuilder.CatalogueFit("Medewerker bouw / afbouw", Profile, "MBO"));

        foreach (var job in CareerCompassBuilder.Occupations)
        {
            var letters = job.Weights
                .Where(weight => weight.Weight > 0)
                .OrderByDescending(weight => weight.Weight)
                .Take(3)
                .Select(weight => Profile.Get(weight.Code))
                .ToList();
            var match = CareerCompassBuilder.ProfileMatch(job.Weights, Profile);
            Assert.True(match <= letters.Max(), $"{job.Title} shows {match} above {letters.Max()}");
        }
    }

    [Fact]
    public void Stored_compass_with_a_different_fingerprint_is_cleared()
    {
        var answers = Enumerable.Range(1, CareerTestCatalog.QuestionCount).ToDictionary(id => id, _ => 4);
        var basic = CareerTestCatalog.Score(answers);
        Assert.NotNull(basic);
        var compass = CareerCompassBuilder.Build(basic!) with { ScoresFingerprint = "1|2|3|4|5|6" };
        var row = new CandidateCareerInterest
        {
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = CareerTestCatalog.SerializeAnswers(answers),
            CompassJson = CareerCompassJson.Serialize(compass)
        };

        Assert.True(CareerInterestDeepReset.ClearDeepCompass(row, DateTime.UtcNow));
        Assert.False(CareerInterestDeepReset.MarksDeepAnalysis(row.CompassJson));
        var stored = CareerCompassJson.TryDeserialize(row.CompassJson);
        Assert.True(stored is null || !stored.HasOccupations);
        Assert.False(CareerInterestDeepReset.ClearDeepCompass(row, DateTime.UtcNow));

        var fresh = CareerCompassBuilder.Build(basic);
        Assert.Equal(CareerCompassBuilder.ScoresKey(basic), fresh.ScoresFingerprint);
        var roundTrip = CareerCompassJson.TryDeserialize(CareerCompassJson.Serialize(fresh));
        Assert.Equal(fresh.ScoresFingerprint, roundTrip?.ScoresFingerprint);
    }

    [Fact]
    public void Single_newline_stories_are_accepted_at_least_half_the_time()
    {
        Assert.Equal("paragraphs", WhoAmIStoryBuilder.StoryRuleReason(
            "Ik ben iemand die taken afmaakt en dat elke dag laat zien in gewone woorden."));

        var stories = new[]
        {
            "Ik maak taken af en houd de dag overzichtelijk.\nIk werk het liefst met een duidelijke klus en een rustig tempo.",
            "Ik pak een klus aan en maak hem af.\nEen duidelijke volgorde helpt mij om door te gaan tot het klaar is.",
            "Ik houd de taken op orde en ik rond af waar ik aan begin.\nDat zie je terug in hoe ik een werkdag indeel.",
            "Ik ben iemand die taken afrondt.\nIk kies een klus, doe de stappen en lever het netjes op.",
            "Ik werk stap voor stap en ik maak af wat ik start.\nEen rustige ploeg waarin afspraken kloppen past bij mijn score.",
            "Ik kom tot mijn recht als de taak duidelijk is.\nIk maak het af en ik houd de lijst bij tot alles klopt.",
            "Ik doe het werk tot het klaar is en ik check de laatste stap.\nDat is hoe mijn score voor netjes werken eruitziet.",
            "Ik start een taak en ik stop pas als het resultaat klopt.\nSamenwerken past bij hoe ik scoor op mijn test.",
            "Ik plan de dag in kleine stappen en ik vink ze af.\nZo blijft het werk overzichtelijk en af.",
            "Ik ben betrouwbaar in afmaken en ik houd de afspraak.\nMijn test laat zien dat netjes werken bij mij hoort."
        };

        var passed = stories.Count(story =>
            WhoAmIStoryBuilder.Accepts(story, WhoAmIProfileHighlights.Empty, Competency, Culture, Profile));
        Assert.True(passed >= 5, $"Accepted {passed}/10 single-newline stories.");
        var stats = WhoAmIStoryBuilder.ParagraphStats(stories[0]);
        Assert.Contains("paragraphs=", stats, StringComparison.Ordinal);
        Assert.Contains("lengths=", stats, StringComparison.Ordinal);
        Assert.DoesNotContain(' ', stats);
    }

    [Fact]
    public void Keywords_use_the_same_codes_in_every_language_and_skip_scores_under_50()
    {
        var codes = WhoAmIKeywords.Codes(Competency, Profile, Culture);
        Assert.DoesNotContain(CompetencyTestCatalog.Stressbestendigheid, codes);
        Assert.DoesNotContain(CompetencyTestCatalog.Innovatie, codes);
        Assert.DoesNotContain(CulturePersonalityCatalog.EmotionalStability, codes);
        Assert.Contains(CulturePersonalityCatalog.Conscientiousness, codes);

        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            var labels = WhoAmIKeywords.FromScores(Competency, Profile, Culture, null, lang);
            Assert.Equal(codes.Count, labels.Count);
            Assert.DoesNotContain(labels, label => label.Contains("Kalm onder druk", StringComparison.Ordinal));
            Assert.DoesNotContain(labels, label => label.Contains("Calm under pressure", StringComparison.Ordinal));
        }

        var dutch = WhoAmIKeywords.FromScores(Competency, Profile, Culture, null, "nl");
        var english = WhoAmIKeywords.FromScores(Competency, Profile, Culture, null, "en");
        Assert.NotEqual(dutch, english);
        Assert.Contains("Neat and reliable", english);
    }

    [Fact]
    public void Work_line_translates_vanaf_and_keeps_the_dutch_title()
    {
        Assert.Equal("Chauffeur (Driver) since 2023", OccupationTitles.LocalizeWorkLine("Chauffeur vanaf 2023", "en"));
        Assert.Equal("Chauffeur (Driver) od 2023", OccupationTitles.LocalizeWorkLine("Chauffeur vanaf 2023", "pl"));
        Assert.Equal("Chauffeur (Driver) din 2023", OccupationTitles.LocalizeWorkLine("Chauffeur vanaf 2023", "ro"));
        Assert.Contains("منذ 2023", OccupationTitles.LocalizeWorkLine("Chauffeur vanaf 2023", "ar"), StringComparison.Ordinal);
        Assert.DoesNotContain("vanaf", OccupationTitles.LocalizeWorkLine("Vakkenvuller supermarkt vanaf 2023", "en"), StringComparison.Ordinal);
        Assert.Contains("since 2023", OccupationTitles.LocalizeWorkLine("Vakkenvuller supermarkt vanaf 2023", "en"), StringComparison.Ordinal);
    }

    [Fact]
    public void Dutch_story_hides_employer_lines_when_the_flag_is_off()
    {
        var off = WhoAmIStoryBuilder.Build(Competency, Profile, Culture, employersEnabled: false);
        var on = WhoAmIStoryBuilder.Build(Competency, Profile, Culture, employersEnabled: true);
        Assert.DoesNotContain("werkgever", off, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("werkgever", on, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Coach_answers_match_the_report_and_stay_in_the_ui_language()
    {
        var traits = new (string Code, int Score)[]
        {
            (CompetencyTestCatalog.Samenwerken, 70),
            (CompetencyTestCatalog.Extraversie, 60),
            (CompetencyTestCatalog.Resultaatgerichtheid, 45),
            (CompetencyTestCatalog.Stressbestendigheid, 40),
            (CompetencyTestCatalog.Innovatie, 30)
        };

        var strengths = CandidateJobAdvice.TryReply(
            "nl", "Wat zijn mijn sterkste punten?", Profile, "MBO", false, competence: traits);
        Assert.Contains("Samen & aardig (70%)", strengths, StringComparison.Ordinal);
        Assert.DoesNotContain("45%", strengths, StringComparison.Ordinal);
        Assert.DoesNotContain("Afmaken", strengths, StringComparison.Ordinal);

        var pitfalls = CandidateJobAdvice.TryReply("nl", "Wat zijn mijn valkuilen?", Profile, "MBO", false);
        Assert.Contains("Je valkuilen", pitfalls, StringComparison.Ordinal);
        Assert.Contains(DeepReportCatalog.Get("pitfall." + CareerTestCatalog.Realistic, "nl"), pitfalls, StringComparison.Ordinal);
        Assert.DoesNotContain("Flexibel", pitfalls, StringComparison.Ordinal);

        var reason = CandidateJobAdvice.TryReply("nl", "Waarom past chauffeur bij mij?", Profile, "MBO", false);
        Assert.Contains("66%", reason, StringComparison.Ordinal);
        Assert.Contains("62%", reason, StringComparison.Ordinal);
        Assert.DoesNotContain("Mensen helpen", reason, StringComparison.Ordinal);

        var polish = CandidateJobAdvice.TryReply("pl", "Dlaczego zawód kierowcy do mnie pasuje?", Profile, "MBO", false);
        Assert.Contains("66%", polish, StringComparison.Ordinal);
        Assert.Contains("62%", polish, StringComparison.Ordinal);
        Assert.DoesNotContain("Mensen helpen", polish, StringComparison.Ordinal);
        Assert.DoesNotContain("1.", polish, StringComparison.Ordinal);

        var romanian = CandidateJobAdvice.TryReply("ro", "De ce mi se potrivește meseria de șofer?", Profile, "MBO", false);
        Assert.Contains("66%", romanian, StringComparison.Ordinal);
        Assert.DoesNotContain("1.", romanian, StringComparison.Ordinal);

        var arabic = CandidateJobAdvice.TryReply("ar", "لماذا تناسبني مهنة السائق؟", Profile, "MBO", false);
        Assert.Contains("66%", arabic, StringComparison.Ordinal);
        Assert.DoesNotContain("1.", arabic, StringComparison.Ordinal);

        var step = CandidateJobAdvice.TryReply("nl", "Wat is mijn eerste stap in mijn actieplan?", Profile, "MBO", false);
        Assert.Contains("Kies ", step, StringComparison.Ordinal);
        Assert.Contains("Praat met iemand die dit werk doet", step, StringComparison.Ordinal);
        Assert.DoesNotContain("opleiding of cursus", step, StringComparison.OrdinalIgnoreCase);

        var likes = CandidateJobAdvice.TryReply("nl", "Wat vind ik leuk (om te doen)?", Profile, "MBO", false);
        Assert.Contains("Dat weet ik niet", likes, StringComparison.Ordinal);
        Assert.Contains("66%", likes, StringComparison.Ordinal);
        Assert.DoesNotContain("sollicitaties komen later", likes, StringComparison.OrdinalIgnoreCase);

        var certificates = CandidateJobAdvice.TryReply(
            "nl", "Heb ik een VCA-certificaat?", Profile, "MBO", false, certificates: []);
        Assert.Equal("Nee, er staan geen certificaten in je profiel.", certificates);

        var lead = CandidateJobAdvice.TryReply("nl", "Past teamleider logistiek bij mij?", Profile, "MBO", false);
        Assert.Contains("43%", lead, StringComparison.Ordinal);
        Assert.Contains("drie hoogste", lead, StringComparison.Ordinal);

        var direction = CandidateJobAdvice.TryReply("nl", "Welke beroepen passen bij mensen helpen?", Profile, "MBO", false);
        Assert.Contains("Helpende zorg", direction, StringComparison.Ordinal);
        Assert.DoesNotContain("Chauffeur", direction, StringComparison.Ordinal);

        var motivation = CandidateJobAdvice.TryReply(
            "nl", "Schrijf een korte motivatie voor de functie kok", Profile, "MBO", false);
        Assert.Contains("Aanpakken", motivation, StringComparison.Ordinal);
        Assert.Contains("66%", motivation, StringComparison.Ordinal);
        Assert.DoesNotContain("Mensen helpen", motivation, StringComparison.Ordinal);
    }

    [Fact]
    public void Unchanged_profile_views_do_not_generate_a_story()
    {
        Assert.False(CandidateInsightsFingerprint.ShouldGenerateWhoAmI(
            fingerprintMatches: true, fromOpenAi: false, storyOk: false, lastAttemptUtc: null, DateTime.UtcNow));
        Assert.False(CandidateInsightsFingerprint.ShouldGenerateWhoAmI(
            fingerprintMatches: true, fromOpenAi: false, storyOk: false, lastAttemptUtc: DateTime.UtcNow.AddHours(-1), DateTime.UtcNow));
        Assert.True(CandidateInsightsFingerprint.ShouldGenerateWhoAmI(
            fingerprintMatches: false, fromOpenAi: false, storyOk: false, lastAttemptUtc: null, DateTime.UtcNow));
    }

    [Fact]
    public async Task Ten_views_of_an_unchanged_profile_enqueue_nothing()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "run13@test.nl",
            FullName = "Run Dertien",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = """{"maxTravelMinutes":30,"preferredTransport":"Fiets","aboutMe":"Ik werk in het magazijn."}"""
        });
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            SamenwerkenPercent = 70,
            ResultaatgerichtheidPercent = 45,
            StressbestendigheidPercent = 40,
            InnovatiePercent = 30,
            ExtraversiePercent = 60
        });
        db.CandidateCareerInterests.Add(new CandidateCareerInterest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            RealisticPercent = 66,
            InvestigativePercent = 38,
            ArtisticPercent = 37,
            SocialPercent = 64,
            EnterprisingPercent = 43,
            ConventionalPercent = 62
        });
        db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AutonomyPercent = 40,
            InformalPercent = 55,
            CollaborationPercent = 48,
            FlexibilityPercent = 42,
            InnovationPercent = 30,
            PeopleFirstPercent = 44,
            OpennessPercent = 40,
            ConscientiousnessPercent = 72,
            ExtraversionPercent = 48,
            AgreeablenessPercent = 46,
            EmotionalStabilityPercent = 40
        });
        await db.SaveChangesAsync();

        var queue = new CountingQueue();
        var sut = new WhoAmIService(db, queue);
        for (var i = 0; i < 10; i++)
        {
            var state = await sut.GetAsync(userId);
            Assert.False(string.IsNullOrWhiteSpace(state.Story));
        }

        Assert.Equal(0, queue.Enqueued);
    }

    [Fact]
    public void Assistant_rate_limit_is_per_user_and_the_wait_copy_is_translated()
    {
        var secret = "unit-test-internal-client-ip-secret";
        var first = UserContext(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var second = UserContext(Guid.Parse("bbbbbbbb-bbbb-cccc-dddd-eeeeeeeeeeee"));
        Assert.Equal("uid:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", RateLimitPartitioning.ResolvePartitionKey(first, secret));
        Assert.NotEqual(
            RateLimitPartitioning.ResolvePartitionKey(first, secret),
            RateLimitPartitioning.ResolvePartitionKey(second, secret));
        Assert.DoesNotContain("ip:", RateLimitPartitioning.ResolvePartitionKey(first, secret), StringComparison.Ordinal);

        Assert.Equal(
            "Je stelt veel vragen achter elkaar. Wacht even en probeer het dan opnieuw.",
            UiStrings.Get("Assistant.RateLimited", "nl"));
        Assert.Contains("Wait a moment", UiStrings.Get("Assistant.RateLimited", "en"), StringComparison.Ordinal);
        Assert.Contains("Odczekaj", UiStrings.Get("Assistant.RateLimited", "pl"), StringComparison.Ordinal);
        Assert.Contains("Așteaptă", UiStrings.Get("Assistant.RateLimited", "ro"), StringComparison.Ordinal);
        Assert.Contains("انتظر", UiStrings.Get("Assistant.RateLimited", "ar"), StringComparison.Ordinal);
        Assert.Equal("Uit je paspoort: wat bij je past.", UiStrings.Get("Career.Empty.SuggestLead", "nl"));
        Assert.Contains("Samen & aardig", AssessmentOutcomeLines.Competence(70, 45, 40, 30, 60), StringComparison.Ordinal);
        Assert.Contains("nooit de woorden graag, leuk of fijn", CareerCompassPrompt.System, StringComparison.Ordinal);
    }

    private static DefaultHttpContext UserContext(Guid userId)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", userId.ToString("D")), new Claim(JobsyAccessToken.ClientIpClaim, "203.0.113.10")],
            "JobsyJwt"));
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        return http;
    }

    private sealed class CountingQueue : ICandidateInsightsQueue
    {
        public int Enqueued { get; private set; }

        public bool TryEnqueue(Guid userId)
        {
            Enqueued++;
            return true;
        }

        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(Guid.Empty);

        public void MarkCompleted(Guid userId)
        {
        }
    }
}
