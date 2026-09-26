namespace Jobsy.Web.Security;

/// <summary>
/// When <c>CLOUDFLARE_ORIGIN_SECRET</c> is set, requires that shared secret header
/// (Cloudflare Transform Rule). Empty secret skips enforcement (bootstrap until configured).
/// </summary>
public sealed class CloudflareOriginMiddleware
{
    public const string HeaderName = "X-Jobsy-Origin-Secret";
    public const string ConfigKey = "CLOUDFLARE_ORIGIN_SECRET";
    public const string ConfigKeyAlt = "Cloudflare:OriginSecret";

    private readonly RequestDelegate _next;
    private readonly byte[]? _expected;
    private readonly bool _enforce;

    public CloudflareOriginMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _next = next;
        var secret = configuration[ConfigKey] ?? configuration[ConfigKeyAlt];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            _expected = System.Text.Encoding.UTF8.GetBytes(secret.Trim());
            _enforce = true;
        }
        else
        {
            // Same bootstrap as API: do not crash Acceptatie/Production when the Dashboard
            // secret is still empty (sync:false). Enforcement activates once configured.
            if (environment.IsProduction())
            {
                Console.Error.WriteLine(
                    "CRITICAL: CLOUDFLARE_ORIGIN_SECRET is unset in Production; " +
                    "origin-header enforcement is disabled until the secret is configured.");
            }

            _expected = null;
            _enforce = false;
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_enforce && !IsValidOrigin(context))
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
        // client address. This keeps rate limits and audit data tied to the browser IP.
        if (_enforce
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
        var providedBytes = System.Text.Encoding.UTF8.GetBytes(provided);
        return providedBytes.Length == _expected.Length
               && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                   providedBytes, _expected);
    }
}
