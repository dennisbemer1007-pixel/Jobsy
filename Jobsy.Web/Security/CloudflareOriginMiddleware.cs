using Jobsy.Core.Security;

namespace Jobsy.Web.Security;

/// <summary>
/// When <c>CLOUDFLARE_ORIGIN_SECRET</c> is set, requires that shared secret header
/// (Cloudflare Transform Rule). Skips Render health probes (<c>/healthz</c>, <c>/health</c>).
/// Empty secret skips enforcement (bootstrap until configured).
/// </summary>
public sealed class CloudflareOriginMiddleware
{
    public const string HeaderName = CloudflareOriginSecret.HeaderName;
    public const string ConfigKey = "CLOUDFLARE_ORIGIN_SECRET";
    public const string ConfigKeyAlt = "Cloudflare:OriginSecret";
    public const string UnsetInProductionMessage =
        "CRITICAL: CLOUDFLARE_ORIGIN_SECRET is unset in Production; " +
        "origin-header enforcement is disabled until the secret is configured.";
    public const string InvalidInProductionMessage =
        "CRITICAL: CLOUDFLARE_ORIGIN_SECRET is set but not a single-line header value; " +
        "origin-header enforcement is disabled until it is replaced.";

    private readonly RequestDelegate _next;
    private readonly byte[]? _expected;
    private readonly bool _enforce;

    public CloudflareOriginMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<CloudflareOriginMiddleware>? logger = null)
    {
        _next = next;
        var raw = configuration[ConfigKey] ?? configuration[ConfigKeyAlt];
        var secret = CloudflareOriginSecret.Normalize(raw);
        if (secret is not null)
        {
            _expected = System.Text.Encoding.UTF8.GetBytes(secret);
            _enforce = true;
        }
        else
        {
            // Same bootstrap as API: do not crash Acceptatie/Production when the Dashboard
            // secret is still empty (sync:false). Enforcement activates once configured.
            if (environment.IsProduction())
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    logger?.LogCritical(UnsetInProductionMessage);
                    Console.Error.WriteLine(UnsetInProductionMessage);
                }
                else
                {
                    logger?.LogCritical(InvalidInProductionMessage);
                    Console.Error.WriteLine(InvalidInProductionMessage);
                }
            }

            _expected = null;
            _enforce = false;
        }
    }

    /// <summary>Render health checks hit the service directly and cannot send the Cloudflare header.</summary>
    public static bool IsHealthProbe(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.Equals("/healthz", StringComparison.OrdinalIgnoreCase)
               || value.Equals("/healthz/", StringComparison.OrdinalIgnoreCase)
               || value.Equals("/health", StringComparison.OrdinalIgnoreCase)
               || value.Equals("/health/", StringComparison.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var presentedValidSecret = IsValidOrigin(context);
        if (_enforce && !IsHealthProbe(context.Request.Path) && !presentedValidSecret)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(
                "Deze pagina is alleen bereikbaar via Cloudflare.",
                context.RequestAborted);
            return;
        }

        // Render terminates TLS; Kestrel sees HTTP. Without https Scheme, Secure auth
        // cookies (CookieSecurePolicy.Always) fail to clear/round-trip and /home↔/login
        // loops (ERR_TOO_MANY_REDIRECTS). Apply Forwarded-Proto even when origin
        // enforcement is off (bootstrap / unset CLOUDFLARE_ORIGIN_SECRET).
        ApplyForwardedHttps(context);

        // Only a request which passed the origin-secret check may supply the Cloudflare
        // client address. Health probes must not be able to spoof it.
        if (presentedValidSecret
            && System.Net.IPAddress.TryParse(
                context.Request.Headers["CF-Connecting-IP"].ToString(),
                out var clientIp))
        {
            context.Connection.RemoteIpAddress = clientIp;
        }

        await _next(context);
    }

    internal static void ApplyForwardedHttps(HttpContext context)
    {
        var raw = context.Request.Headers["X-Forwarded-Proto"].ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        // CF + Render may send "https, https" — use the leftmost (original client) hop.
        var proto = raw.Split(',', 2)[0].Trim();
        if (string.Equals(proto, "https", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Scheme = Uri.UriSchemeHttps;
        }
    }

    private bool IsValidOrigin(HttpContext context)
    {
        if (_expected is null)
        {
            return false;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            return false;
        }

        var provided = values.ToString() ?? string.Empty;
        var expected = System.Text.Encoding.UTF8.GetString(_expected);
        return CloudflareOriginSecret.Matches(expected, provided);
    }
}
