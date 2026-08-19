using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class CvExtractionService : ICvExtractionService
{
    private const string SystemPrompt =
        """
        Je extraheert kandidaatgegevens uit een CV voor het Nederlandse Lobsy-platform.
        Vul ALLEEN velden die echt duidelijk in de tekst staan. Verzin niets, leid niets af.
        Onduidelijk, vaag of tegenstrijdig → null of lege lijst.
        Antwoord ALLEEN als JSON-object met exact deze velden:
        {
          "firstName": "string of null",
          "lastName": "string of null",
          "phoneNumber": "string of null",
          "aboutMe": "korte NL samenvatting in 1-3 zinnen of null",
          "drivingLicenses": ["B","AM", "..."],
          "educations": ["MBO","Havo", "..."],
          "roles": ["horeca","retail", "..."],
          "employers": [{"employerName":"","role":null,"years":null,"description":null,"startMonth":null,"endMonth":null}],
          "certificates": [{"name":"","year":null}]
        }
        drivingLicenses alleen bekende codes (AM,A1,A2,A,B,BE,C,CE,D,DE).
        educations alleen niveaus zoals VMBO, HAVO, VWO, MBO, HBO, WO.
        roles korte branches (horeca, retail, logistiek, zorg, administratie, …).
        startMonth/endMonth als yyyy-MM als dat duidelijk is, anders null.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<CvExtractionService> _logger;

    public CvExtractionService(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<CvExtractionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CvExtractedProfile> ExtractAsync(string cvText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cvText))
        {
            return new CvExtractedProfile();
        }

        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new CvExtractedProfile();
        }

        try
        {
            var model = await ResolveModelAsync(cancellationToken);
            var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
            var parsed = await ExtractWithOpenAiAsync(cvText, apiKey, model, baseUrl, cancellationToken);
            return parsed ?? new CvExtractedProfile();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI CV-extractie mislukt; profiel blijft ongewijzigd.");
            return new CvExtractedProfile();
        }
    }

    private async Task<CvExtractedProfile?> ExtractWithOpenAiAsync(
        string cvText,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("IntegrationProbe");
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
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = cvText }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "OpenAI CV-extractie gaf {StatusCode}: {Body}",
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

        var parsed = JsonSerializer.Deserialize<ExtractedJson>(content, JsonOptions);
        if (parsed is null)
        {
            return null;
        }

        return new CvExtractedProfile(
            FirstName: EmptyToNull(parsed.FirstName),
            LastName: EmptyToNull(parsed.LastName),
            PhoneNumber: EmptyToNull(parsed.PhoneNumber),
            AboutMe: EmptyToNull(parsed.AboutMe),
            DrivingLicenses: CleanList(parsed.DrivingLicenses),
            Educations: CleanList(parsed.Educations),
            Roles: CleanList(parsed.Roles),
            Employers: parsed.Employers?
                .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName))
                .Select(e => new CandidateEmployerHistoryDto(
                    e.EmployerName!.Trim(),
                    EmptyToNull(e.Role),
                    e.Years is >= 0 and <= 80 ? e.Years : null,
                    EmptyToNull(e.Description),
                    LobsyCvModelFactory.NormalizeMonth(e.StartMonth),
                    LobsyCvModelFactory.NormalizeMonth(e.EndMonth)))
                .Take(12)
                .ToList(),
            Certificates: parsed.Certificates?
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => new CandidateCertificateDto(
                    c.Name!.Trim(),
                    c.Year is >= 1950 and <= 2100 ? c.Year : null))
                .Take(20)
                .ToList());
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

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<string>? CleanList(IReadOnlyList<string>? items)
    {
        if (items is null || items.Count == 0)
        {
            return null;
        }

        var list = items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
        return list.Count == 0 ? null : list;
    }

    private sealed class ExtractedJson
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AboutMe { get; set; }
        public List<string>? DrivingLicenses { get; set; }
        public List<string>? Educations { get; set; }
        public List<string>? Roles { get; set; }
        public List<EmployerJson>? Employers { get; set; }
        public List<CertificateJson>? Certificates { get; set; }
    }

    private sealed class EmployerJson
    {
        public string? EmployerName { get; set; }
        public string? Role { get; set; }
        public int? Years { get; set; }
        public string? Description { get; set; }
        public string? StartMonth { get; set; }
        public string? EndMonth { get; set; }
    }

    private sealed class CertificateJson
    {
        public string? Name { get; set; }
        public int? Year { get; set; }
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
