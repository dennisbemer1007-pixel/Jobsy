using Jobsy.Core.Ai;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services.OpenAi;

/// <summary>
/// Shared OpenAI API-key / model / base-URL resolution formerly copied into 11 services.
/// Fallback order is unchanged: DB integration credentials → <see cref="OpenAiOptions"/> → feature defaults.
/// </summary>
public sealed class OpenAiEndpointResolver : IOpenAiEndpointResolver
{
    public const string DefaultModel = "gpt-4o-mini";
    public const string DefaultBaseUrl = "https://api.openai.com/v1/";

    /// <summary>
    /// How config/env <see cref="OpenAiOptions.BaseUrl"/> is applied when DB has no usable base URL.
    /// Preserves the two legacy behaviours found across the 11 copies.
    /// </summary>
    public enum BaseUrlConfigFallbackStyle
    {
        /// <summary>
        /// Run <see cref="IntegrationEndpointUrl.TryNormalizeBaseUrl"/> on the config value;
        /// if that fails, return <see cref="DefaultBaseUrl"/>.
        /// </summary>
        NormalizeOrDefault,

        /// <summary>
        /// Trim the config value and ensure a trailing slash (no SSRF normalize on config).
        /// Used by CareerPathPlan and CompetenceDeepReport before dedup.
        /// </summary>
        TrimAndEnsureSlash,
    }

    public sealed record FeatureDefaults(
        string DefaultModel,
        string DefaultBaseUrl,
        IntegrationKey IntegrationKey,
        string OptionsSection,
        BaseUrlConfigFallbackStyle BaseUrlConfigFallback);

    /// <summary>Per-feature defaults and setting keys (literal values from the pre-dedup copies).</summary>
    public static readonly IReadOnlyDictionary<OpenAiFeature, FeatureDefaults> Features =
        new Dictionary<OpenAiFeature, FeatureDefaults>
        {
            [OpenAiFeature.WhoAmI] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.AssistantChat] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.CareerPathPlan] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.TrimAndEnsureSlash),
            [OpenAiFeature.CultureFit] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.VacancyContentModeration] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.CvExtraction] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.MockInterview] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.Translation] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.CareerCompass] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.RoleFitCheck] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.NormalizeOrDefault),
            [OpenAiFeature.CompetenceDeepReport] = new(DefaultModel, DefaultBaseUrl, IntegrationKey.OpenAI, OpenAiOptions.SectionName, BaseUrlConfigFallbackStyle.TrimAndEnsureSlash),
        };

    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly AiOptions _ai;
    private readonly MistralOptions _mistral;
    private readonly ILogger<OpenAiEndpointResolver>? _logger;
    private readonly IPlatformErrorLog? _platformLog;

    public OpenAiEndpointResolver(
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        IOptions<AiOptions>? ai = null,
        IOptions<MistralOptions>? mistral = null,
        ILogger<OpenAiEndpointResolver>? logger = null,
        IPlatformErrorLog? platformLog = null)
    {
        _credentials = credentials;
        _options = options.Value;
        _ai = ai?.Value ?? new AiOptions();
        _mistral = mistral?.Value ?? new MistralOptions();
        _logger = logger;
        _platformLog = platformLog;
    }

    public async Task<OpenAiEndpointResolution> ResolveAsync(
        OpenAiFeature feature,
        CancellationToken cancellationToken = default)
    {
        if (!Features.TryGetValue(feature, out var defaults))
        {
            throw new ArgumentOutOfRangeException(nameof(feature), feature, "Unknown OpenAI feature.");
        }

        var decision = AiProviderChoice.Decide(_ai.Provider, _mistral.ApiKey);
        if (!decision.Available)
        {
            await AiProviderFallbackLog.ReportUnavailableAsync(
                _logger,
                _platformLog,
                decision,
                _ai.Provider,
                cancellationToken);
            return new OpenAiEndpointResolution(null, string.Empty, string.Empty, Unavailable: true);
        }

        if (decision.Kind == AiProviderKind.Mistral)
        {
            return new OpenAiEndpointResolution(
                _mistral.ApiKey!.Trim(),
                ResolveMistralModel(),
                ResolveMistralBaseUrl());
        }

        var apiKey = await ResolveApiKeyAsync(defaults.IntegrationKey, cancellationToken);
        var model = await ResolveModelAsync(defaults, cancellationToken);
        var baseUrl = await ResolveBaseUrlAsync(defaults, cancellationToken);
        return new OpenAiEndpointResolution(apiKey, model, baseUrl);
    }

    private string ResolveMistralModel()
        => string.IsNullOrWhiteSpace(_mistral.Model)
            ? MistralOptions.DefaultModel
            : _mistral.Model.Trim();

    private string ResolveMistralBaseUrl()
        => MistralEndpoint.EffectiveBaseUrl(_mistral.BaseUrl);

    private async Task<string?> ResolveApiKeyAsync(IntegrationKey key, CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetRawApiKeyAsync(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.ApiKey) ? null : _options.ApiKey.Trim();
    }

    private async Task<string> ResolveModelAsync(FeatureDefaults defaults, CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetModelAsync(defaults.IntegrationKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.Model)
            ? defaults.DefaultModel
            : _options.Model.Trim();
    }

    private async Task<string> ResolveBaseUrlAsync(FeatureDefaults defaults, CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetBaseUrlAsync(defaults.IntegrationKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb)
            && IntegrationEndpointUrl.TryNormalizeBaseUrl(fromDb, out var normalized, out _)
            && !string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return defaults.BaseUrlConfigFallback switch
        {
            BaseUrlConfigFallbackStyle.TrimAndEnsureSlash => ResolveBaseUrlTrimAndEnsureSlash(defaults.DefaultBaseUrl),
            _ => ResolveBaseUrlNormalizeOrDefault(defaults.DefaultBaseUrl),
        };
    }

    private string ResolveBaseUrlNormalizeOrDefault(string defaultBaseUrl)
    {
        var fallback = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? defaultBaseUrl
            : _options.BaseUrl;
        if (IntegrationEndpointUrl.TryNormalizeBaseUrl(fallback, out var normalizedFallback, out _)
            && !string.IsNullOrWhiteSpace(normalizedFallback))
        {
            return normalizedFallback;
        }

        return defaultBaseUrl;
    }

    private string ResolveBaseUrlTrimAndEnsureSlash(string defaultBaseUrl)
    {
        var fallback = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? defaultBaseUrl
            : _options.BaseUrl.Trim();
        return fallback.EndsWith('/') ? fallback : fallback + "/";
    }
}
