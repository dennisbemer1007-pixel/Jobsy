namespace Jobsy.Web.Security;

/// <summary>
/// In Production, requires a shared secret header that Cloudflare injects via Transform Rule.
/// Skips static health-ish probes on <c>/</c> HEAD only when needed; enforces on all other paths.
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
        if (environment.IsProduction())
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    "CLOUDFLARE_ORIGIN_SECRET (or Cloudflare:OriginSecret) is required in Production.");
            }

            _expected = System.Text.Encoding.UTF8.GetBytes(secret.Trim());
            _enforce = true;
        }
        else if (!string.IsNullOrWhiteSpace(secret))
        {
            _expected = System.Text.Encoding.UTF8.GetBytes(secret.Trim());
            _enforce = true;
        }
        else
        {
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

        // Only a request which passed the origin-secret check may supply the Cloudflare
        // client address. This keeps rate limits and audit data tied to the browser IP.
        if (_enforce)
        {
            if (System.Net.IPAddress.TryParse(
                    context.Request.Headers["CF-Connecting-IP"].ToString(),
                    out var clientIp))
            {
                context.Connection.RemoteIpAddress = clientIp;
            }

            if (string.Equals(
                    context.Request.Headers["X-Forwarded-Proto"].ToString(),
                    "https",
                    StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Scheme = Uri.UriSchemeHttps;
            }
        }

        await _next(context);
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
