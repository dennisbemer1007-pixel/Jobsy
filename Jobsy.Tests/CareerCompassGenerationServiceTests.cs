using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
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
        Assert.Contains(result.AllOccupations, m => m.Title.Contains("kas", StringComparison.OrdinalIgnoreCase)
                                                 || m.Title.Contains("bouw", StringComparison.OrdinalIgnoreCase)
                                                 || m.Title.Contains("zorg", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OpenAi_success_sanitizes_json_and_sets_from_openai()
    {
        var inner = """
            {
              "strengths": ["Mensen helpen"],
              "superMatches": [
                {"title":"Verpleegkundige","percent":98,"why":"Jij wilt voor mensen klaarstaan.","keys":["zorg","verpleeg"]}
              ],
              "strongChoices": [
                {"title":"Docent","percent":88,"why":"Uitleggen past bij je.","keys":["les","onderwijs"]}
              ],
              "broadening": [
                {"title":"HR-medewerker","percent":78,"why":"Mensen en administratie.","keys":["personeel","hr"]}
              ],
              "practicalNotes": ["Open de banenkaart. Vacatures in Den Haag of het Westland die op zorg lijken scoren hoger."]
            }
            """;
        var handler = new RecordingHandler { ResponseJson = WrapChat(inner) };
        var sut = CreateSut(handler, apiKey: "sk-test");
        var result = await sut.GenerateFromCareerDeepAsync(PeakAll());

        Assert.True(result.FromOpenAi);
        Assert.True(result.FromDeepAnalysis);
        Assert.Contains(result.SuperMatches, m => m.Title == "Verpleegkundige");
        Assert.Contains(result.PracticalNotes, n => n.Contains("banenkaart", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("json_object", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("chat/completions", handler.LastRequestUri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", handler.LastBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Ada", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("200 unieke vragen", handler.LastBody, StringComparison.Ordinal);
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

    private static string WrapChat(string content)
        => JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content } }
            }
        });

    private static CareerCompassGenerationService CreateSut(HttpMessageHandler handler, string? apiKey)
        => new(
            new NamedHttpClientFactory(handler),
            new StubCredentials(apiKey),
            Options.Create(new OpenAiOptions { ApiKey = apiKey, Model = "gpt-4o-mini" }),
            NullLogger<CareerCompassGenerationService>.Instance);

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
        public string LastBody { get; private set; } = "";
        public string LastRequestUri { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString() ?? "";
            LastBody = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json")
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
