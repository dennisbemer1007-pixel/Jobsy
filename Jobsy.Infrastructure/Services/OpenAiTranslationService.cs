using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Translates vacancy content via OpenAI when configured.
/// Persists per-vacancy translations in <see cref="VacancyTranslation"/> with a small IMemoryCache in front.
/// Without an API key or on failure, returns the original (Dutch) text.
/// </summary>
public sealed class OpenAiTranslationService : ITranslationService
{
    private const int MaxInputChars = 6_000;
    private const int MaxParallelTranslations = 4;
    public static readonly TimeSpan MemoryCacheTtl = TimeSpan.FromMinutes(30);
    private const string MemoryCachePrefix = "vac-tr:";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly JobsyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenAiTranslationService> _logger;

    public OpenAiTranslationService(
        JobsyDbContext db,
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        IMemoryCache cache,
        ILogger<OpenAiTranslationService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var batch = await TranslateVacancyAsync(text, string.Empty, sourceLanguage, targetLanguage, cancellationToken);
        return batch.Title;
    }

    public async Task<TranslatedVacancyContent> TranslateVacancyAsync(
        string title,
        string description,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default,
        Guid? vacancyId = null)
    {
        var source = JobsyLanguages.Normalize(sourceLanguage);
        var target = JobsyLanguages.Normalize(targetLanguage);
        if (JobsyLanguages.AreSame(source, target))
        {
            return new TranslatedVacancyContent(title, description, source, target, WasTranslated: false, vacancyId);
        }

        if (vacancyId is Guid vid)
        {
            var fromStore = await TryReadStoredAsync(vid, title, description, source, target, cancellationToken);
            if (fromStore is not null)
            {
                return fromStore;
            }
        }

        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TranslatedVacancyContent(title, description, source, target, WasTranslated: false, vacancyId);
        }

        try
        {
            var batch = await CompleteVacancyBatchAsync(title, Clip(description), source, target, apiKey, cancellationToken);
            var translatedTitle = string.IsNullOrWhiteSpace(batch.Title) ? title : batch.Title.Trim();
            var translatedDescription = string.IsNullOrWhiteSpace(batch.Description) ? description : batch.Description.Trim();
            var changed = !string.Equals(translatedTitle, title, StringComparison.Ordinal)
                          || !string.Equals(translatedDescription, description, StringComparison.Ordinal);
            if (changed && vacancyId is Guid persistId)
            {
                await PersistAsync(persistId, title, description, target, translatedTitle, translatedDescription, cancellationToken);
            }

            return new TranslatedVacancyContent(
                translatedTitle,
                translatedDescription,
                source,
                target,
                WasTranslated: changed,
                vacancyId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI vacancy translation failed ({Source}→{Target}); returning original.", source, target);
            return new TranslatedVacancyContent(title, description, source, target, WasTranslated: false, vacancyId);
        }
    }

    public async Task<IReadOnlyList<TranslatedVacancyContent>> TranslateVacanciesBatchAsync(
        IReadOnlyList<VacancyTranslationRequest> items,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var source = JobsyLanguages.Normalize(sourceLanguage);
        var target = JobsyLanguages.Normalize(targetLanguage);
        if (items.Count == 0)
        {
            return [];
        }

        if (JobsyLanguages.AreSame(source, target))
        {
            return items
                .Select(i => new TranslatedVacancyContent(i.Title, i.Description, source, target, false, i.VacancyId))
                .ToList();
        }

        var results = new TranslatedVacancyContent[items.Count];
        var missing = new List<int>();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var stored = await TryReadStoredAsync(
                item.VacancyId, item.Title, item.Description, source, target, cancellationToken);
            if (stored is not null)
            {
                results[i] = stored;
            }
            else
            {
                missing.Add(i);
                results[i] = new TranslatedVacancyContent(
                    item.Title, item.Description, source, target, false, item.VacancyId);
            }
        }

        if (missing.Count == 0)
        {
            return results;
        }

        using var gate = new SemaphoreSlim(MaxParallelTranslations);
        var tasks = missing.Select(async index =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var item = items[index];
                results[index] = await TranslateVacancyAsync(
                    item.Title,
                    item.Description,
                    source,
                    target,
                    cancellationToken,
                    item.VacancyId);
            }
            finally
            {
                gate.Release();
            }
        });
        await Task.WhenAll(tasks);
        return results;
    }

    public async Task InvalidateVacancyAsync(Guid vacancyId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.VacancyTranslations
            .Where(t => t.VacancyId == vacancyId)
            .ToListAsync(cancellationToken);
        if (rows.Count > 0)
        {
            _db.VacancyTranslations.RemoveRange(rows);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Drop memory cache entries for this vacancy (any language).
        // Keys are vac-tr:{id}:{lang}:{hash} — clear by known languages when possible.
        foreach (var lang in JobsyLanguages.All.Select(l => l.Code))
        {
            // We cannot enumerate MemoryCache keys; callers re-read DB after invalidation.
            _cache.Remove(MemoryKey(vacancyId, lang, ""));
        }
    }

    private async Task<TranslatedVacancyContent?> TryReadStoredAsync(
        Guid vacancyId,
        string title,
        string description,
        string source,
        string target,
        CancellationToken cancellationToken)
    {
        var sourceHash = VacancyTranslationHash.ForSource(title, description);
        var memKey = MemoryKey(vacancyId, target, sourceHash);
        if (_cache.TryGetValue(memKey, out TranslatedVacancyContent? cached) && cached is not null)
        {
            return cached;
        }

        var row = await _db.VacancyTranslations.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.VacancyId == vacancyId
                     && t.Language == target
                     && t.SourceHash == sourceHash,
                cancellationToken);
        if (row is null)
        {
            return null;
        }

        var parsed = TryParseTranslatedJson(row.TranslatedJson);
        if (parsed is null)
        {
            return null;
        }

        var content = new TranslatedVacancyContent(
            parsed.Value.Title,
            parsed.Value.Description,
            source,
            target,
            WasTranslated: true,
            vacancyId);
        _cache.Set(memKey, content, MemoryCacheTtl);
        return content;
    }

    private async Task PersistAsync(
        Guid vacancyId,
        string sourceTitle,
        string sourceDescription,
        string target,
        string translatedTitle,
        string translatedDescription,
        CancellationToken cancellationToken)
    {
        var sourceHash = VacancyTranslationHash.ForSource(sourceTitle, sourceDescription);
        var json = JsonSerializer.Serialize(new { title = translatedTitle, description = translatedDescription });
        var row = await _db.VacancyTranslations
            .FirstOrDefaultAsync(t => t.VacancyId == vacancyId && t.Language == target, cancellationToken);
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new VacancyTranslation
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyId,
                Language = target
            };
            _db.VacancyTranslations.Add(row);
        }

        row.SourceHash = sourceHash;
        row.TranslatedJson = json;
        row.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Set(
            MemoryKey(vacancyId, target, sourceHash),
            new TranslatedVacancyContent(translatedTitle, translatedDescription, "nl", target, true, vacancyId),
            MemoryCacheTtl);
    }

    private static (string Title, string Description)? TryParseTranslatedJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var title = doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var description = doc.RootElement.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            return (title, description);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string MemoryKey(Guid vacancyId, string language, string sourceHash)
        => $"{MemoryCachePrefix}{vacancyId:D}:{language}:{sourceHash}";

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

    private static string Clip(string text)
        => text.Length <= MaxInputChars ? text : text[..MaxInputChars].TrimEnd() + "…";

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
