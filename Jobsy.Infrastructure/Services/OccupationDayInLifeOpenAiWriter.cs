using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Careers;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Fills one occupation day with OpenAI only. Mistral and <c>Ai:Provider</c> are not consulted.
/// </summary>
public sealed class OccupationDayInLifeOpenAiWriter : IOccupationDayInLifeWriter
{
    public const string HttpClientName = "OpenAIOccupationDay";
    public const string DefaultBaseUrl = "https://api.openai.com/v1/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationCredentialService _credentials;
    private readonly OpenAiOptions _openAi;
    private readonly OccupationDayInLifeOptions _options;
    private readonly ILogger<OccupationDayInLifeOpenAiWriter> _logger;

    public OccupationDayInLifeOpenAiWriter(
        IHttpClientFactory httpClientFactory,
        IIntegrationCredentialService credentials,
        IOptions<OpenAiOptions> openAi,
        IOptions<OccupationDayInLifeOptions> options,
        ILogger<OccupationDayInLifeOpenAiWriter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentials = credentials;
        _openAi = openAi.Value;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OccupationDayWriteResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _credentials.GetRawApiKeyAsync(IntegrationKey.OpenAI, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _openAi.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new OccupationDayWriteResult(false, null, OccupationDayWriteErrors.KeyMissing, ModelName());
        }

        var model = ModelName();
        var baseUrl = await ResolveBaseUrlAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var useSchema = true;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
            request.Content = JsonContent.Create(new
            {
                model,
                temperature = 0.2,
                response_format = ResponseFormat(useSchema),
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
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
                    "OpenAI dag-in-het-leven aanroep mislukt: {Reason}",
                    OccupationDayWriteErrors.SafeSnippet(reason));
                return new OccupationDayWriteResult(false, null, reason, model);
            }

            using (response)
            {
                if ((int)response.StatusCode == 429 && attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var detail = await ReadErrorDetailAsync(response, cancellationToken);
                    if (useSchema && (int)response.StatusCode == 400 && SchemaRejected(detail) && attempt < 3)
                    {
                        useSchema = false;
                        continue;
                    }

                    var reason = OccupationDayWriteErrors.FromStatus((int)response.StatusCode, detail);
                    _logger.LogWarning(
                        "OpenAI dag-in-het-leven gaf {StatusCode}: {Reason}",
                        (int)response.StatusCode,
                        OccupationDayWriteErrors.SafeSnippet(detail));
                    return new OccupationDayWriteResult(false, null, reason, model);
                }

                var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
                var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new OccupationDayWriteResult(false, null, OccupationDayWriteErrors.Empty, model);
                }

                return new OccupationDayWriteResult(true, content, null, model);
            }
        }

        return new OccupationDayWriteResult(false, null, OccupationDayWriteErrors.Http, model);
    }

    private static object ResponseFormat(bool schema)
    {
        if (!schema)
        {
            return new { type = "json_object" };
        }

        return new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "occupation_day",
                strict = true,
                schema = new
                {
                    type = "object",
                    additionalProperties = false,
                    required = new[] { "blocks", "highlights", "varies" },
                    properties = new
                    {
                        blocks = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                additionalProperties = false,
                                required = new[] { "key", "label", "text" },
                                properties = new
                                {
                                    key = new { type = "string", @enum = OccupationDayBlocks.Keys },
                                    label = new { type = "string" },
                                    text = new { type = "string" }
                                }
                            }
                        },
                        highlights = new
                        {
                            type = "array",
                            items = new { type = "string" }
                        },
                        varies = new { type = "string" }
                    }
                }
            }
        };
    }

    private static bool SchemaRejected(string? detail)
    {
        var text = detail ?? "";
        return text.Contains("json_schema", StringComparison.OrdinalIgnoreCase)
            || text.Contains("response_format", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadErrorDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > 2000)
            {
                body = body[..2000];
            }

            return OccupationDayWriteErrors.DetailFromBody(body);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return "";
        }
    }

    /// <summary>OpenAI host only. A Mistral base URL is replaced with the public OpenAI endpoint.</summary>
    public static string ForceOpenAiBaseUrl(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return DefaultBaseUrl;
        }

        if (!IntegrationEndpointUrl.TryNormalizeBaseUrl(configured, out var normalized, out _)
            || string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultBaseUrl;
        }

        if (normalized.Contains("mistral", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultBaseUrl;
        }

        return normalized;
    }

    private string ModelName()
    {
        var model = (_options.Model ?? "").Trim();
        if (model.Length == 0 || model.Contains("mistral", StringComparison.OrdinalIgnoreCase))
        {
            return OccupationDayInLifeOptions.DefaultModel;
        }

        return model;
    }

    private async Task<string> ResolveBaseUrlAsync(CancellationToken cancellationToken)
    {
        var fromDb = await _credentials.GetBaseUrlAsync(IntegrationKey.OpenAI, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return ForceOpenAiBaseUrl(fromDb);
        }

        return ForceOpenAiBaseUrl(_openAi.BaseUrl);
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
