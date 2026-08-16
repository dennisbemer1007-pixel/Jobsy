using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class CursorCloudAgentClient : ICursorCloudAgentClient
{
    public const string HttpClientName = "CursorCloud";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptions<CursorCloudOptions> _options;
    private readonly ILogger<CursorCloudAgentClient> _logger;

    public CursorCloudAgentClient(
        IHttpClientFactory httpFactory,
        IOptions<CursorCloudOptions> options,
        ILogger<CursorCloudAgentClient> logger)
    {
        _httpFactory = httpFactory;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Value.ApiKey)
        && !string.IsNullOrWhiteSpace(_options.Value.Repository);

    public async Task<CursorAgentLaunchResult> LaunchAsync(
        CursorAgentLaunchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Cursor Cloud is niet geconfigureerd (ApiKey + Repository).");
        }

        var options = _options.Value;
        var images = request.Images?
            .Where(i => !string.IsNullOrWhiteSpace(i.Base64Data))
            .Select(i => new CursorLaunchImageDto(
                i.Base64Data,
                i.Width is int w && i.Height is int h
                    ? new CursorLaunchDimensionDto(w, h)
                    : null))
            .ToList();

        var body = new CursorLaunchRequestDto(
            new CursorLaunchPromptDto(request.Prompt, images is { Count: > 0 } ? images : null),
            string.IsNullOrWhiteSpace(options.Model) ? null : options.Model,
            new CursorLaunchSourceDto(options.Repository!.Trim(), string.IsNullOrWhiteSpace(options.Ref) ? "main" : options.Ref.Trim()),
            new CursorLaunchTargetDto(true, request.BranchName),
            TryWebhook(options));

        using var client = CreateClient();
        using var response = await client.PostAsync(
            "v0/agents",
            new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json"),
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Cursor launch failed: {Status} {Body}", (int)response.StatusCode, Truncate(payload));
            throw new InvalidOperationException(
                $"Cursor kon de taak niet starten ({(int)response.StatusCode}).");
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
        var root = doc.RootElement;
        var id = ReadString(root, "id")
                 ?? throw new InvalidOperationException("Cursor gaf geen agent-id terug.");
        return new CursorAgentLaunchResult(
            id,
            ReadString(root, "status"),
            ReadNestedString(root, "target", "url"));
    }

    public async Task<CursorAgentStatusResult?> GetAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(agentId))
        {
            return null;
        }

        using var client = CreateClient();
        using var response = await client.GetAsync(
            "v0/agents/" + Uri.EscapeDataString(agentId.Trim()),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Cursor agent poll failed: {Status} {Id}", (int)response.StatusCode, agentId);
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
        var root = doc.RootElement;
        var id = ReadString(root, "id") ?? agentId;
        return new CursorAgentStatusResult(
            id,
            ReadString(root, "status") ?? "UNKNOWN",
            ReadNestedString(root, "target", "prUrl") ?? ReadNestedString(root, "target", "prURL"),
            ReadNestedString(root, "target", "branchName"),
            ReadString(root, "summary"));
    }

    private HttpClient CreateClient()
    {
        var client = _httpFactory.CreateClient(HttpClientName);
        var key = _options.Value.ApiKey!.Trim();
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes(key + ":"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        return client;
    }

    private static CursorLaunchWebhookDto? TryWebhook(CursorCloudOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookUrl))
        {
            return null;
        }

        return new CursorLaunchWebhookDto(
            options.WebhookUrl.Trim(),
            string.IsNullOrWhiteSpace(options.WebhookSecret) ? null : options.WebhookSecret.Trim());
    }

    private static string? ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static string? ReadNestedString(JsonElement root, string parent, string name)
    {
        if (!root.TryGetProperty(parent, out var obj) || obj.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(obj, name);
    }

    private static string Truncate(string value)
        => value.Length <= 240 ? value : value[..240];

    private sealed record CursorLaunchRequestDto(
        CursorLaunchPromptDto Prompt,
        string? Model,
        CursorLaunchSourceDto Source,
        CursorLaunchTargetDto Target,
        CursorLaunchWebhookDto? Webhook);

    private sealed record CursorLaunchPromptDto(
        string Text,
        IReadOnlyList<CursorLaunchImageDto>? Images);

    private sealed record CursorLaunchImageDto(
        string Data,
        CursorLaunchDimensionDto? Dimension);

    private sealed record CursorLaunchDimensionDto(int Width, int Height);

    private sealed record CursorLaunchSourceDto(string Repository, string Ref);

    private sealed record CursorLaunchTargetDto(
        [property: JsonPropertyName("autoCreatePr")] bool AutoCreatePr,
        string BranchName);

    private sealed record CursorLaunchWebhookDto(string Url, string? Secret);
}
