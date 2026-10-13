using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class VacancyContentModerationService : IVacancyContentModerationService
{
    private const string SystemPrompt =
        """
        Je bent een vriendelijke vacaturemoderator voor de Nederlandse arbeidsmarkt (gelijke behandeling).
        Beoordeel titel en tekst op verboden of discriminerende formuleringen:
        - leeftijd (grenzen, 'jong', 'ouder dan', …)
        - geslacht / gender
        - afkomst, nationaliteit, etniciteit
        - onnodige harde eisen die kandidaat onnodig uitsluiten

        Functionele eisen (rijbewijs als je moet rijden, relevante ervaring, taal op werkbaar niveau) mogen.
        Antwoord ALLEEN als JSON-object met exact deze velden:
        {"allowed":true|false,"warning":"korte vriendelijke waarschuwing of leeg","suggestion":"concrete verbetertip of leeg"}
        Schrijf warning en suggestion in het Nederlands. Bij allowed=true laat warning en suggestion leeg.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPlatformFeatureService _features;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly ILogger<VacancyContentModerationService> _logger;

    public VacancyContentModerationService(
        IHttpClientFactory httpClientFactory,
        IPlatformFeatureService features,
        IOpenAiEndpointResolver openAi,
        ILogger<VacancyContentModerationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _features = features;
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<VacancyContentModerationResult> CheckAsync(
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        var features = await _features.GetAsync(cancellationToken);
        if (!features.VacancyContentModerationEnabled)
        {
            return VacancyContentModerationResult.Allowed();
        }

        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.VacancyContentModeration, cancellationToken);
        var apiKey = endpoint.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return DutchVacancyModerationHeuristics.Check(title, description);
        }

        try
        {
            var model = endpoint.Model;
            var baseUrl = endpoint.BaseUrl;
            var aiResult = await CheckWithOpenAiAsync(title, description, apiKey, model, baseUrl, cancellationToken);
            if (aiResult is not null)
            {
                return aiResult;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI vacaturemoderatie mislukt; lokale check wordt gebruikt.");
        }

        return DutchVacancyModerationHeuristics.Check(title, description);
    }




    private async Task<VacancyContentModerationResult?> CheckWithOpenAiAsync(
        string title,
        string description,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var plain = HtmlSanitize.ToPlainPreview($"{title}\n\n{description}", maxLength: 8_000);
        var client = _httpClientFactory.CreateClient("IntegrationProbe");

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
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Titel: {title}\n\nTekst:\n{plain}" }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "OpenAI moderatie gaf {StatusCode}: {Body}",
                (int)response.StatusCode,
                body.Length > 400 ? body[..400] : body);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var parsed = JsonSerializer.Deserialize<ModerationJson>(content, JsonOptions);
        if (parsed is null)
        {
            return null;
        }

        if (parsed.Allowed)
        {
            return VacancyContentModerationResult.Allowed();
        }

        var warning = string.IsNullOrWhiteSpace(parsed.Warning)
            ? "We hebben een formulering gevonden die mogelijk discriminerend of onnodig streng is."
            : parsed.Warning.Trim();
        var suggestion = string.IsNullOrWhiteSpace(parsed.Suggestion)
            ? "Pas de tekst aan zodat eisen functioneel en inclusief zijn, en probeer opnieuw op te slaan."
            : parsed.Suggestion.Trim();

        return VacancyContentModerationResult.Blocked(warning, suggestion);
    }

    private sealed class ModerationJson
    {
        [JsonPropertyName("allowed")]
        public bool Allowed { get; set; }

        [JsonPropertyName("warning")]
        public string? Warning { get; set; }

        [JsonPropertyName("suggestion")]
        public string? Suggestion { get; set; }
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
