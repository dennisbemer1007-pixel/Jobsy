namespace Jobsy.Api.Security;

/// <summary>
/// In Production, requires a shared secret header that Cloudflare injects via Transform Rule.
/// Skips <c>/health</c>. When the secret env var is empty outside Production, the check is skipped
/// (local/test). In Production an empty secret fails closed at startup.
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
