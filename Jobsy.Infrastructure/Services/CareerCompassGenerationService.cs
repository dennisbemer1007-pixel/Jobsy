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

public sealed class CareerCompassGenerationService : ICareerCompassGenerationService
{
    public const string HttpClientName = "OpenAICareerCompass";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly ILogger<CareerCompassGenerationService> _logger;
    private readonly IPlatformErrorLog? _platformLog;

    public CareerCompassGenerationService(
        IHttpClientFactory httpClientFactory,
        IOpenAiEndpointResolver openAi,
        ILogger<CareerCompassGenerationService> logger,
        IPlatformErrorLog? platformLog = null)
    {
        _httpClientFactory = httpClientFactory;
        _openAi = openAi;
        _logger = logger;
        _platformLog = platformLog;
    }

    public async Task<CareerCompassSnapshot> GenerateFromCareerDeepAsync(
        IReadOnlyDictionary<int, int> answers,
        CancellationToken cancellationToken = default)
    {
        var scores = DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career);
        var riasec = DeepAnalysisCatalog.ToRiasecScores(scores);
        var local = CareerCompassSanitize.EnsureDepth(
            CareerCompassBuilder.Build(riasec, fromDeepAnalysis: true),
            riasec);
        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.CareerCompass, cancellationToken);
        var apiKey = endpoint.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return local;
        }

        var sheet = CandidateFactSheet.ForCareer(scores, answers);
        try
        {
            var model = endpoint.Model;
            var baseUrl = endpoint.BaseUrl;
            var systems = new[]
            {
                CareerCompassPrompt.System,
                CareerCompassPrompt.System + "\n" + CandidateFactGuard.StrictAddendum
            };
            for (var attempt = 1; attempt <= systems.Length; attempt++)
            {
                var (generated, reason, rejectedText) = await GenerateWithOpenAiAsync(
                    sheet,
                    systems[attempt - 1],
                    apiKey,
                    model,
                    baseUrl,
                    cancellationToken);
                if (reason is not null)
                {
                    await AiFactRejectionLog.WriteAsync(
                        _platformLog,
                        _logger,
                        "compass",
                        reason,
                        attempt,
                        cancellationToken,
                        model,
                        rejectedText);
                }

                if (generated is not null)
                {
                    generated = CareerCompassSanitize.EnsureDepth(generated, riasec);
                }

                if (generated is { HasOccupations: true })
                {
                    if (generated.Strengths.Count < 3)
                    {
                        generated = generated with { Strengths = local.Strengths };
                    }

                    return generated;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI beroepen-kompas mislukt; lokale algemene catalogus wordt gebruikt.");
        }

        return local;
    }

    private async Task<(CareerCompassSnapshot? Snapshot, string? Reason, string? RejectedText)> GenerateWithOpenAiAsync(
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
            temperature = 0.1,
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
                "OpenAI beroepen-kompas gaf {StatusCode} (response body not logged).",
                (int)response.StatusCode);
            return (null, null, null);
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        var reason = CandidateFactGuard.CompassRejection(content, sheet);
        if (reason is not null)
        {
            return (null, reason, content);
        }

        var parsed = CareerCompassJson.TryDeserialize(content);
        return parsed is null
            ? (null, "unreadable", content)
            : (parsed with { FromOpenAi = true, FromDeepAnalysis = true }, null, null);
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
