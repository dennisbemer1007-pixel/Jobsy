using Jobsy.Core;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Jobsy.Web.Auth;

/// <summary>
/// Builds a stable HTTPS redirect_uri for Entra (Render terminates TLS; Kestrel may see http).
/// </summary>
public static class EntraOidcRedirectUri
{
    public static string Build(HttpContext http, IConfiguration configuration, string callbackPath)
    {
        var path = callbackPath.StartsWith('/') ? callbackPath : "/" + callbackPath;
        var configured = JobsyPublicUrl.NormalizeOrigin(configuration["PublicWebBaseUrl"]);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/') + path;
        }

        var scheme = ResolveScheme(http);
        var host = http.Request.Host;
        return $"{scheme}://{host.Value}{path}";
    }

    public static void ApplyToProtocolMessage(
        HttpContext http,
        IConfiguration configuration,
        OpenIdConnectMessage message,
        string callbackPath)
    {
        message.RedirectUri = Build(http, configuration, callbackPath);
    }

    internal static string ResolveScheme(HttpContext http)
    {
        if (http.Request.IsHttps)
        {
            return Uri.UriSchemeHttps;
        }

        var forwarded = http.Request.Headers["X-Forwarded-Proto"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var proto = forwarded.Split(',', 2)[0].Trim();
            if (string.Equals(proto, "https", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UriSchemeHttps;
            }
        }

        // CloudflareOriginMiddleware may not have run yet during early OIDC events; mirror its rule.
        if (JobsyCookie.ShouldMarkSecure(http))
        {
            return Uri.UriSchemeHttps;
        }

        return http.Request.Scheme;
    }
}
