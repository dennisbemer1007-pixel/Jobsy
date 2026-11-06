using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancyExtractionService : IExternalVacancyExtractionService
{
    private const string SystemPrompt =
        """
        Je extraheert vacaturegegevens uit zichtbare paginatekst voor het Lobsy-platform.
        Antwoord ALLEEN als JSON met velden:
        title, company, place, hours, pay, start, training, requirementBullets (string array, B1 Nederlands).
        Gebruik lege string of lege array wanneer onbekend. Verzin geen feiten. Geef NOOIT de volledige brontekst terug.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly ILogger<ExternalVacancyExtractionService> _logger;

    public ExternalVacancyExtractionService(
        IHttpClientFactory httpClientFactory,
        IOpenAiEndpointResolver openAi,
        ILogger<ExternalVacancyExtractionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<ExternalVacancyExtractionResult?> ExtractAsync(
        string visibleText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(visibleText))
        {
            return null;
        }

        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.ExternalVacancyExtraction, cancellationToken);
        if (string.IsNullOrWhiteSpace(endpoint.ApiKey))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("IntegrationProbe");
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(endpoint.BaseUrl, UriKind.Absolute), "chat/completions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.ApiKey);
            request.Content = JsonContent.Create(new
            {
                model = endpoint.Model,
                temperature = 0,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = visibleText }
                }
            });

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var parsed = JsonSerializer.Deserialize<ExtractDto>(content, JsonOptions);
            if (parsed is null)
            {
                return null;
            }

            return new ExternalVacancyExtractionResult(
                parsed.Title ?? "",
                parsed.Company ?? "",
                parsed.Place ?? "",
                parsed.Hours ?? "",
                parsed.Pay ?? "",
                parsed.Start ?? "",
                parsed.Training ?? "",
                parsed.RequirementBullets?.Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.Trim()).ToList()
                ?? []);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "External vacancy extraction failed.");
            return null;
        }
    }

    internal static ExternalVacancyExtractionResult? ParseJsonContent(string json)
    {
        var parsed = JsonSerializer.Deserialize<ExtractDto>(json, JsonOptions);
        if (parsed is null)
        {
            return null;
        }

        return new ExternalVacancyExtractionResult(
            parsed.Title ?? "",
            parsed.Company ?? "",
            parsed.Place ?? "",
            parsed.Hours ?? "",
            parsed.Pay ?? "",
            parsed.Start ?? "",
            parsed.Training ?? "",
            parsed.RequirementBullets?.Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.Trim()).ToList()
            ?? []);
    }

    private sealed class ExtractDto
    {
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? Place { get; set; }
        public string? Hours { get; set; }
        public string? Pay { get; set; }
        public string? Start { get; set; }
        public string? Training { get; set; }
        public List<string>? RequirementBullets { get; set; }
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
        public string? Content { get; set; }
    }
}
