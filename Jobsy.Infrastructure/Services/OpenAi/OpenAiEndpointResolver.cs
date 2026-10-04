using System.Threading;
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
    public const string DefaultModel = OpenAiOptions.DefaultModel;
    public const string DefaultBaseUrl = OpenAiOptions.DefaultBaseUrl;

    private static int _unknownProviderWarned;
    private static int _missingMistralKeyWarned;

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

    public OpenAiEndpointResolver(
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        IOptions<AiOptions>? ai = null,
        IOptions<MistralOptions>? mistral = null,
        ILogger<OpenAiEndpointResolver>? logger = null)
    {
        _credentials = credentials;
        _options = options.Value;
        _ai = ai?.Value ?? new AiOptions();
        _mistral = mistral?.Value ?? new MistralOptions();
        _logger = logger;
    }

    public async Task<OpenAiEndpointResolution> ResolveAsync(
        OpenAiFeature feature,
        CancellationToken cancellationToken = default)
    {
        if (!Features.TryGetValue(feature, out var defaults))
        {
            throw new ArgumentOutOfRangeException(nameof(feature), feature, "Unknown OpenAI feature.");
        }

        WarnUnknownProviderOnce();
        if (_ai.Resolved == AiProvider.Mistral)
        {
            return ResolveMistral();
        }

        var apiKey = await ResolveApiKeyAsync(defaults.IntegrationKey, cancellationToken);
        var model = await ResolveModelAsync(defaults, cancellationToken);
        var baseUrl = await ResolveBaseUrlAsync(defaults, cancellationToken);
        return new OpenAiEndpointResolution(apiKey, model, baseUrl);
    }

    /// <summary>
    /// Mistral uses its own key, model and base URL. The OpenAI row in admin integrations
    /// (often <c>gpt-4o-mini</c>) is not sent, because that model name is rejected by Mistral.
    /// </summary>
    private OpenAiEndpointResolution ResolveMistral()
    {
        var apiKey = string.IsNullOrWhiteSpace(_mistral.ApiKey) ? null : _mistral.ApiKey.Trim();
        if (apiKey is null)
        {
            WarnMissingMistralKeyOnce();
        }

        var model = string.IsNullOrWhiteSpace(_mistral.Model)
            ? MistralOptions.DefaultModel
            : _mistral.Model.Trim();
        return new OpenAiEndpointResolution(apiKey, model, ResolveMistralBaseUrl());
    }

    private string ResolveMistralBaseUrl()
    {
        var fallback = string.IsNullOrWhiteSpace(_mistral.BaseUrl)
            ? MistralOptions.DefaultBaseUrl
            : _mistral.BaseUrl;
        if (IntegrationEndpointUrl.TryNormalizeBaseUrl(fallback, out var normalized, out _)
            && !string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return MistralOptions.DefaultBaseUrl;
    }

    private void WarnUnknownProviderOnce()
    {
        if (_ai.IsKnownProvider || string.IsNullOrWhiteSpace(_ai.Provider))
        {
            return;
        }

        if (Interlocked.Exchange(ref _unknownProviderWarned, 1) == 1)
        {
            return;
        }

        _logger?.LogWarning(
            "AI provider '{Provider}' is unknown. Using OpenAI.",
            _ai.Provider.Trim());
    }

    private void WarnMissingMistralKeyOnce()
    {
        if (Interlocked.Exchange(ref _missingMistralKeyWarned, 1) == 1)
        {
            return;
        }

        _logger?.LogWarning(
            "AI provider is Mistral but Mistral:ApiKey is empty. AI calls use the local fallback. Set Mistral__ApiKey. The OpenAI key is not used.");
    }

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
