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

public sealed class CultureFitAiService : ICultureFitAiService
{
    public const string HttpClientName = "OpenAICultureFit";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<CultureFitAiService> _logger;

    public CultureFitAiService(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<CultureFitAiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CultureFitResult?> TryRefineAsync(
        CultureFitResult local,
        CompetencyScores scores,
        IReadOnlyList<string> pillarLabels,
        DiscScores? disc = null,
        CancellationToken cancellationToken = default)
    {
        if (pillarLabels.Count == 0 || scores is not { IsComplete: true })
        {
            return null;
        }

        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            var model = await ResolveModelAsync(cancellationToken);
            var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
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
                    new { role = "system", content = CultureFitPrompt.System },
                    new { role = "user", content = CultureFitPrompt.User(pillarLabels, scores, disc) }
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
            return CultureFitJson.TryParse(content, local);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI cultuurfit mislukt; lokale score wordt gebruikt.");
            return null;
        }
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
