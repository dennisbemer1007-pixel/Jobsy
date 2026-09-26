namespace Jobsy.Api.Security;

/// <summary>
/// When <c>CLOUDFLARE_ORIGIN_SECRET</c> is set, requires that shared secret header
/// (Cloudflare Transform Rule). Skips <c>/health</c>. Empty secret skips enforcement
/// (local/test and Production bootstrap until the Dashboard value is set).
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
            // Render leaves CLOUDFLARE_ORIGIN_SECRET as sync:false. Prefer enforcement when set;
            // boot without it so Acceptatie/onrender.com still work before Transform Rules exist.
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
        if (_enforce
            && !context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            && !IsValidOrigin(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(
                """{"title":"Verboden","detail":"Verzoek moet via Cloudflare binnenkomen."}""",
                context.RequestAborted);
            return;
        }

        // CF-Connecting-IP is meaningful only after the origin secret was checked.  Do not
        // accept X-Forwarded-For from arbitrary clients or trust every reverse proxy.
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
