using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class CareerPathPlanGenerationService : ICareerPathPlanGenerationService
{
    public const string HttpClientName = "OpenAICareerPath";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _options;
    private readonly ILogger<CareerPathPlanGenerationService> _logger;

    public CareerPathPlanGenerationService(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> options,
        ILogger<CareerPathPlanGenerationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HorizonCareerPathPlan> GenerateAsync(
        string dreamTitle,
        HorizonCareerProfileSnapshot? profile = null,
        CancellationToken cancellationToken = default)
    {
        var local = HorizonCareerPathBuilder.BuildLocal(dreamTitle, profile);
        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return local;
        }

        try
        {
            var generated = await GenerateWithOpenAiAsync(
                dreamTitle,
                profile,
                apiKey,
                await ResolveModelAsync(cancellationToken),
                await ResolveBaseUrlAsync(cancellationToken),
                cancellationToken);
            if (generated is not null && generated.Steps.Count > 0)
            {
                return generated;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI carrièrepad mislukt; lokale gap-analyse wordt gebruikt.");
        }

        return local;
    }

    private async Task<HorizonCareerPathPlan?> GenerateWithOpenAiAsync(
        string dreamTitle,
        HorizonCareerProfileSnapshot? profile,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Stip op de horizon: {dreamTitle.Trim()}");
        sb.AppendLine("Geen vaste huidige functietitel — ga uit van het kandidaatprofiel/DNA.");
        if (profile?.StrengthHints is { Count: > 0 })
        {
            sb.AppendLine("Sterke punten uit DNA/profiel: " + string.Join(", ", profile.StrengthHints.Take(6)));
        }

        sb.AppendLine("Geef precies 4 stappen. Per stap: title, summary, skillsGap[], courses[], minRequirements[], yearsExperienceNeeded (int), actionLabel.");
        sb.AppendLine("Antwoord ALLEEN als JSON: {\"matchPercent\":number,\"matchSummary\":\"...\",\"steps\":[{\"title\":\"...\",\"summary\":\"...\",\"skillsGap\":[],\"courses\":[],\"minRequirements\":[],\"yearsExperienceNeeded\":0,\"actionLabel\":\"...\"}]}");

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
                        Je bent carrièrecoach van Lobsy (Den Haag / Westland). Schrijf in helder Nederlands (Jip-en-Janneke).
                        Bouw een diepgaand stappenplan van huidige DNA/profiel naar de stip op de horizon.
                        Geen hardcoded functietitel als "huidige rol". Geen jargon (RIASEC, OCEAN, DISC, Schwartz).
                        Per stap: skills gap, concrete cursussen/opleidingen, minimale eisen, jaren ervaring.
                        """
                },
                new { role = "user", content = sb.ToString() }
            }
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI carrièrepad gaf {StatusCode}.", (int)response.StatusCode);
            return null;
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        PlanDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PlanDto>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto?.Steps is null || dto.Steps.Count == 0)
        {
            return null;
        }

        var dream = string.IsNullOrWhiteSpace(dreamTitle) ? "Jouw droombaan" : dreamTitle.Trim();
        var query = Uri.EscapeDataString(dream);
        var steps = new List<HorizonCareerPathStep>();
        for (var i = 0; i < Math.Min(4, dto.Steps.Count); i++)
        {
            var row = dto.Steps[i];
            var status = i == 0
                ? HorizonCareerStepKind.Completed
                : i == 1
                    ? HorizonCareerStepKind.Active
                    : HorizonCareerStepKind.Open;
            var href = i switch
            {
                0 => "/candidate/profile",
                1 => "/candidate/profile?tab=fit",
                _ => "/?q=" + query
            };
            steps.Add(new HorizonCareerPathStep(
                $"ai-{i + 1}",
                i + 1,
                string.IsNullOrWhiteSpace(row.Title) ? $"Stap {i + 1}" : row.Title.Trim(),
                status,
                string.IsNullOrWhiteSpace(row.Summary) ? "Werk gericht aan deze stap." : row.Summary.Trim(),
                CleanList(row.SkillsGap),
                CleanList(row.Courses),
                CleanList(row.MinRequirements),
                Math.Clamp(row.YearsExperienceNeeded ?? 0, 0, 15),
                string.IsNullOrWhiteSpace(row.ActionLabel) ? "Verder" : row.ActionLabel.Trim(),
                href,
                i == 0 ? 100 : Math.Max(0, 55 - (i * 15))));
        }

        var match = Math.Clamp(dto.MatchPercent ?? 28, 15, 70);
        var summary = string.IsNullOrWhiteSpace(dto.MatchSummary)
            ? $"Pad naar “{dream}” op basis van je profiel en DNA."
            : dto.MatchSummary.Trim();
        return new HorizonCareerPathPlan(dream, match, summary, steps);
    }

    private static IReadOnlyList<string> CleanList(IReadOnlyList<string>? items)
        => (items ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

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
            : _options.BaseUrl.Trim();
        return fallback.EndsWith('/') ? fallback : fallback + "/";
    }

    private sealed class PlanDto
    {
        public int? MatchPercent { get; set; }
        public string? MatchSummary { get; set; }
        public List<StepDto> Steps { get; set; } = [];
    }

    private sealed class StepDto
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public List<string>? SkillsGap { get; set; }
        public List<string>? Courses { get; set; }
        public List<string>? MinRequirements { get; set; }
        public int? YearsExperienceNeeded { get; set; }
        public string? ActionLabel { get; set; }
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
