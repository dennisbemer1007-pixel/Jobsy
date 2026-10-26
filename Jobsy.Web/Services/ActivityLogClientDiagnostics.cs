using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jobsy.Web.Services;

internal static class ActivityLogClientDiagnostics
{
    internal static void LogOverviewFailure(
        ILogger logger,
        HttpStatusCode status,
        string? errorCode,
        Exception? ex = null)
    {
        var code = errorCode ?? HintForStatus(status);
        if (ex is null)
        {
            logger.LogWarning(
                "job_search_activities status={Status} code={Code} exception_type=none",
                (int)status,
                code);
            return;
        }

        logger.LogWarning(
            ex,
            "job_search_activities status={Status} code={Code} exception_type={ExceptionType}",
            (int)status,
            code,
            ex.GetType().Name);
    }

    internal static void LogPdfFailure(ILogger logger, HttpStatusCode status, string? errorCode)
    {
        logger.LogWarning(
            "job_search_activities_pdf status={Status} code={Code} exception_type=none",
            (int)status,
            errorCode ?? HintForStatus(status));
    }

    internal static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var media = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!media.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var typeEl))
            {
                var type = typeEl.GetString() ?? "";
                if (type.Contains("feature_disabled", StringComparison.OrdinalIgnoreCase))
                {
                    return "feature_disabled";
                }
            }

            if (root.TryGetProperty("message", out var messageEl))
            {
                var message = messageEl.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }

            if (root.TryGetProperty("detail", out var detailEl))
            {
                var detail = detailEl.GetString();
                if (!string.IsNullOrWhiteSpace(detail))
                {
                    return detail.Length > 120 ? detail[..120] : detail;
                }
            }
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }

    private static string HintForStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.NotFound => "feature_disabled_or_route",
        HttpStatusCode.Unauthorized => "auth",
        HttpStatusCode.Forbidden => "auth",
        HttpStatusCode.ServiceUnavailable => "activity_log_unavailable",
        _ => "upstream_error"
    };
}
