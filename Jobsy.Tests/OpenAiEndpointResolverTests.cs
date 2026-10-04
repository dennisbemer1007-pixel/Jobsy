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
    public async Task Mistral_without_a_key_stays_on_openai()
    {
        var sut = CreateSut(
            dbApiKey: "sk-openai-db",
            options: new OpenAiOptions { ApiKey = "sk-config", Model = "gpt-4o-mini" },
            ai: new AiOptions { Provider = "mistral" },
            mistral: new MistralOptions { ApiKey = "  " });

        var resolved = await sut.ResolveAsync(OpenAiFeature.Translation);

        Assert.Equal("sk-openai-db", resolved.ApiKey);
        Assert.Equal("https://api.openai.com/v1/", resolved.BaseUrl);
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
        MistralOptions? mistral = null)
        => new(
            new StubCredentials(dbApiKey, dbModel, dbBaseUrl),
            Options.Create(options ?? new OpenAiOptions { ApiKey = null, Model = "   ", BaseUrl = "   " }),
            Options.Create(ai ?? new AiOptions()),
            Options.Create(mistral ?? new MistralOptions()));

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
