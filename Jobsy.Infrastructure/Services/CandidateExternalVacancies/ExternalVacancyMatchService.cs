using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancyMatchService : IExternalVacancyMatchService
{
    private const string SystemPrompt =
        """
        Je vergelijkt een kandidaat met een externe vacature. Antwoord ALLEEN JSON:
        {"strengths":["..."],"challenges":["..."]}
        Maximaal 3 korte regels per lijst, B1 Nederlands. Gebruik alleen feiten uit de feitenlijst.
        Onbekend = "Dat weet ik niet".
        """;

    private readonly JobsyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly ILogger<ExternalVacancyMatchService> _logger;

    public ExternalVacancyMatchService(
        JobsyDbContext db,
        IHttpClientFactory httpClientFactory,
        IOpenAiEndpointResolver openAi,
        ILogger<ExternalVacancyMatchService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<ExternalVacancyMatchInsightsDto> BuildMatchInsightsAsync(
        Guid candidateUserId,
        IReadOnlyDictionary<string, string> structuredFacts,
        CancellationToken cancellationToken = default)
    {
        var sheet = await BuildFactSheetAsync(candidateUserId, cancellationToken);
        var fallback = CandidateExternalVacancyRules.SanitizeMatchInsights(
            ["Dat weet ik niet"],
            ["Dat weet ik niet"],
            sheet);

        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.ExternalVacancyExtraction, cancellationToken);
        if (string.IsNullOrWhiteSpace(endpoint.ApiKey))
        {
            return fallback;
        }

        try
        {
            var factLines = structuredFacts.Select(kv => $"- {kv.Key}: {kv.Value}");
            var user = $"{sheet.ToPrompt()}\n\nVacature-feiten:\n{string.Join('\n', factLines)}";
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
                    new { role = "system", content = SystemPrompt + "\n" + CandidateFactGuard.StrictAddendum },
                    new { role = "user", content = user }
                }
            });

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return fallback;
            }

            var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return fallback;
            }

            using var doc = JsonDocument.Parse(content);
            var strengths = ReadArray(doc.RootElement, "strengths");
            var challenges = ReadArray(doc.RootElement, "challenges");
            return CandidateExternalVacancyRules.SanitizeMatchInsights(strengths, challenges, sheet);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "External vacancy match insights failed.");
            return fallback;
        }
    }

    private async Task<CandidateFactSheet> BuildFactSheetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var competency = await _db.CandidateCompetencies.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var culture = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var career = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var scores = new List<string>();
        if (competency is not null && CandidateCompetencyStatuses.IsCompleted(competency.Status))
        {
            scores.Add("Competenties beschikbaar");
        }

        if (culture is not null && CandidateCompetencyStatuses.IsCompleted(culture.Status))
        {
            scores.Add("Cultuurprofiel beschikbaar");
        }

        return CandidateFactSheet.Personal([], [], [], scores: scores, checkJobTitles: false);
    }

    private static List<string> ReadArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return el.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString() ?? "")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
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
