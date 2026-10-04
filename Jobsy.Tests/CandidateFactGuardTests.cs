using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.OpenAi;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class CandidateFactGuardTests
{
    private const string Run10Mistral =
        "Ik heb jarenlang in de bouw gewerkt. Ook heb ik in de zorg gestaan.";

    private const string Run10OpenAi =
        "Ik heb ervaring in de zorg en heb jarenlang in de bouw gewerkt. Later heb ik in de horeca gestaan.";

    [Fact]
    public void Empty_experience_rejects_recorded_mistral_and_openai_stories()
    {
        var sheet = CandidateFactSheet.Personal([], [], []);
        Assert.Equal("invented-work", CandidateFactGuard.RejectionReason(Run10Mistral, sheet));
        Assert.Equal("invented-work", CandidateFactGuard.RejectionReason(Run10OpenAi, sheet));
        Assert.Equal("invented-work", CandidateFactGuard.RejectionReason("Ik heb gewerkt in de techniek.", sheet));
        Assert.Equal("invented-work", CandidateFactGuard.RejectionReason("Ik heb ervaring in de logistiek.", sheet));
        Assert.Equal("markdown", CandidateFactGuard.RejectionReason("**Ik ben sterk.**\n\nEn ik pak aan.", sheet));
        Assert.Equal("unknown-diploma", CandidateFactGuard.RejectionReason("Ik heb een mbo-diploma.", sheet));
        Assert.Equal("unknown-year", CandidateFactGuard.RejectionReason("In 2019 begon ik met werk.", sheet));
        Assert.Null(CandidateFactGuard.RejectionReason(
            "Ik heb geen werkervaring. Ik weet het niet.",
            sheet));
    }

    [Fact]
    public void Known_role_and_year_stay_unknown_sectors_and_jarenlang_do_not()
    {
        var sheet = CandidateFactSheet.Personal(["monteur (3 jaar)"], ["MBO Zorg"], ["VCA (2020)"]);
        Assert.Null(CandidateFactGuard.RejectionReason("Ik heb als monteur gewerkt. Dat was 3 jaar.", sheet));
        Assert.Null(CandidateFactGuard.RejectionReason("Mijn opleiding is MBO Zorg. Mijn certificaat is VCA uit 2020.", sheet));
        Assert.Equal("invented-work", CandidateFactGuard.RejectionReason("Ik heb jarenlang in de bouw gewerkt.", sheet));
        Assert.Equal("invented-work", CandidateFactGuard.RejectionReason("Ik heb in de horeca gestaan.", sheet));
    }

    [Fact]
    public void Work_entry_keeps_role_and_years_and_drops_the_employer_name()
    {
        var entry = new CandidateEmployerHistoryDto("Shell", "monteur", Years: 4, StartMonth: "2019-03", EndMonth: "2023-01");
        var fact = CandidateFactSheet.FormatWorkEntry(entry);
        Assert.Equal("monteur (4 jaar) 2019-2023", fact);
        Assert.DoesNotContain("Shell", fact, StringComparison.OrdinalIgnoreCase);

        var highlights = WhoAmIProfileHighlights.FromPreferences(new CandidatePreferencesDto(
            [],
            30,
            "Fiets",
            Employers: [entry]));
        var prompt = WhoAmIPrompt.User(
            new CompetencyScores(80, 70, 60, 50),
            new RiasecScores(66, 38, 37, 64, 43, 62),
            EmptyCulture(),
            highlights);
        Assert.Contains("werkervaring: monteur (4 jaar) 2019-2023", prompt, StringComparison.Ordinal);
        Assert.Contains("Feitenlijst", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Shell", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Local_story_for_an_empty_profile_does_not_invent_past_work()
    {
        var competency = new CompetencyScores(80, 70, 65, 90);
        var career = new RiasecScores(66, 38, 37, 64, 43, 62);
        var culture = EmptyCulture();
        var story = WhoAmIStoryBuilder.Build(competency, career, culture, WhoAmIProfileHighlights.Empty);
        var sheet = CandidateFactSheet.ForWhoAmI(competency, career, culture, WhoAmIProfileHighlights.Empty, null);
        Assert.Null(CandidateFactGuard.RejectionReason(story, sheet));
        Assert.DoesNotContain("jarenlang", story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gewerkt", story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("in de bouw", story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("in de zorg", story, StringComparison.OrdinalIgnoreCase);
        Assert.False(WhoAmIStoryBuilder.Accepts(Run10Mistral + "\n\n" + Run10OpenAi, WhoAmIProfileHighlights.Empty, competency, culture));
    }

    [Fact]
    public void Compass_prose_and_job_list_stay_inside_the_catalogue()
    {
        var answers = DeepAnalysisCatalog.CareerQuestions.ToDictionary(q => q.Id, _ => 4);
        var scores = DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career);
        var sheet = CandidateFactSheet.ForCareer(scores, answers);
        var allowed = sheet.AllowedJobTitles[0];
        var inventedWhy = """
            {
              "strengths": ["Aanpakken", "Helpen", "Ordenen"],
              "superMatches": [
                {"title":"__TITLE__","percent":90,"why":"Ik heb jarenlang in de bouw gewerkt.","keys":["klus"]}
              ],
              "strongChoices": [],
              "broadening": [],
              "practicalNotes": ["Kijk welke taken bij je passen."]
            }
            """.Replace("__TITLE__", allowed, StringComparison.Ordinal);
        Assert.Equal("invented-work", CandidateFactGuard.CompassRejection(inventedWhy, sheet));

        var recordedMistral = """
            {
              "strengths": ["Aanpakken", "Helpen", "Ordenen"],
              "superMatches": [
                {"title":"Hovenier of Kassenmedewerker","percent":92,"why":"Dit past bij aanpakken.","keys":["hovenier"]}
              ],
              "strongChoices": [
                {"title":"Onbekend beroep xyz","percent":99,"why":"Verzonnen titel.","keys":[]}
              ],
              "broadening": [],
              "practicalNotes": ["Kijk welke taken bij je passen."]
            }
            """;
        Assert.Equal("unknown-job", CandidateFactGuard.CompassRejection(recordedMistral, sheet));

        var stored = CareerCompassJson.TryDeserialize("""
            {
              "strengths": ["Aanpakken", "Helpen", "Ordenen"],
              "superMatches": [
                {"title":"Elektrotechnicus","percent":89,"why":"Ik heb jarenlang in de bouw gewerkt.","keys":["elektro"]}
              ],
              "strongChoices": [
                {"title":"Allround technisch talent","percent":91,"why":"Verzonnen titel.","keys":[]}
              ],
              "broadening": [],
              "practicalNotes": ["Ik heb in de zorg gestaan."]
            }
            """);
        Assert.NotNull(stored);
        Assert.All(stored!.AllOccupations, job => Assert.True(CandidateFactGuard.IsCatalogueTitle(job.Title)));
        Assert.DoesNotContain(stored.AllOccupations, job => job.Why.Contains("jarenlang", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(stored.PracticalNotes, note => note.Contains("gestaan", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(stored.AllOccupations, job => job.Title.Contains("alleskunner", StringComparison.OrdinalIgnoreCase)
            || job.Title.Contains("Allround", StringComparison.OrdinalIgnoreCase));

        var report = CareerDeepReportBuilder.Build(
            scores,
            stored,
            null,
            new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
        Assert.All(report.Occupations, job => Assert.True(CandidateFactGuard.IsCatalogueTitle(job.TitleNl)));
        Assert.DoesNotContain(report.Occupations, job => job.ReasonNl.Contains("jarenlang", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Empty_profile_story_never_keeps_an_invented_work_history()
    {
        var invented = WrapChat(JsonSerializer.Serialize(new
        {
            story = Run10Mistral + "\n\nOok heb ik in de zorg gestaan.",
            keywords = new[] { "bouw" }
        }));
        var clean = WrapChat(JsonSerializer.Serialize(new
        {
            story = "Ik pak taken aan en maak ze af. Samenwerken past bij mij.\n\nIk houd van een duidelijke dag. In Den Haag of het Westland voel ik me op mijn plek.",
            keywords = new[] { "aanpakken" }
        }));
        var handler = new RecordingHandler { Responses = [invented, clean] };
        var log = new CapturingLog();
        var sut = new WhoAmIGenerationService(
            new NamedHttpClientFactory(handler),
            Resolver("sk-test"),
            NullLogger<WhoAmIGenerationService>.Instance,
            log);

        var result = await sut.GenerateAsync(
            new CompetencyScores(80, 70, 65, 90),
            new RiasecScores(66, 38, 37, 64, 43, 62),
            EmptyCulture());

        Assert.Equal(2, handler.Calls);
        Assert.Contains("werkervaring: geen", handler.Bodies[0], StringComparison.Ordinal);
        Assert.Contains("STRIKT", handler.Bodies[1], StringComparison.Ordinal);
        Assert.True(result.FromOpenAi);
        Assert.DoesNotContain("jarenlang", result.Story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bouw", result.Story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("zorg", result.Story, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(log.Messages, m => m.Contains("surface=whoami", StringComparison.Ordinal) && m.Contains("reason=invented-work", StringComparison.Ordinal));
        Assert.DoesNotContain(log.Messages, m => m.Contains("jarenlang", StringComparison.OrdinalIgnoreCase) || m.Contains('@'));

        var bothBad = new RecordingHandler { Responses = [invented, invented] };
        var fallback = new WhoAmIGenerationService(
            new NamedHttpClientFactory(bothBad),
            Resolver("sk-test"),
            NullLogger<WhoAmIGenerationService>.Instance,
            new CapturingLog());
        var local = await fallback.GenerateAsync(
            new CompetencyScores(80, 70, 65, 90),
            new RiasecScores(66, 38, 37, 64, 43, 62),
            EmptyCulture());
        Assert.False(local.FromOpenAi);
        Assert.Equal("invented-work", CandidateFactGuard.RejectionReason(Run10OpenAi, CandidateFactSheet.Personal([], [], [])));
        Assert.Null(CandidateFactGuard.RejectionReason(local.Story, CandidateFactSheet.Personal([], [], [])));
        Assert.DoesNotContain("gewerkt", local.Story, StringComparison.OrdinalIgnoreCase);
    }

    private static CulturePersonalityScores EmptyCulture()
        => new(
            Autonomy: 40, Informal: 60, Collaboration: 80, Flexibility: 55, Innovation: 50, PeopleFirst: 65,
            Openness: 55, Conscientiousness: 70, Extraversion: 60, Agreeableness: 75, EmotionalStability: 70);

    private static string WrapChat(string content)
        => JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } });

    private static OpenAiEndpointResolver Resolver(string apiKey)
        => new(
            new StubCredentials(apiKey),
            Options.Create(new OpenAiOptions { ApiKey = apiKey, Model = "gpt-4o-mini" }));

    private sealed class CapturingLog : Jobsy.Core.Diagnostics.IPlatformErrorLog
    {
        public List<string> Messages { get; } = [];

        public Task WriteAsync(string category, string message, string? supportCode, string? detail, CancellationToken cancellationToken = default)
        {
            Messages.Add(category + " " + message);
            return Task.CompletedTask;
        }
    }

    private sealed class NamedHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public IReadOnlyList<string> Responses { get; init; } = [];
        public List<string> Bodies { get; } = [];
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            var json = Responses[Math.Min(Calls, Responses.Count - 1)];
            Calls++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StubCredentials(string? apiKey) : IIntegrationCredentialService
    {
        public Task<IntegrationCredentialView?> GetAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialView?>(null);

        public Task<IReadOnlyList<IntegrationCredentialView>> GetConfigurableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IntegrationCredentialView>>([]);

        public Task<IntegrationCredentialView> UpsertAsync(IntegrationKey key, IntegrationCredentialUpdate update, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SavePingResultAsync(IntegrationKey key, bool ok, string message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string?> GetRawApiKeyAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(apiKey);

        public Task<string?> GetModelAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<IntegrationCredentialSecrets?> GetSecretsAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialSecrets?>(null);
    }
}
