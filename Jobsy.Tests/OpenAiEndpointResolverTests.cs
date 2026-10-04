using Jobsy.Core.Ai;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services.OpenAi;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class OpenAiEndpointResolverTests
{
    [Fact]
    public async Task Db_api_key_wins_over_config()
    {
        var sut = CreateSut(
            dbApiKey: "sk-db",
            options: new OpenAiOptions { ApiKey = "sk-config" });

        var resolved = await sut.ResolveAsync(OpenAiFeature.WhoAmI);

        Assert.Equal("sk-db", resolved.ApiKey);
    }

    [Fact]
    public async Task Config_api_key_used_when_db_empty()
    {
        var sut = CreateSut(
            dbApiKey: null,
            options: new OpenAiOptions { ApiKey = " sk-config " });

        var resolved = await sut.ResolveAsync(OpenAiFeature.Translation);

        Assert.Equal("sk-config", resolved.ApiKey);
    }

    [Fact]
    public async Task Null_api_key_when_db_and_config_empty()
    {
        var sut = CreateSut(dbApiKey: "  ", options: new OpenAiOptions { ApiKey = null });

        var resolved = await sut.ResolveAsync(OpenAiFeature.CvExtraction);

        Assert.Null(resolved.ApiKey);
    }

    [Fact]
    public async Task Db_model_wins_over_config_and_default()
    {
        var sut = CreateSut(
            dbModel: "gpt-db",
            options: new OpenAiOptions { Model = "gpt-config" });

        var resolved = await sut.ResolveAsync(OpenAiFeature.CultureFit);

        Assert.Equal("gpt-db", resolved.Model);
    }

    [Fact]
    public async Task Config_model_wins_over_default()
    {
        var sut = CreateSut(
            dbModel: null,
            options: new OpenAiOptions { Model = " gpt-config " });

        var resolved = await sut.ResolveAsync(OpenAiFeature.MockInterview);

        Assert.Equal("gpt-config", resolved.Model);
    }

    [Fact]
    public async Task Db_base_url_wins_when_normalizable()
    {
        var sut = CreateSut(
            dbBaseUrl: "https://proxy.example.com/v1",
            options: new OpenAiOptions { BaseUrl = "https://api.openai.com/v1/" });

        var resolved = await sut.ResolveAsync(OpenAiFeature.AssistantChat);

        Assert.Equal("https://proxy.example.com/v1/", resolved.BaseUrl);
    }

    [Fact]
    public async Task Normalize_style_falls_back_to_default_when_config_base_url_invalid()
    {
        var sut = CreateSut(
            options: new OpenAiOptions { BaseUrl = "http://127.0.0.1:8080/v1" });

        var resolved = await sut.ResolveAsync(OpenAiFeature.WhoAmI);

        Assert.Equal(OpenAiEndpointResolver.DefaultBaseUrl, resolved.BaseUrl);
    }

    [Fact]
    public async Task Trim_slash_style_keeps_config_base_url_without_normalize()
    {
        var sut = CreateSut(
            options: new OpenAiOptions { BaseUrl = " https://custom.example/openai/v1 " });

        var resolved = await sut.ResolveAsync(OpenAiFeature.CareerPathPlan);

        Assert.Equal("https://custom.example/openai/v1/", resolved.BaseUrl);
    }

    [Theory]
    [InlineData(OpenAiFeature.WhoAmI, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.AssistantChat, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.CareerPathPlan, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.CultureFit, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.VacancyContentModeration, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.CvExtraction, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.MockInterview, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.Translation, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.CareerCompass, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.RoleFitCheck, "gpt-4o-mini")]
    [InlineData(OpenAiFeature.CompetenceDeepReport, "gpt-4o-mini")]
    public async Task Per_feature_default_model_matches_pre_dedup_literal(
        OpenAiFeature feature,
        string expectedDefaultModel)
    {
        // Empty model forces the feature default (OpenAiOptions.Model property default is
        // also gpt-4o-mini; clear it so we assert the resolver table, not Options defaults).
        var sut = CreateSut(options: new OpenAiOptions { Model = "   " });

        var resolved = await sut.ResolveAsync(feature);

        Assert.Equal(expectedDefaultModel, resolved.Model);
        Assert.Equal(
            expectedDefaultModel,
            OpenAiEndpointResolver.Features[feature].DefaultModel);
    }

    [Theory]
    [InlineData(OpenAiFeature.WhoAmI, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.AssistantChat, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.CareerPathPlan, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.TrimAndEnsureSlash)]
    [InlineData(OpenAiFeature.CultureFit, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.VacancyContentModeration, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.CvExtraction, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.MockInterview, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.Translation, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.CareerCompass, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.RoleFitCheck, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.NormalizeOrDefault)]
    [InlineData(OpenAiFeature.CompetenceDeepReport, OpenAiEndpointResolver.BaseUrlConfigFallbackStyle.TrimAndEnsureSlash)]
    public void Per_feature_base_url_fallback_style_matches_pre_dedup_copy(
        OpenAiFeature feature,
        OpenAiEndpointResolver.BaseUrlConfigFallbackStyle expectedStyle)
    {
        Assert.Equal(expectedStyle, OpenAiEndpointResolver.Features[feature].BaseUrlConfigFallback);
        Assert.Equal(IntegrationKey.OpenAI, OpenAiEndpointResolver.Features[feature].IntegrationKey);
        Assert.Equal(OpenAiOptions.SectionName, OpenAiEndpointResolver.Features[feature].OptionsSection);
        Assert.Equal("https://api.openai.com/v1/", OpenAiEndpointResolver.Features[feature].DefaultBaseUrl);
    }

    [Theory]
    [InlineData(OpenAiFeature.WhoAmI)]
    [InlineData(OpenAiFeature.AssistantChat)]
    [InlineData(OpenAiFeature.CareerPathPlan)]
    [InlineData(OpenAiFeature.CultureFit)]
    [InlineData(OpenAiFeature.VacancyContentModeration)]
    [InlineData(OpenAiFeature.CvExtraction)]
    [InlineData(OpenAiFeature.MockInterview)]
    [InlineData(OpenAiFeature.Translation)]
    [InlineData(OpenAiFeature.CareerCompass)]
    [InlineData(OpenAiFeature.RoleFitCheck)]
    [InlineData(OpenAiFeature.CompetenceDeepReport)]
    public async Task Mistral_provider_ignores_the_openai_database_key_model_and_url(OpenAiFeature feature)
    {
        var sut = CreateSut(
            dbApiKey: "sk-openai-db",
            dbModel: "gpt-4o-mini",
            dbBaseUrl: "https://api.openai.com/v1/",
            options: new OpenAiOptions
            {
                ApiKey = "sk-openai-config",
                Model = "gpt-4o-mini",
                BaseUrl = "https://api.openai.com/v1/"
            },
            ai: new AiOptions { Provider = "Mistral" },
            mistral: new MistralOptions { ApiKey = "mistral-test-key", Model = "  ", BaseUrl = "  " });

        var resolved = await sut.ResolveAsync(feature);

        Assert.Equal("mistral-test-key", resolved.ApiKey);
        Assert.Equal(MistralOptions.DefaultModel, resolved.Model);
        Assert.Equal(MistralOptions.DefaultBaseUrl, resolved.BaseUrl);
    }

    [Fact]
    public async Task Mistral_without_a_key_does_not_use_openai()
    {
        var log = new CapturingPlatformLog();
        var sut = CreateSut(
            dbApiKey: "sk-openai-db",
            options: new OpenAiOptions { ApiKey = "sk-config", Model = "gpt-4o-mini" },
            ai: new AiOptions { Provider = "mistral" },
            mistral: new MistralOptions { ApiKey = "  " },
            platformLog: log);

        var resolved = await sut.ResolveAsync(OpenAiFeature.Translation);
        await sut.ResolveAsync(OpenAiFeature.AssistantChat);

        Assert.True(resolved.Unavailable);
        Assert.Null(resolved.ApiKey);
        Assert.DoesNotContain("openai.com", resolved.BaseUrl, StringComparison.OrdinalIgnoreCase);
        var message = Assert.Single(log.Messages);
        Assert.Contains("OpenAI wordt niet gebruikt", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Explicit_openai_still_uses_the_openai_key()
    {
        var sut = CreateSut(
            dbApiKey: "sk-openai-db",
            options: new OpenAiOptions { ApiKey = "sk-config" },
            ai: new AiOptions { Provider = "OpenAI" },
            mistral: new MistralOptions());

        var resolved = await sut.ResolveAsync(OpenAiFeature.Translation);

        Assert.False(resolved.Unavailable);
        Assert.Equal("sk-openai-db", resolved.ApiKey);
        Assert.Contains("api.openai.com", resolved.BaseUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mistral_feature_model_overrides_only_that_feature()
    {
        var sut = CreateSut(
            ai: new AiOptions { Provider = "Mistral" },
            mistral: new MistralOptions
            {
                ApiKey = "mistral-test-key",
                Model = "mistral-small-latest",
                Models = new MistralFeatureModels
                {
                    Story = "mistral-medium-latest",
                    CareerReport = "  ",
                    Compass = "mistral-medium-latest",
                    Chat = null
                }
            });

        var story = await sut.ResolveAsync(OpenAiFeature.WhoAmI);
        var chat = await sut.ResolveAsync(OpenAiFeature.AssistantChat);
        var compass = await sut.ResolveAsync(OpenAiFeature.CareerCompass);
        var translation = await sut.ResolveAsync(OpenAiFeature.Translation);

        Assert.Equal("mistral-medium-latest", story.Model);
        Assert.Equal("mistral-small-latest", chat.Model);
        Assert.Equal("mistral-medium-latest", compass.Model);
        Assert.Equal("mistral-small-latest", translation.Model);
    }

    [Fact]
    public async Task Mistral_career_report_model_is_used_when_compass_slot_is_empty()
    {
        var sut = CreateSut(
            ai: new AiOptions { Provider = "Mistral" },
            mistral: new MistralOptions
            {
                ApiKey = "mistral-test-key",
                Model = "mistral-small-latest",
                Models = new MistralFeatureModels { CareerReport = "mistral-medium-latest" }
            });

        var compass = await sut.ResolveAsync(OpenAiFeature.CareerCompass);
        var story = await sut.ResolveAsync(OpenAiFeature.WhoAmI);

        Assert.Equal("mistral-medium-latest", compass.Model);
        Assert.Equal(MistralOptions.DefaultModel, story.Model);
    }

    [Fact]
    public async Task Mistral_compass_model_wins_over_the_career_report_model()
    {
        var sut = CreateSut(
            ai: new AiOptions { Provider = "Mistral" },
            mistral: new MistralOptions
            {
                ApiKey = "mistral-test-key",
                Models = new MistralFeatureModels
                {
                    Compass = "mistral-small-latest",
                    CareerReport = "mistral-medium-latest"
                }
            });

        var compass = await sut.ResolveAsync(OpenAiFeature.CareerCompass);

        Assert.Equal("mistral-small-latest", compass.Model);
    }

    [Fact]
    public async Task Mistral_keeps_a_global_host_and_rejects_a_private_host()
    {
        var global = CreateSut(
            ai: new AiOptions { Provider = "Mistral" },
            mistral: new MistralOptions
            {
                ApiKey = "mistral-test-key",
                BaseUrl = "https://api.mistral.ai/v1"
            });
        var globalResolved = await global.ResolveAsync(OpenAiFeature.CvExtraction);
        Assert.Equal("https://api.mistral.ai/v1/", globalResolved.BaseUrl);
        Assert.False(MistralEndpoint.InferenceStaysInEu(globalResolved.BaseUrl));

        var blocked = CreateSut(
            ai: new AiOptions { Provider = "Mistral" },
            mistral: new MistralOptions
            {
                ApiKey = "mistral-test-key",
                BaseUrl = "http://127.0.0.1/v1/"
            });
        var blockedResolved = await blocked.ResolveAsync(OpenAiFeature.CvExtraction);
        Assert.Equal(MistralOptions.DefaultBaseUrl, blockedResolved.BaseUrl);
    }

    [Fact]
    public void Feature_table_covers_all_enum_values()
    {
        foreach (var feature in Enum.GetValues<OpenAiFeature>())
        {
            Assert.True(
                OpenAiEndpointResolver.Features.ContainsKey(feature),
                $"Missing defaults for {feature}");
        }
    }

    private static OpenAiEndpointResolver CreateSut(
        string? dbApiKey = null,
        string? dbModel = null,
        string? dbBaseUrl = null,
        OpenAiOptions? options = null,
        AiOptions? ai = null,
        MistralOptions? mistral = null,
        Jobsy.Core.Diagnostics.IPlatformErrorLog? platformLog = null)
        => new(
            new StubCredentials(dbApiKey, dbModel, dbBaseUrl),
            Options.Create(options ?? new OpenAiOptions { ApiKey = null, Model = "   ", BaseUrl = "   " }),
            Options.Create(ai ?? new AiOptions()),
            Options.Create(mistral ?? new MistralOptions()),
            logger: null,
            platformLog: platformLog);

    private sealed class CapturingPlatformLog : Jobsy.Core.Diagnostics.IPlatformErrorLog
    {
        public List<string> Messages { get; } = [];

        public Task WriteAsync(
            string category,
            string message,
            string? supportCode,
            string? detail,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class StubCredentials(
        string? apiKey,
        string? model,
        string? baseUrl) : IIntegrationCredentialService
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
            => Task.FromResult(model);

        public Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(baseUrl);

        public Task<IntegrationCredentialSecrets?> GetSecretsAsync(
            IntegrationKey key,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialSecrets?>(null);
    }
}
