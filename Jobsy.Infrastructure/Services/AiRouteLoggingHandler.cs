using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Logs the AI endpoint host and model id for each outbound call. The prompt stays out of the log.
/// </summary>
public sealed class AiRouteLoggingHandler : DelegatingHandler
{
    private readonly ILogger<AiRouteLoggingHandler> _logger;

    public AiRouteLoggingHandler(ILogger<AiRouteLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (AiCallRoute.IsAiHost(request.RequestUri))
        {
            var modelId = await AiCallRoute.TryReadModelIdAsync(request.Content, cancellationToken);
            _logger.LogInformation(
                "AI call host {Host} model {ModelId}",
                request.RequestUri!.Host,
                modelId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>Host and model id taken from an outbound AI request, without the prompt.</summary>
public static class AiCallRoute
{
    public static bool IsAiHost(Uri? uri)
    {
        var host = uri?.IdnHost;
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        return host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".openai.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("api.mistral.ai", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".mistral.ai", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> TryReadModelIdAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            return "-";
        }

        try
        {
            await content.LoadIntoBufferAsync(cancellationToken);
            var json = await content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("model", out var model)
                && model.ValueKind == JsonValueKind.String)
            {
                var id = model.GetString()?.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    return "-";
                }

                return id.Length <= 80 ? id : id[..80];
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return "-";
        }

        return "-";
    }
}
