using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Careers;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// One-shot translation through the shared translation model route (<see cref="OpenAiFeature.Translation"/>).
/// The candidate read path does not use this type.
/// </summary>
public sealed class OccupationDayInLifeTranslator : IOccupationDayInLifeTranslator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenAiEndpointResolver _openAi;
    private readonly ILogger<OccupationDayInLifeTranslator> _logger;

    public OccupationDayInLifeTranslator(
        IHttpClientFactory httpClientFactory,
        IOpenAiEndpointResolver openAi,
        ILogger<OccupationDayInLifeTranslator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<OccupationDayTranslateResult> TranslateAsync(
        OccupationDayDraft source,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var target = JobsyLanguages.Normalize(targetLanguage);
        if (JobsyLanguages.AreSame(target, JobsyLanguages.Default)
            || !OccupationDayTranslations.TargetLanguages.Contains(target, StringComparer.OrdinalIgnoreCase))
        {
            return new OccupationDayTranslateResult(false, null, "taal", "", false);
        }

        var endpoint = await _openAi.ResolveAsync(OpenAiFeature.Translation, cancellationToken);
        if (endpoint.Unavailable || string.IsNullOrWhiteSpace(endpoint.ApiKey))
        {
            return new OccupationDayTranslateResult(false, null, OccupationDayWriteErrors.KeyMissing, endpoint.Model, true);
        }

        var model = string.IsNullOrWhiteSpace(endpoint.Model) ? "gpt-4o-mini" : endpoint.Model;
        var payload = JsonSerializer.Serialize(new
        {
            title = source.TitleNl,
            morning = source.Morning,
            midday = source.Midday,
            afternoon = source.Afternoon,
            closing = source.Closing,
            highlights = source.Highlights,
            blocks = (source.Blocks ?? []).Select(block => new { key = block.Key, label = block.Label, text = block.Text }),
            tasks = source.Tasks ?? [],
            skills = source.Skills ?? [],
            varies = source.VariesNote
        });

        var client = _httpClientFactory.CreateClient("IntegrationProbe");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(endpoint.BaseUrl, UriKind.Absolute), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.ApiKey.Trim());
        request.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = OccupationDayTranslationPrompt.System(target) },
                new { role = "user", content = payload }
            }
        });

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            var reason = ex is OperationCanceledException
                ? OccupationDayWriteErrors.Timeout
                : OccupationDayWriteErrors.FromStatus(0, ex.Message);
            _logger.LogWarning(
                "Vertaling dag-in-het-leven naar {Language} mislukt: {Reason}",
                target,
                OccupationDayWriteErrors.SafeSnippet(reason));
            return new OccupationDayTranslateResult(false, null, reason, model, false);
        }

        using (response)
        {
        if (!response.IsSuccessStatusCode)
        {
            var detail = "";
            try
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                detail = OccupationDayWriteErrors.DetailFromBody(body.Length > 2000 ? body[..2000] : body);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                detail = "";
            }

            var reason = OccupationDayWriteErrors.FromStatus((int)response.StatusCode, detail);
            _logger.LogWarning(
                "Vertaling dag-in-het-leven gaf {StatusCode} naar {Language}: {Reason}",
                (int)response.StatusCode,
                target,
                OccupationDayWriteErrors.SafeSnippet(detail));
            return new OccupationDayTranslateResult(false, null, reason, model, false);
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (!OccupationDayInLifeJson.TryParse(content, SourceFacts(source), out var parsed, out var error))
        {
            return new OccupationDayTranslateResult(false, null, error ?? OccupationDayWriteErrors.Empty, model, false);
        }

        var title = ReadTitle(content);
        var draft = string.IsNullOrWhiteSpace(title) ? parsed : parsed with { TitleNl = title };
        if (draft.Tasks is not { Count: > 0 })
        {
            draft = draft with { Tasks = source.Tasks ?? [] };
        }

        if (draft.Skills is not { Count: > 0 })
        {
            draft = draft with { Skills = source.Skills ?? [] };
        }

        return new OccupationDayTranslateResult(true, draft, null, model, false);
        }
    }

    private static string ReadTitle(string? json)
    {
        var payload = (json ?? "").Trim();
        var open = payload.IndexOf('{');
        var close = payload.LastIndexOf('}');
        if (open < 0 || close <= open)
        {
            return "";
        }

        try
        {
            using var doc = JsonDocument.Parse(payload[open..(close + 1)]);
            return doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
                ? (title.GetString() ?? "").Trim()
                : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static OccupationDayFacts SourceFacts(OccupationDayDraft source)
        => OccupationDayFacts.Create(
            "translation",
            "http://data.europa.eu/esco/occupation/translation",
            source.TitleNl,
            source.Morning,
            source.Highlights,
            []);

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
