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

/// <summary>
/// Normalizes scraped vacancy HTML into readable ATS fields (description, salary, start, requirements).
/// Uses OpenAI when configured; always falls back to local heuristics.
/// </summary>
public sealed class AtsListingEnrichmentService : IAtsListingEnrichmentService
{
    public const string HttpClientName = "OpenAIAtsEnrichment";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<AtsListingEnrichmentService> _logger;

    public AtsListingEnrichmentService(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<AtsListingEnrichmentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AtsListingEnrichmentResult> EnrichAsync(
        string? title,
        string? companyName,
        string? locationLabel,
        string? description,
        string? salaryText,
        string? hoursText,
        decimal? minHours,
        decimal? maxHours,
        CancellationToken cancellationToken = default)
    {
        var local = AtsListingEnrichment.FromHeuristics(
            title, locationLabel, description, salaryText, hoursText, minHours, maxHours);

        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(description))
        {
            return local;
        }

        try
        {
            var fromAi = await TryOpenAiAsync(
                title, companyName, locationLabel, description, salaryText, hoursText, apiKey, cancellationToken);
            if (fromAi is null)
            {
                return local;
            }

            // Prefer AI values when present; keep local fallbacks for blanks.
            return new AtsListingEnrichmentResult(
                Title: First(fromAi.Title, local.Title),
                Description: First(fromAi.Description, local.Description) ?? string.Empty,
                SalaryText: First(fromAi.SalaryText, local.SalaryText),
                HoursText: First(fromAi.HoursText, local.HoursText),
                MinHoursPerWeek: fromAi.MinHoursPerWeek ?? local.MinHoursPerWeek,
                MaxHoursPerWeek: fromAi.MaxHoursPerWeek ?? local.MaxHoursPerWeek,
                StartDateText: First(fromAi.StartDateText, local.StartDateText),
                LocationLabel: First(fromAi.LocationLabel, local.LocationLabel),
                RequirementsText: First(fromAi.RequirementsText, local.RequirementsText),
                FromOpenAi: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ATS AI-enrichment mislukt; lokale normalisatie gebruikt.");
            return local;
        }
    }

    private async Task<AtsListingEnrichmentResult?> TryOpenAiAsync(
        string? title,
        string? companyName,
        string? locationLabel,
        string description,
        string? salaryText,
        string? hoursText,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var model = await ResolveModelAsync(cancellationToken);
        var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        var userPayload =
            $"""
            Titel: {title ?? ""}
            Bedrijf: {companyName ?? ""}
            Locatie hint: {locationLabel ?? ""}
            Salaris hint: {salaryText ?? ""}
            Uren hint: {hoursText ?? ""}

            Brontekst:
            {(description.Length > 12_000 ? description[..12_000] : description)}
            """;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = AtsListingEnrichment.SystemPrompt },
                new { role = "user", content = userPayload }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "ATS OpenAI enrichment gaf {StatusCode} (body not logged).",
                (int)response.StatusCode);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
            JsonOptions, cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var parsed = JsonSerializer.Deserialize<EnrichmentJson>(content, JsonOptions);
        if (parsed is null)
        {
            return null;
        }

        var cleanedDescription = AtsListingEnrichment.CleanDescription(parsed.Description);
        if (string.IsNullOrWhiteSpace(cleanedDescription))
        {
            cleanedDescription = null;
        }

        return new AtsListingEnrichmentResult(
            Title: EmptyToNull(parsed.Title),
            Description: cleanedDescription,
            SalaryText: EmptyToNull(parsed.SalaryText),
            HoursText: EmptyToNull(parsed.HoursText),
            MinHoursPerWeek: parsed.MinHoursPerWeek is > 0 and <= 60 ? parsed.MinHoursPerWeek : null,
            MaxHoursPerWeek: parsed.MaxHoursPerWeek is > 0 and <= 60 ? parsed.MaxHoursPerWeek : null,
            StartDateText: EmptyToNull(parsed.StartDateText),
            LocationLabel: EmptyToNull(parsed.LocationLabel),
            RequirementsText: EmptyToNull(parsed.RequirementsText),
            FromOpenAi: true);
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

    private static string? First(string? preferred, string? fallback)
        => !string.IsNullOrWhiteSpace(preferred) ? preferred.Trim() : fallback;

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class EnrichmentJson
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? SalaryText { get; set; }
        public string? HoursText { get; set; }
        public decimal? MinHoursPerWeek { get; set; }
        public decimal? MaxHoursPerWeek { get; set; }
        public string? StartDateText { get; set; }
        public string? LocationLabel { get; set; }
        public string? RequirementsText { get; set; }
    }

    private sealed class ChatCompletionResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? Message { get; set; }
    }

    private sealed class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
