using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Jobsy.Web.Diagnostics;

/// <summary>
/// Writes the one log line support needs to find an error back from a code (E2).
/// Logged: support code, request id, route template and user id. Never logged here:
/// query strings, form bodies, headers or the raw path when a route template exists.
/// </summary>
public static class ErrorTelemetry
{
    private const string LoggedItemsKey = "Jobsy.Error.Logged";

    public static void Record(
        ILogger logger,
        HttpContext? http,
        IExceptionHandlerPathFeature feature,
        string supportCode)
    {
        if (http is not null)
        {
            if (http.Items.ContainsKey(LoggedItemsKey))
            {
                return;
            }

            http.Items[LoggedItemsKey] = true;
        }

        TagSentry(supportCode);

        logger.LogError(
            feature.Error,
            "Unhandled error {SupportCode} {RequestId} {PathTemplate} {UserId}",
            supportCode,
            http?.TraceIdentifier,
            PathTemplate(feature),
            UserId(http));
    }

    /// <summary>Route pattern when the failing endpoint had one, else the path without its query.</summary>
    public static string PathTemplate(IExceptionHandlerPathFeature feature)
    {
        if (feature.Endpoint is RouteEndpoint { RoutePattern: RoutePattern pattern }
            && !string.IsNullOrEmpty(pattern.RawText))
        {
            return pattern.RawText;
        }

        var path = feature.Path ?? "/";
        var query = path.IndexOf('?');
        return query < 0 ? path : path[..query];
    }

    private static string? UserId(HttpContext? http)
        => http?.User?.FindFirst("sub")?.Value
           ?? http?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    private static void TagSentry(string supportCode)
    {
        try
        {
            SentrySdk.ConfigureScope(scope => scope.SetTag(SupportCode.SentryTag, supportCode));
        }
        catch (Exception)
        {
            // Sentry is optional (no DSN in Development / tests); never fail the error page over it.
        }
    }
}
