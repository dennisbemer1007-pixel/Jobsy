using Jobsy.Core.Security;

namespace Jobsy.Api.Security;

/// <summary>
/// When <c>CLOUDFLARE_ORIGIN_SECRET</c> is set, requires that shared secret header
/// (Cloudflare Transform Rule, or the web service on server-side API calls).
/// Skips <c>/health</c> (Render) and inbound callbacks that target the Render URL
/// directly (Mollie, Cursor). Empty secret skips enforcement.
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
            // Render leaves CLOUDFLARE_ORIGIN_SECRET as sync:false. Prefer enforcement when set;
            // boot without it so Acceptatie/onrender.com still work before Transform Rules exist.
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

    /// <summary>
    /// Mollie and Cursor call <c>PublicApiBaseUrl</c> (the Render onrender.com URL), not Cloudflare.
    /// Those routes authenticate on their own (Mollie pull / HMAC).
    /// </summary>
    public static bool IsPlatformCallback(PathString path)
        => path.StartsWithSegments("/api/webhooks", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/feedback/cursor-webhook", StringComparison.OrdinalIgnoreCase);

    public static bool IsHealthProbe(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.Equals("/health", StringComparison.OrdinalIgnoreCase)
               || value.Equals("/health/", StringComparison.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var presentedValidSecret = IsValidOrigin(context);
        if (_enforce
            && !IsHealthProbe(context.Request.Path)
            && !IsPlatformCallback(context.Request.Path)
            && !presentedValidSecret)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(
                """{"title":"Verboden","detail":"Verzoek moet via Cloudflare binnenkomen."}""",
                context.RequestAborted);
            return;
        }

        // Keep Scheme=https behind Render/CF even when origin enforcement is off so
        // cookie Secure policies and absolute redirects stay correct.
        ApplyForwardedHttps(context);

        // CF-Connecting-IP is meaningful only after the origin secret was checked.
        // Exempt probes (health, webhooks) must not be able to spoof it.
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
