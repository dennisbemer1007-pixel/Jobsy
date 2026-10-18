using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class OpenAiTestReflectionAiService : ITestReflectionAiService
{
    public const string HttpClientName = "OpenAITestReflection";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly ILogger<OpenAiTestReflectionAiService> _logger;

    public OpenAiTestReflectionAiService(
        IHttpClientFactory httpClientFactory,
        IOpenAiEndpointResolver openAi,
        ILogger<OpenAiTestReflectionAiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<TestReflectionFull?> TryRewriteAsync(
        TestReflectionFull template,
        CandidateFactSheet sheet,
        CancellationToken cancellationToken = default)
    {
        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.TestReflection, cancellationToken);
        if (string.IsNullOrWhiteSpace(endpoint.ApiKey))
        {
            return null;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            return await GenerateAsync(template, sheet, endpoint.ApiKey, endpoint.Model, endpoint.BaseUrl, cts.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI testreflectie mislukt; template blijft staan.");
            return null;
        }
    }

    private async Task<TestReflectionFull?> GenerateAsync(
        TestReflectionFull template,
        CandidateFactSheet sheet,
        string apiKey,
        string model,
        string baseUrl,
        CancellationToken ct)
    {
        var user = new StringBuilder();
        user.AppendLine(sheet.ToPrompt());
        user.AppendLine("Template (alle feiten hieruit behouden, alleen warmer herschrijven):");
        user.AppendLine(TestReflectionJson.Serialize(template));
        user.AppendLine($"Taal: {template.Language}");

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.35,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = TestReflectionPrompt.System + "\n" + CandidateFactGuard.StrictAddendum },
                new { role = "user", content = user.ToString() }
            }
        });

        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            LogFailure(response.StatusCode.ToString(), body);
            return null;
        }

        var parsed = JsonSerializer.Deserialize<OpenAiChatResponse>(body, JsonOptions);
        var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var dto = JsonSerializer.Deserialize<TestReflectionAiDto>(content, JsonOptions);
        if (dto is null)
        {
            return null;
        }

        var merged = Merge(template, dto);
        var guardText = TestReflectionJson.FlattenForGuard(merged);
        if (CandidateFactGuard.RejectionReason(guardText, sheet) is not null)
        {
            return null;
        }

        return merged with { FromOpenAi = true };
    }

    private static TestReflectionFull Merge(TestReflectionFull template, TestReflectionAiDto dto)
        => template with
        {
            HeroTitle = Pick(dto.HeroTitle, template.HeroTitle),
            HeroSubtitle = Pick(dto.HeroSubtitle, template.HeroSubtitle),
            BadgeLabel = Pick(dto.BadgeLabel, template.BadgeLabel),
            Themes = MergeThemes(dto.Themes, template.Themes),
            PracticeTitle = Pick(dto.PracticeTitle, template.PracticeTitle),
            PracticeExamples = PickList(dto.PracticeExamples, template.PracticeExamples),
            WatchTitle = Pick(dto.WatchTitle, template.WatchTitle),
            WatchPoints = PickList(dto.WatchPoints, template.WatchPoints),
            EnergyGives = PickList(dto.EnergyGives, template.EnergyGives),
            EnergyDrains = PickList(dto.EnergyDrains, template.EnergyDrains),
            EnvironmentTitle = Pick(dto.EnvironmentTitle, template.EnvironmentTitle),
            EnvironmentBody = Pick(dto.EnvironmentBody, template.EnvironmentBody),
            DreamCallout = string.IsNullOrWhiteSpace(dto.DreamCallout) ? template.DreamCallout : dto.DreamCallout.Trim()
        };

    private static IReadOnlyList<TestReflectionTheme> MergeThemes(
        IReadOnlyList<TestReflectionTheme>? ai,
        IReadOnlyList<TestReflectionTheme> template)
    {
        if (ai is null || ai.Count == 0)
        {
            return template;
        }

        var list = new List<TestReflectionTheme>();
        for (var i = 0; i < Math.Min(3, Math.Max(ai.Count, template.Count)); i++)
        {
            var t = i < template.Count ? template[i] : template[^1];
            var a = i < ai.Count ? ai[i] : t;
            list.Add(new TestReflectionTheme(Pick(a.Title, t.Title), Pick(a.Summary, t.Summary)));
        }

        return list;
    }

    private static string Pick(string? ai, string fallback)
        => string.IsNullOrWhiteSpace(ai) ? fallback : ai.Trim();

    private static IReadOnlyList<string> PickList(IReadOnlyList<string>? ai, IReadOnlyList<string> fallback)
    {
        if (ai is null || ai.Count == 0)
        {
            return fallback;
        }

        return ai.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Take(3).ToList();
    }

    private void LogFailure(string status, string body)
        => _logger.LogWarning("OpenAI testreflectie HTTP {Status}: {Body}", status, body.Length > 500 ? body[..500] : body);

    private sealed class OpenAiChatResponse
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

    private sealed class TestReflectionAiDto
    {
        public string? HeroTitle { get; set; }
        public string? HeroSubtitle { get; set; }
        public string? BadgeLabel { get; set; }
        public List<TestReflectionTheme>? Themes { get; set; }
        public string? PracticeTitle { get; set; }
        public List<string>? PracticeExamples { get; set; }
        public string? WatchTitle { get; set; }
        public List<string>? WatchPoints { get; set; }
        public List<string>? EnergyGives { get; set; }
        public List<string>? EnergyDrains { get; set; }
        public string? EnvironmentTitle { get; set; }
        public string? EnvironmentBody { get; set; }
        public string? DreamCallout { get; set; }
    }
}
