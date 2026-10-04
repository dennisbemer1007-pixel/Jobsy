using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public CareerCompassGenerationService(
        IHttpClientFactory httpClientFactory,
        IOpenAiEndpointResolver openAi,
        ILogger<CareerCompassGenerationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<CareerCompassSnapshot> GenerateFromCareerDeepAsync(
        IReadOnlyDictionary<int, int> answers,
        CancellationToken cancellationToken = default)
    {
        var scores = DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career);
        var local = CareerCompassBuilder.Build(DeepAnalysisCatalog.ToRiasecScores(scores), fromDeepAnalysis: true);
        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.CareerCompass, cancellationToken);
        var apiKey = endpoint.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return local;
        }

        try
        {
            var model = endpoint.Model;
            var baseUrl = endpoint.BaseUrl;
            var generated = await GenerateWithOpenAiAsync(scores, answers, apiKey, model, baseUrl, cancellationToken);
            if (generated is { HasOccupations: true })
            {
                if (generated.Strengths.Count < 3)
                {
                    generated = generated with { Strengths = local.Strengths };
                }

                return generated;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI beroepen-kompas mislukt; lokale algemene catalogus wordt gebruikt.");
        }

        return local;
    }

    private async Task<CareerCompassSnapshot?> GenerateWithOpenAiAsync(
        IReadOnlyList<DeepAnalysisDomainScore> scores,
        IReadOnlyDictionary<int, int> answers,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var user = CareerCompassPrompt.User(scores, answers);
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
                new { role = "system", content = CareerCompassPrompt.System },
                new { role = "user", content = user }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "OpenAI beroepen-kompas gaf {StatusCode} (response body not logged).",
                (int)response.StatusCode);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var parsed = CareerCompassJson.TryDeserialize(content);
        return parsed is null ? null : parsed with { FromOpenAi = true, FromDeepAnalysis = true };
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
