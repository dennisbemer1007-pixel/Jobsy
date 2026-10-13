using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CultureFitAiService : ICultureFitAiService
{
    public const string HttpClientName = "OpenAICultureFit";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly ILogger<CultureFitAiService> _logger;
    private readonly IPlatformErrorLog? _platformLog;

    public CultureFitAiService(
        IHttpClientFactory httpClientFactory,
        IOpenAiEndpointResolver openAi,
        ILogger<CultureFitAiService> logger,
        IPlatformErrorLog? platformLog = null)
    {
        _httpClientFactory = httpClientFactory;
        _openAi = openAi;
        _logger = logger;
        _platformLog = platformLog;
    }

    public async Task<CultureFitResult?> TryRefineAsync(
        CultureFitResult local,
        CompetencyScores scores,
        IReadOnlyList<string> pillarLabels,
        CulturePersonalityScores? culture = null,
        CancellationToken cancellationToken = default)
    {
        if (pillarLabels.Count == 0 || scores is not { IsComplete: true })
        {
            return null;
        }

        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.CultureFit, cancellationToken);
        var apiKey = endpoint.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            var model = endpoint.Model;
            var baseUrl = endpoint.BaseUrl;
            var sheet = CandidateFactSheet.Personal(
                [],
                [],
                [],
                scores:
                [
                    $"Samenwerken {scores.Samenwerken ?? 0}%",
                    $"Afmaken {scores.Resultaatgerichtheid ?? 0}%"
                ],
                checkJobTitles: false);
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
                    new { role = "system", content = CultureFitPrompt.System + "\n" + sheet.ToPrompt() + "\n" + CandidateFactGuard.StrictAddendum },
                    new { role = "user", content = CultureFitPrompt.User(pillarLabels, scores, culture) }
                }
            });

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI cultuurfit gaf {StatusCode} (response body not logged).",
                    (int)response.StatusCode);
                return null;
            }

            var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            var parsed = CultureFitJson.TryParse(content, local);
            if (parsed is null)
            {
                return null;
            }

            var reason = CandidateFactGuard.RejectionReason(parsed.Why, sheet);
            if (reason is not null)
            {
                await AiFactRejectionLog.WriteAsync(
                    _platformLog, _logger, "culture-fit", reason, 1, cancellationToken, model, parsed.Why);
                return null;
            }

            return parsed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI cultuurfit mislukt; lokale score wordt gebruikt.");
            return null;
        }
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
