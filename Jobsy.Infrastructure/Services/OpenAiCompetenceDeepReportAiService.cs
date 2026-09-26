using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Generates the personal three-sentence summary and three-step action plan for the paid
/// competence deep-analysis report via OpenAI. Mirrors <see cref="CareerPathPlanGenerationService"/>'s
/// credential resolution and HTTP setup. Returns null (never throws to the caller) when no API
/// key is configured, the request fails, or the response cannot be parsed, so callers always
/// fall back to <see cref="CompetenceDeepReportTexts"/> template copy.
/// </summary>
public sealed class OpenAiCompetenceDeepReportAiService : ICompetenceDeepReportAiService
{
    public const string HttpClientName = "OpenAICompetenceDeepReport";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiCompetenceDeepReportAiService> _logger;

    public OpenAiCompetenceDeepReportAiService(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiCompetenceDeepReportAiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(string Summary, IReadOnlyList<(string Title, string Body)> Steps)?> TryGenerateAsync(
        CompetenceDeepReport draftWithoutAi, string? jobTitle, CancellationToken ct)
    {
        var apiKey = await ResolveApiKeyAsync(ct);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            return await GenerateWithOpenAiAsync(
                draftWithoutAi,
                jobTitle,
                apiKey,
                await ResolveModelAsync(ct),
                await ResolveBaseUrlAsync(ct),
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI competentierapport mislukt; template-tekst wordt gebruikt.");
            return null;
        }
    }

    private async Task<(string Summary, IReadOnlyList<(string Title, string Body)> Steps)?> GenerateWithOpenAiAsync(
        CompetenceDeepReport draft,
        string? jobTitle,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Persoonlijkheidsprofiel (Big Five, 0-100, hoog naar laag):");
        foreach (var trait in draft.Traits.OrderByDescending(t => t.Score))
        {
            sb.AppendLine($"- {trait.LabelNl}: {trait.Score}/100 ({trait.Level}). {trait.Meaning}");
        }

        if (!string.IsNullOrWhiteSpace(jobTitle))
        {
            sb.AppendLine($"Gewenste functie/richting: {jobTitle.Trim()}");
        }

        if (draft.Occupations.Count > 0)
        {
            sb.AppendLine("Passende beroepen: " + string.Join(", ", draft.Occupations.Select(o => o.Title)));
        }

        sb.AppendLine(
            "Schrijf een korte, persoonlijke samenvatting van dit profiel in helder Nederlands (B1-niveau, spreek de lezer aan met 'je'), exact 3 zinnen.");
        sb.AppendLine(
            "Geef daarna precies 3 concrete actiestappen die de lezer deze week kan zetten, gebaseerd op dit profiel. Per stap: title (kort) en body (1-2 zinnen).");
        sb.AppendLine(
            "Antwoord ALLEEN als JSON: {\"summary\":\"...\",\"steps\":[{\"title\":\"...\",\"body\":\"...\"}]}");

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.4,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                        Je bent een loopbaancoach van Lobsy. Schrijf in helder, positief maar eerlijk Nederlands
                        op B1-niveau en spreek de lezer aan met 'je'. Geen jargon zoals OCEAN, Big Five, RIASEC,
                        DISC. Blijf dicht bij het gegeven profiel; verzin geen feiten die er niet in staan.
                        """
                },
                new { role = "user", content = sb.ToString() }
            }
        });

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI competentierapport gaf {StatusCode}.", (int)response.StatusCode);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, ct);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        ReportDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ReportDto>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.Summary) || dto.Steps is not { Count: > 0 })
        {
            return null;
        }

        var steps = dto.Steps
            .Where(s => !string.IsNullOrWhiteSpace(s.Title) && !string.IsNullOrWhiteSpace(s.Body))
            .Take(3)
            .Select(s => (s.Title!.Trim(), s.Body!.Trim()))
            .ToList();
        if (steps.Count == 0)
        {
            return null;
        }

        return (dto.Summary.Trim(), steps);
    }

    private async Task<string?> ResolveApiKeyAsync(CancellationToken ct)
    {
        var fromDb = await _credentials.GetRawApiKeyAsync(IntegrationKey.OpenAI, ct);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.ApiKey) ? null : _options.ApiKey.Trim();
    }

    private async Task<string> ResolveModelAsync(CancellationToken ct)
    {
        var fromDb = await _credentials.GetModelAsync(IntegrationKey.OpenAI, ct);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        return string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model.Trim();
    }

    private async Task<string> ResolveBaseUrlAsync(CancellationToken ct)
    {
        var fromDb = await _credentials.GetBaseUrlAsync(IntegrationKey.OpenAI, ct);
        if (!string.IsNullOrWhiteSpace(fromDb)
            && IntegrationEndpointUrl.TryNormalizeBaseUrl(fromDb, out var normalized, out _)
            && !string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var fallback = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.openai.com/v1/"
            : _options.BaseUrl.Trim();
        return fallback.EndsWith('/') ? fallback : fallback + "/";
    }

    private sealed class ReportDto
    {
        public string? Summary { get; set; }
        public List<StepDto>? Steps { get; set; }
    }

    private sealed class StepDto
    {
        public string? Title { get; set; }
        public string? Body { get; set; }
    }

    private sealed class ChatCompletionResponse
    {
        public List<ChoiceDto>? Choices { get; set; }
    }

    private sealed class ChoiceDto
    {
        public MessageDto? Message { get; set; }
    }

    private sealed class MessageDto
    {
        public string? Content { get; set; }
    }
}
