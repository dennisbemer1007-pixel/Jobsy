using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Translates vacancy content via OpenAI when configured, with an in-memory cache.
/// Without an API key, returns the original text unchanged (no "[EN]" stubs).
/// </summary>
public sealed class OpenAiTranslationService : ITranslationService
{
    private const int MaxInputChars = 6_000;
    private const int MaxCacheEntries = 500;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.Ordinal);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiTranslationService> _logger;

    public OpenAiTranslationService(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiTranslationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        var source = JobsyLanguages.Normalize(sourceLanguage);
        var target = JobsyLanguages.Normalize(targetLanguage);
        if (JobsyLanguages.AreSame(source, target))
        {
            return text;
        }

        var clipped = Clip(text);
        var cacheKey = CacheKey(source, target, clipped);
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return text;
        }

        try
        {
            var translated = await CompleteAsync(clipped, source, target, apiKey, cancellationToken);
            if (string.IsNullOrWhiteSpace(translated))
            {
                return text;
            }

            var result = translated.Trim();
            Remember(cacheKey, result);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI translation failed ({Source}→{Target}); returning original.", source, target);
            return text;
        }
    }

    public async Task<TranslatedVacancyContent> TranslateVacancyAsync(
        string title,
        string description,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var source = JobsyLanguages.Normalize(sourceLanguage);
        var target = JobsyLanguages.Normalize(targetLanguage);
        if (JobsyLanguages.AreSame(source, target))
        {
            return new TranslatedVacancyContent(title, description, source, target, WasTranslated: false);
        }

        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TranslatedVacancyContent(title, description, source, target, WasTranslated: false);
        }

        var titleKey = CacheKey(source, target, "t:" + title);
        var descKey = CacheKey(source, target, "d:" + Clip(description));
        var titleCached = Cache.TryGetValue(titleKey, out var cachedTitle);
        var descCached = Cache.TryGetValue(descKey, out var cachedDesc);

        if (titleCached && descCached)
        {
            return new TranslatedVacancyContent(cachedTitle!, cachedDesc!, source, target, WasTranslated: true);
        }

        try
        {
            var batch = await CompleteVacancyBatchAsync(title, Clip(description), source, target, apiKey, cancellationToken);
            var translatedTitle = string.IsNullOrWhiteSpace(batch.Title) ? title : batch.Title.Trim();
            var translatedDescription = string.IsNullOrWhiteSpace(batch.Description) ? description : batch.Description.Trim();
            Remember(titleKey, translatedTitle);
            Remember(descKey, translatedDescription);
            var changed = !string.Equals(translatedTitle, title, StringComparison.Ordinal)
                          || !string.Equals(translatedDescription, description, StringComparison.Ordinal);
            return new TranslatedVacancyContent(translatedTitle, translatedDescription, source, target, changed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI vacancy translation failed ({Source}→{Target}); returning original.", source, target);
            return new TranslatedVacancyContent(title, description, source, target, WasTranslated: false);
        }
    }

    private async Task<(string? Title, string? Description)> CompleteVacancyBatchAsync(
        string title,
        string description,
        string source,
        string target,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var sourceName = JobsyLanguages.Get(source).NativeName;
        var targetName = JobsyLanguages.Get(target).NativeName;
        var model = await ResolveModelAsync(cancellationToken);
        var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
        var payload = JsonSerializer.Serialize(new { title, description });

        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.2,
            max_tokens = Math.Clamp(400 + description.Length / 2, 600, 2500),
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content =
                        $"You are a professional translator from {sourceName} to {targetName}. " +
                        "Return ONLY valid JSON: {\"title\":\"...\",\"description\":\"...\"}. " +
                        "Keep company names and street addresses unchanged. No markdown."
                },
                new { role = "user", content = payload }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "OpenAI vacancy translation HTTP {Status}: {Body}",
                (int)response.StatusCode,
                body.Length > 300 ? body[..300] : body);
            return (null, null);
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var raw = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null);
        }

        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            return (null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var t = root.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
            var d = root.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
            return (t, d);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OpenAI vacancy translation returned invalid JSON.");
            return (null, null);
        }
    }

    private async Task<string?> CompleteAsync(
        string text,
        string source,
        string target,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var sourceName = JobsyLanguages.Get(source).NativeName;
        var targetName = JobsyLanguages.Get(target).NativeName;
        var model = await ResolveModelAsync(cancellationToken);
        var baseUrl = await ResolveBaseUrlAsync(cancellationToken);

        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.2,
            max_tokens = Math.Clamp(200 + text.Length / 2, 400, 2500),
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content =
                        $"You are a professional translator. Translate from {sourceName} to {targetName}. " +
                        "Return ONLY the translation — no quotes, no labels, no explanations. " +
                        "Keep company names and street addresses unchanged when they appear."
                },
                new { role = "user", content = text }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "OpenAI translation HTTP {Status}: {Body}",
                (int)response.StatusCode,
                body.Length > 300 ? body[..300] : body);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        return completion?.Choices?.FirstOrDefault()?.Message?.Content;
    }

    private static string Clip(string text)
        => text.Length <= MaxInputChars ? text : text[..MaxInputChars].TrimEnd() + "…";

    private static string CacheKey(string source, string target, string text)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        return $"{source}|{target}|{hash}";
    }

    private static void Remember(string key, string value)
    {
        if (Cache.Count >= MaxCacheEntries)
        {
            // Simple eviction: drop an arbitrary entry when full.
            foreach (var existing in Cache.Keys.Take(50))
            {
                Cache.TryRemove(existing, out _);
            }
        }

        Cache[key] = value;
    }

    private static string? ExtractJsonObject(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith('{'))
        {
            return trimmed;
        }

        var match = Regex.Match(trimmed, @"\{[\s\S]*\}");
        return match.Success ? match.Value : null;
    }

    private async Task<string?> ResolveApiKeyAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetRawApiKeyAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.ApiKey) ? null : _options.ApiKey.Trim();
    }

    private async Task<string> ResolveModelAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetModelAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model.Trim();
    }

    private async Task<string> ResolveBaseUrlAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetBaseUrlAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb)
            && IntegrationEndpointUrl.TryNormalizeBaseUrl(fromDb, out var normalized, out _)
            && !string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var fallback = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.openai.com/v1/"
            : _options.BaseUrl;
        if (IntegrationEndpointUrl.TryNormalizeBaseUrl(fallback, out var normalizedFallback, out _)
            && !string.IsNullOrWhiteSpace(normalizedFallback))
        {
            return normalizedFallback;
        }

        return "https://api.openai.com/v1/";
    }

    private sealed class ChatCompletionResponse
    {
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        public string? Content { get; set; }
    }
}
