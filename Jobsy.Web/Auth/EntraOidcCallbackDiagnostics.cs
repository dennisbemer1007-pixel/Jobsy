using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jobsy.Web.Auth;

/// <summary>Request context for Entra OIDC callback failures (no PII, no secrets).</summary>
public static class EntraOidcCallbackDiagnostics
{
    public static void LogContext(ILogger logger, HttpContext http, string? authority, string? clientId, string? tenantId)
    {
        var hasCorrelation = http.Request.Cookies.Keys.Any(k =>
            k.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
        var hasNonce = http.Request.Cookies.Keys.Any(k =>
            k.StartsWith(".AspNetCore.OpenIdConnect.Nonce.", StringComparison.Ordinal));

        logger.LogWarning(
            "Entra callback context: method={Method}, scheme={Scheme}, host={Host}, forwardedProto={ForwardedProto}, " +
            "isHttps={IsHttps}, correlationCookie={CorrelationCookie}, nonceCookie={NonceCookie}, " +
            "authority={Authority}, tenant={TenantId}, clientIdSuffix={ClientIdSuffix}",
            http.Request.Method,
            http.Request.Scheme,
            http.Request.Host.Value,
            http.Request.Headers["X-Forwarded-Proto"].ToString(),
            http.Request.IsHttps,
            hasCorrelation,
            hasNonce,
            authority ?? "(unknown)",
            tenantId ?? "(unknown)",
            ClientIdSuffix(clientId));
    }

    private static string ClientIdSuffix(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return "(none)";
        }

        var trimmed = clientId.Trim();
        return trimmed.Length <= 8 ? trimmed : "…" + trimmed[^8..];
    }
}
