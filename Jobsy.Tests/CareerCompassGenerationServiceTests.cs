using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.OpenAi;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class CareerCompassGenerationServiceTests
{
    [Fact]
    public async Task Without_api_key_uses_local_general_catalog()
    {
        var sut = CreateSut(new RecordingHandler(), apiKey: null);
        var result = await sut.GenerateFromCareerDeepAsync(PeakAll());
        Assert.False(result.FromOpenAi);
        Assert.True(result.FromDeepAnalysis);
        Assert.True(result.HasOccupations);
        Assert.InRange(result.AllOccupations.Count(), CareerCompassSanitize.MinCatalogueJobs, CareerCompassSanitize.MaxCatalogueJobs);
        Assert.All(result.AllOccupations, m =>
        {
            Assert.False(m.NoScore);
            Assert.True(CandidateFactGuard.IsCatalogueTitle(m.Title));
        });
    }

    [Fact]
    public async Task OpenAi_success_sanitizes_json_and_sets_from_openai()
    {
        var allowed = AllowedReply();
        var inner = JsonSerializer.Serialize(new
        {
            strengths = new[] { "Mensen helpen", "Aanpakken", "Ordenen" },
            superMatches = new[]
            {
                new { title = allowed.Titles[0], percent = allowed.Percents[0], why = allowed.Why, keys = new[] { "klus" } }
            },
            strongChoices = new[]
            {
                new { title = allowed.Titles[1], percent = allowed.Percents[1], why = allowed.Why, keys = new[] { "taak" } }
            },
            broadening = new[]
            {
                new { title = allowed.Titles[2], percent = allowed.Percents[2], why = allowed.Why, keys = new[] { "werk" } }
            },
            practicalNotes = new[]
            {
                "Open de banenkaart. Vacatures in Den Haag of het Westland die op zorg lijken scoren hoger."
            }
        });
        var handler = new RecordingHandler { ResponseJson = WrapChat(inner) };
        var sut = CreateSut(handler, apiKey: "sk-test");
        var result = await sut.GenerateFromCareerDeepAsync(PeakAll());

        Assert.True(result.FromOpenAi);
        Assert.True(result.FromDeepAnalysis);
        Assert.Contains(result.AllOccupations, m => m.Title == allowed.Titles[0]);
        Assert.Contains(result.AllOccupations, m => m.Why.Contains(allowed.Direction, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.PracticalNotes, n => n.Contains("banenkaart", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.PracticalNotes, n => n.Contains("Den Haag", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.PracticalNotes, n => n.Contains("Westland", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("json_object", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("chat/completions", handler.LastRequestUri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", handler.LastBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Ada", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("200 unieke vragen", handler.LastBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mistral_provider_posts_json_chat_to_mistral_and_ignores_the_openai_key()
    {
        var handler = new RecordingHandler { ResponseJson = WrapChat("{}") };
        var sut = new CareerCompassGenerationService(
            new NamedHttpClientFactory(handler),
            new OpenAiEndpointResolver(
                new StubCredentials("sk-openai-db"),
                Options.Create(new OpenAiOptions
                {
                    ApiKey = "sk-openai-config",
                    Model = "gpt-4o-mini",
                    BaseUrl = "https://api.openai.com/v1/"
                }),
                Options.Create(new AiOptions { Provider = "Mistral" }),
                Options.Create(new MistralOptions { ApiKey = "mistral-test-key" })),
            NullLogger<CareerCompassGenerationService>.Instance);

        await sut.GenerateFromCareerDeepAsync(PeakAll());

        Assert.Contains(
            "https://api.eu.mistral.ai/v1/chat/completions",
            handler.LastRequestUri,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mistral-small-latest", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("json_object", handler.LastBody, StringComparison.Ordinal);
        Assert.DoesNotContain("gpt-4o-mini", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("Bearer mistral-test-key", handler.LastAuthorization, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-openai", handler.LastAuthorization, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invented_titles_are_rejected_once_then_a_catalogue_reply_is_kept()
    {
        var invented = WrapChat("""
            {
              "strengths": ["Aanpakken", "Helpen", "Ordenen"],
              "superMatches": [
                {"title":"Onbekend beroep xyz","percent":99,"why":"Ik heb jarenlang in de bouw gewerkt.","keys":[]}
              ],
              "strongChoices": [],
              "broadening": [],
              "practicalNotes": ["Kijk welke taken bij je passen."]
            }
            """);
        var allowed = AllowedReply();
        var clean = WrapChat(JsonSerializer.Serialize(new
        {
            strengths = new[] { "Mensen helpen", "Aanpakken", "Ordenen" },
            superMatches = new[]
            {
                new { title = allowed.Titles[0], percent = allowed.Percents[0], why = allowed.Why, keys = new[] { "klus" } }
            },
            strongChoices = new[]
            {
                new { title = allowed.Titles[1], percent = allowed.Percents[1], why = allowed.Why, keys = new[] { "taak" } }
            },
            broadening = new[]
            {
                new { title = allowed.Titles[2], percent = allowed.Percents[2], why = allowed.Why, keys = new[] { "werk" } }
            },
            practicalNotes = new[] { "Open de banenkaart." }
        }));
        var handler = new RecordingHandler { Responses = [invented, clean] };
        var log = new CapturingLog();
        var sut = CreateSut(handler, apiKey: "sk-test", log);
        var result = await sut.GenerateFromCareerDeepAsync(PeakAll());

        Assert.Equal(2, handler.Calls);
        Assert.Contains("STRIKT", handler.LastBody, StringComparison.Ordinal);
        Assert.True(result.FromOpenAi);
        Assert.Contains(result.AllOccupations, m => m.Title == allowed.Titles[0]);
        Assert.DoesNotContain(result.AllOccupations, m => !CandidateFactGuard.IsCatalogueTitle(m.Title));
        Assert.DoesNotContain(result.AllOccupations, m => m.Why.Contains("jarenlang", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(log.Messages, m => m.Contains("surface=compass", StringComparison.Ordinal) && m.Contains("reason=unknown-job", StringComparison.Ordinal));
        Assert.DoesNotContain(log.Messages, m => m.Contains("jarenlang", StringComparison.OrdinalIgnoreCase) || m.Contains('@'));
    }

    [Fact]
    public async Task Two_invented_compass_replies_fall_back_to_the_local_catalogue()
    {
        var invented = WrapChat("""
            {
              "strengths": ["Aanpakken"],
              "superMatches": [
                {"title":"Bouwplaats alleskunner","percent":91,"why":"Ik heb in de zorg gestaan.","keys":[]}
              ],
              "strongChoices": [],
              "broadening": [],
              "practicalNotes": ["Ik heb jarenlang in de bouw gewerkt."]
            }
            """);
        var handler = new RecordingHandler { Responses = [invented, invented] };
        var sut = CreateSut(handler, apiKey: "sk-test", new CapturingLog());
        var result = await sut.GenerateFromCareerDeepAsync(PeakAll());

        Assert.Equal(2, handler.Calls);
        Assert.False(result.FromOpenAi);
        Assert.NotEmpty(result.AllOccupations);
        Assert.DoesNotContain(result.AllOccupations, m => !CandidateFactGuard.IsCatalogueTitle(m.Title));
        Assert.DoesNotContain(result.AllOccupations, m => m.Why.Contains("jarenlang", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.PracticalNotes, n => n.Contains("jarenlang", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Http_error_falls_back_to_local_catalog()
    {
        var handler = new RecordingHandler { Status = HttpStatusCode.InternalServerError };
        var sut = CreateSut(handler, apiKey: "sk-test");
        var result = await sut.GenerateFromCareerDeepAsync(PeakAll());
        Assert.False(result.FromOpenAi);
        Assert.True(result.FromDeepAnalysis);
        Assert.True(result.HasOccupations);
    }

    private static Dictionary<int, int> PeakAll()
        => DeepAnalysisCatalog.CareerQuestions.ToDictionary(q => q.Id, q => q.Reverse ? 1 : 5);

    private static (IReadOnlyList<string> Titles, IReadOnlyList<int> Percents, string Direction, string Why) AllowedReply()
    {
        var answers = PeakAll();
        var scores = DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career);
        var sheet = CandidateFactSheet.ForCareer(scores, answers);
        var titles = sheet.AllowedJobTitles.Take(3).ToList();
        var percents = titles.Select(title =>
        {
            var line = sheet.Scores.First(item =>
                item.Contains(title, StringComparison.OrdinalIgnoreCase) && item.Contains('%'));
            var start = line.LastIndexOf(": ", StringComparison.Ordinal) + 2;
            var end = line.IndexOf('%', start);
            return int.Parse(line[start..end], System.Globalization.CultureInfo.InvariantCulture);
        }).ToList();
        var direction = sheet.DirectionLabels[0];
        return (titles, percents, direction, $"Dit sluit aan bij {direction}.");
    }

    private static string WrapChat(string content)
        => JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content } }
            }
        });

    private static CareerCompassGenerationService CreateSut(
        HttpMessageHandler handler,
        string? apiKey,
        CapturingLog? log = null)
        => new(
            new NamedHttpClientFactory(handler),
            new OpenAiEndpointResolver(
                new StubCredentials(apiKey),
                Options.Create(new OpenAiOptions { ApiKey = apiKey, Model = "gpt-4o-mini" })),
            NullLogger<CareerCompassGenerationService>.Instance,
            log);

    private sealed class CapturingLog : Jobsy.Core.Diagnostics.IPlatformErrorLog
    {
        public List<string> Messages { get; } = [];

        public Task WriteAsync(
            string category,
            string message,
            string? supportCode,
            string? detail,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(category + " " + message);
            return Task.CompletedTask;
        }
    }

    private sealed class NamedHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public string ResponseJson { get; init; } = """{"choices":[{"message":{"content":"{}"}}]}""";
        public IReadOnlyList<string>? Responses { get; init; }
        public string LastBody { get; private set; } = "";
        public string LastRequestUri { get; private set; } = "";
        public string LastAuthorization { get; private set; } = "";
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString() ?? "";
            LastAuthorization = request.Headers.Authorization?.ToString() ?? "";
            LastBody = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var json = Responses is { Count: > 0 }
                ? Responses[Math.Min(Calls, Responses.Count - 1)]
                : ResponseJson;
            Calls++;
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StubCredentials(string? apiKey) : IIntegrationCredentialService
    {
        public Task<IntegrationCredentialView?> GetAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialView?>(null);

        public Task<IReadOnlyList<IntegrationCredentialView>> GetConfigurableAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IntegrationCredentialView>>([]);

        public Task<IntegrationCredentialView> UpsertAsync(
            IntegrationKey key,
            IntegrationCredentialUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SavePingResultAsync(
            IntegrationKey key,
            bool ok,
            string message,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string?> GetRawApiKeyAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(apiKey);

        public Task<string?> GetModelAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<IntegrationCredentialSecrets?> GetSecretsAsync(
            IntegrationKey key,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialSecrets?>(null);
    }
}
