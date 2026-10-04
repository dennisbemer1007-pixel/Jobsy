using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Ai;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class WhoAmIGenerationService : IWhoAmIGenerationService
{
    public const string HttpClientName = "OpenAIWhoAmI";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly ILogger<WhoAmIGenerationService> _logger;
    private readonly IPlatformErrorLog? _platformLog;

    public WhoAmIGenerationService(
        IHttpClientFactory httpClientFactory,
        IOpenAiEndpointResolver openAi,
        ILogger<WhoAmIGenerationService> logger,
        IPlatformErrorLog? platformLog = null)
    {
        _httpClientFactory = httpClientFactory;
        _openAi = openAi;
        _logger = logger;
        _platformLog = platformLog;
    }

    public async Task<WhoAmIGeneratedStory> GenerateAsync(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights? profile = null,
        SchwartzValuesScores? values = null,
        CancellationToken cancellationToken = default)
    {
        profile ??= WhoAmIProfileHighlights.Empty;
        var local = Local(competency, career, culture, profile, values);
        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.WhoAmI, cancellationToken);
        if (endpoint.Unavailable)
        {
            return new WhoAmIGeneratedStory(AiUnavailableCopy.Dutch, [], FromOpenAi: false);
        }

        var apiKey = endpoint.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return local;
        }

        var sheet = CandidateFactSheet.ForWhoAmI(competency, career, culture, profile, values);
        try
        {
            using var openAiCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            openAiCts.CancelAfter(TimeSpan.FromSeconds(7));
            var systems = new[]
            {
                WhoAmIPrompt.System,
                WhoAmIPrompt.System + "\n" + CandidateFactGuard.StrictAddendum
            };
            for (var attempt = 1; attempt <= systems.Length; attempt++)
            {
                var (generated, reason) = await GenerateWithOpenAiAsync(
                    competency,
                    career,
                    culture,
                    profile,
                    values,
                    sheet,
                    systems[attempt - 1],
                    apiKey,
                    endpoint.Model,
                    endpoint.BaseUrl,
                    openAiCts.Token);
                if (generated is not null)
                {
                    return generated;
                }

                if (reason is not null)
                {
                    await AiFactRejectionLog.WriteAsync(
                        _platformLog,
                        _logger,
                        "whoami",
                        reason,
                        attempt,
                        cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("OpenAI Wie-ben-ik timed out; lokale tekst wordt gebruikt.");
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
        WhoAmIProfileHighlights profile,
        SchwartzValuesScores? values)
        => new(
            WhoAmIStoryBuilder.Build(competency, career, culture, profile, values),
            WhoAmIKeywords.FromScores(competency, career, culture, values),
            FromOpenAi: false);

    private async Task<(WhoAmIGeneratedStory? Story, string? Reason)> GenerateWithOpenAiAsync(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights profile,
        SchwartzValuesScores? values,
        CandidateFactSheet sheet,
        string systemPrompt,
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
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = sheet.ToPrompt() }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "OpenAI Wie-ben-ik gaf {StatusCode} (response body not logged).",
                (int)response.StatusCode);
            return (null, null);
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return (null, "empty");
        }

        StoryDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<StoryDto>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return (null, "unreadable");
        }

        var story = WhoAmIStoryBuilder.Sanitize(dto?.Story);
        var reason = CandidateFactGuard.RejectionReason(story, sheet)
            ?? (story is null || !story.Contains("ik", StringComparison.OrdinalIgnoreCase) || !WhoAmIStoryBuilder.Accepts(story, profile, competency, culture)
                ? "story-rules"
                : null);
        if (reason is not null || story is null)
        {
            return (null, reason ?? "story-rules");
        }

        var keywords = (dto?.Keywords ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k) && !CareerCompassBuilder.ContainsForbiddenJargon(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(WhoAmIKeywords.MaxCount)
            .ToList();
        if (keywords.Count == 0)
        {
            keywords = WhoAmIKeywords.FromScores(competency, career, culture, values).ToList();
        }

        return (new WhoAmIGeneratedStory(story, keywords, FromOpenAi: true), null);
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
