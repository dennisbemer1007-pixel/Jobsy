using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class WhoAmIGenerationService : IWhoAmIGenerationService
{
    public const string HttpClientName = "OpenAIWhoAmI";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<WhoAmIGenerationService> _logger;

    public WhoAmIGenerationService(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<WhoAmIGenerationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WhoAmIGeneratedStory> GenerateAsync(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights? profile = null,
        CancellationToken cancellationToken = default)
    {
        profile ??= WhoAmIProfileHighlights.Empty;
        var local = Local(competency, career, culture, profile);
        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return local;
        }

        try
        {
            var generated = await GenerateWithOpenAiAsync(
                competency,
                career,
                culture,
                profile,
                apiKey,
                await ResolveModelAsync(cancellationToken),
                await ResolveBaseUrlAsync(cancellationToken),
                cancellationToken);
            if (generated is not null)
            {
                return generated;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI Wie-ben-ik verhaal mislukt; lokale tekst wordt gebruikt.");
        }

        return local;
    }

    private static WhoAmIGeneratedStory Local(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights profile)
        => new(
            WhoAmIStoryBuilder.Build(competency, career, culture, profile),
            WhoAmIKeywords.FromScores(competency, career, culture),
            FromOpenAi: false);

    private async Task<WhoAmIGeneratedStory?> GenerateWithOpenAiAsync(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights profile,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.5,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = WhoAmIPrompt.System },
                new { role = "user", content = WhoAmIPrompt.User(competency, career, culture, profile) }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "OpenAI Wie-ben-ik gaf {StatusCode} (response body not logged).",
                (int)response.StatusCode);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        StoryDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<StoryDto>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        var story = WhoAmIStoryBuilder.Sanitize(dto?.Story);
        if (story is null || !story.Contains("ik", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var keywords = (dto?.Keywords ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k) && !CareerCompassBuilder.ContainsForbiddenJargon(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(WhoAmIKeywords.MaxCount)
            .ToList();
        if (keywords.Count == 0)
        {
            keywords = WhoAmIKeywords.FromScores(competency, career, culture).ToList();
        }

        return new WhoAmIGeneratedStory(story, keywords, FromOpenAi: true);
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

    private sealed class StoryDto
    {
        public string? Story { get; set; }
        public List<string>? Keywords { get; set; }
    }

    private sealed class ChatCompletionResponse
    {
        public List<Choice>? Choices { get; set; }

        public sealed class Choice
        {
            public Message? Message { get; set; }
        }

        public sealed class Message
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
