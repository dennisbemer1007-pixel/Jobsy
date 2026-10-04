using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Legal;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.OpenAi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class AiProviderSelectionTests
{
    [Fact]
    public void Unknown_provider_stays_on_openai()
    {
        Assert.Equal(AiProvider.OpenAI, AiProviderParser.Parse(null));
        Assert.Equal(AiProvider.OpenAI, AiProviderParser.Parse(" "));
        Assert.Equal(AiProvider.OpenAI, AiProviderParser.Parse("Groq"));
        Assert.False(new AiOptions { Provider = "Groq" }.IsKnownProvider);
        Assert.Equal(AiProvider.Mistral, AiProviderParser.Parse("mistral"));
    }

    [Fact]
    public void Catalog_lists_only_the_active_ai_company()
    {
        var openAi = LegalProcessors.ForAiProvider(AiProvider.OpenAI);
        var mistral = LegalProcessors.ForAiProvider(AiProvider.Mistral);

        Assert.Contains(openAi, row => row.Id == "openai");
        Assert.DoesNotContain(openAi, row => row.Id == "mistral");
        Assert.Contains(mistral, row => row.Id == "mistral");
        Assert.DoesNotContain(mistral, row => row.Id == "openai");
        Assert.Equal(LegalProcessors.InsideEu, LegalProcessors.ById("mistral").TransferBasisKey);
        Assert.Contains("Parijs", LegalProcessors.ById("mistral").Region, StringComparison.Ordinal);
        Assert.Equal(LegalProcessors.All.Count, openAi.Count + 1);
    }

    [Fact]
    public void Admin_status_names_the_model_that_will_be_called()
    {
        var openAi = AiAdminStatus.Describe(
            new AiOptions(),
            new MistralOptions(),
            "gpt-4o-mini");
        Assert.Equal(("OpenAI", "gpt-4o-mini"), openAi);

        var mistral = AiAdminStatus.Describe(
            new AiOptions { Provider = "Mistral" },
            new MistralOptions(),
            "gpt-4o-mini");
        Assert.Equal(("Mistral", MistralOptions.DefaultModel), mistral);
    }

    [Fact]
    public async Task Resolver_keeps_openai_db_key_when_provider_is_default()
    {
        var sut = new OpenAiEndpointResolver(
            new StubCredentials(apiKey: "sk-db", model: "gpt-db"),
            Options.Create(new OpenAiOptions { ApiKey = "sk-config", Model = "gpt-config" }));

        var resolved = await sut.ResolveAsync(OpenAiFeature.WhoAmI);

        Assert.Equal("sk-db", resolved.ApiKey);
        Assert.Equal("gpt-db", resolved.Model);
        Assert.Equal(OpenAiOptions.DefaultBaseUrl, resolved.BaseUrl);
    }

    [Fact]
    public async Task Resolver_uses_mistral_and_ignores_the_openai_key_and_model()
    {
        var credentials = new StubCredentials(apiKey: "sk-openai-db", model: "gpt-4o-mini", baseUrl: "https://api.openai.com/v1/");
        var sut = new OpenAiEndpointResolver(
            credentials,
            Options.Create(new OpenAiOptions { ApiKey = "sk-openai-config", Model = "gpt-4o-mini" }),
            Options.Create(new AiOptions { Provider = "Mistral" }),
            Options.Create(new MistralOptions { ApiKey = " mistral-test-key " }));

        var resolved = await sut.ResolveAsync(OpenAiFeature.CvExtraction);

        Assert.Equal("mistral-test-key", resolved.ApiKey);
        Assert.Equal(MistralOptions.DefaultModel, resolved.Model);
        Assert.Equal(MistralOptions.DefaultBaseUrl, resolved.BaseUrl);
        Assert.Equal(0, credentials.Reads);
    }

    [Fact]
    public async Task Resolver_without_mistral_key_does_not_fall_back_to_openai()
    {
        var sut = new OpenAiEndpointResolver(
            new StubCredentials(apiKey: "sk-openai-db"),
            Options.Create(new OpenAiOptions { ApiKey = "sk-openai-config" }),
            Options.Create(new AiOptions { Provider = "Mistral" }),
            Options.Create(new MistralOptions { ApiKey = "  " }),
            NullLogger<OpenAiEndpointResolver>.Instance);

        var resolved = await sut.ResolveAsync(OpenAiFeature.Translation);

        Assert.Null(resolved.ApiKey);
        Assert.Equal(MistralOptions.DefaultModel, resolved.Model);
        Assert.Equal(MistralOptions.DefaultBaseUrl, resolved.BaseUrl);
    }

    [Fact]
    public async Task Unknown_provider_still_uses_the_openai_endpoint()
    {
        var sut = new OpenAiEndpointResolver(
            new StubCredentials(apiKey: "sk-db"),
            Options.Create(new OpenAiOptions { ApiKey = "sk-config" }),
            Options.Create(new AiOptions { Provider = "Groq" }),
            Options.Create(new MistralOptions { ApiKey = "mistral-key" }),
            NullLogger<OpenAiEndpointResolver>.Instance);

        var resolved = await sut.ResolveAsync(OpenAiFeature.AssistantChat);

        Assert.Equal("sk-db", resolved.ApiKey);
        Assert.Equal(OpenAiOptions.DefaultBaseUrl, resolved.BaseUrl);
    }

    [Fact]
    public async Task Mistral_chat_request_uses_json_object_on_the_eu_host()
    {
        var handler = new RecordingHandler();
        var resolver = new OpenAiEndpointResolver(
            new StubCredentials(apiKey: "sk-openai-db", model: "gpt-4o-mini"),
            Options.Create(new OpenAiOptions { ApiKey = "sk-openai-config", Model = "gpt-4o-mini" }),
            Options.Create(new AiOptions { Provider = "Mistral" }),
            Options.Create(new MistralOptions { ApiKey = "mistral-test-key" }));
        var sut = new CareerCompassGenerationService(
            new NamedHttpClientFactory(handler),
            resolver,
            NullLogger<CareerCompassGenerationService>.Instance);

        await sut.GenerateFromCareerDeepAsync(PeakAll());

        Assert.Equal("https://api.mistral.ai/v1/chat/completions", handler.LastRequestUri);
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal("mistral-test-key", handler.Authorization?.Parameter);
        Assert.Contains("\"model\":\"mistral-small-latest\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("json_object", handler.LastBody, StringComparison.Ordinal);
        Assert.DoesNotContain("gpt-4o-mini", handler.LastBody, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-openai", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("JSON", CareerCompassPrompt.System, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_mode_prompts_say_json_so_mistral_does_not_stream_whitespace()
    {
        Assert.Contains("JSON", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("JSON", WhoAmIPrompt.System, StringComparison.Ordinal);
        Assert.Contains("JSON", CultureFitPrompt.System, StringComparison.Ordinal);
        Assert.Contains("JSON", RoleFitCheckPrompt.System, StringComparison.Ordinal);

        var root = RepoRoot();
        foreach (var relative in new[]
        {
            "Jobsy.Infrastructure/Services/VacancyContentModerationService.cs",
            "Jobsy.Infrastructure/Services/CvExtractionService.cs",
            "Jobsy.Infrastructure/Services/OpenAiCompetenceDeepReportAiService.cs",
            "Jobsy.Infrastructure/Services/CareerPathPlanGenerationService.cs"
        })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("json_object", text, StringComparison.Ordinal);
            Assert.Contains("JSON", text, StringComparison.Ordinal);
        }

        // Translation asks for JSON in the prompt and parses it. It does not set response_format.
        var translation = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/OpenAiTranslationService.cs"));
        Assert.Contains("JSON", translation, StringComparison.Ordinal);
        Assert.Contains("chat/completions", translation, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_aloud_stays_on_the_device_and_there_is_no_embeddings_or_audio_call()
    {
        var root = RepoRoot();
        var script = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/read-aloud.js"));
        Assert.Contains("speechSynthesis", script, StringComparison.Ordinal);
        Assert.Contains("never leaves the device", script, StringComparison.Ordinal);

        foreach (var project in new[] { "Jobsy.Core", "Jobsy.Infrastructure", "Jobsy.Api", "Jobsy.Web" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, project), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.DoesNotContain("/embeddings", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("audio/speech", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("/moderations", text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task Admin_dto_reports_mistral_while_the_stored_model_stays_openai()
    {
        var dbOptions = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(dbOptions);
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        await credentials.UpsertAsync(
            IntegrationKey.OpenAI,
            new IntegrationCredentialUpdate(ApiKey: "sk-openai-db", Model: "gpt-4o-mini"));
        var config = new ConfigurationBuilder().Build();
        var features = new PlatformFeatureService(db, Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()), config);
        var company = new PlatformCompanySettingsService(db);
        var flyer = new MarketingFlyerSettingsService(db);
        var controller = new SettingsController(
            db,
            credentials,
            features,
            company,
            flyer,
            new MarketingFlyerPdfService(flyer, company, features),
            new FlexCommercialService(db),
            new NoOpAdminAuditLog(),
            new NoOpAdminAuditContext(),
            new FakeUserLookup(),
            Options.Create(new AiOptions { Provider = "Mistral" }),
            Options.Create(new MistralOptions()));

        var result = await controller.GetIntegrationCredential(IntegrationKey.OpenAI, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<IntegrationCredentialDto>(ok.Value);

        Assert.Equal("Mistral", dto.ActiveAiProvider);
        Assert.Equal(MistralOptions.DefaultModel, dto.ActiveAiModel);
        Assert.Equal("gpt-4o-mini", dto.Model);
    }

    private static Dictionary<int, int> PeakAll()
        => DeepAnalysisCatalog.CareerQuestions.ToDictionary(q => q.Id, q => q.Reverse ? 1 : 5);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
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
        public string LastBody { get; private set; } = "";
        public string LastRequestUri { get; private set; } = "";
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString() ?? "";
            Authorization = request.Headers.Authorization;
            LastBody = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StubCredentials(string? apiKey, string? model = null, string? baseUrl = null) : IIntegrationCredentialService
    {
        public int Reads { get; private set; }

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
        {
            Reads++;
            return Task.FromResult(apiKey);
        }

        public Task<string?> GetModelAsync(IntegrationKey key, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(model);
        }

        public Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(baseUrl);
        }

        public Task<IntegrationCredentialSecrets?> GetSecretsAsync(
            IntegrationKey key,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialSecrets?>(null);
    }
}
