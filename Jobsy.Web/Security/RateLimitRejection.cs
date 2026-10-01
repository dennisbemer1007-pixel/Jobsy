using System.Text.Json;
using System.Threading.RateLimiting;
using Jobsy.Web.Diagnostics;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Jobsy.Web.Security;

/// <summary>
/// What a rate-limited visitor gets (errors 04 §04.2). HTML requests end up on the friendly
/// "Even rustig aan" page: the response stays an empty 429, which the HTML status-code
/// re-execute in <see cref="ErrorPagesExtensions.UseHtmlStatusCodePages"/> turns into
/// <c>/status/429</c>. Everything else gets ProblemDetails.
/// </summary>
public static class RateLimitRejection
{
    public const string RetryAfterItemsKey = "Jobsy.RateLimit.RetryAfter";

    public const string Code = "rate_limited";

    /// <summary>Fallback when the limiter has no <see cref="MetadataName.RetryAfter"/> metadata.</summary>
    public const int DefaultRetryAfterSeconds = 60;

    private static readonly JsonSerializerOptions ProblemJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static int RetryAfterSeconds(RateLimitLease lease)
        => lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
            : DefaultRetryAfterSeconds;

    public static async ValueTask WriteAsync(
        HttpContext http,
        int retryAfterSeconds,
        CancellationToken cancellationToken)
    {
        if (http.Response.HasStarted)
        {
            return;
        }

        var supportCode = SupportCode.GetOrCreate(http);
        http.Items[RetryAfterItemsKey] = retryAfterSeconds;

        Log(http, retryAfterSeconds, supportCode);

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        http.Response.Headers.RetryAfter = retryAfterSeconds.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        ErrorResponse.ApplyHeaders(http);

        if (ErrorPagesExtensions.WantsHtmlStatusPage(http))
        {
            // Body stays empty on purpose: UseHtmlStatusCodePages re-executes /status/429.
            return;
        }

        http.Response.ContentType = "application/problem+json; charset=utf-8";
        await http.Response.WriteAsync(
            JsonSerializer.Serialize(new RateLimitProblem
            {
                Status = StatusCodes.Status429TooManyRequests,
                Code = Code,
                RetryAfterSeconds = retryAfterSeconds,
                SupportCode = supportCode
            }, ProblemJson),
            cancellationToken);
    }

    /// <summary>Seconds the 429 page should show; the <c>Retry-After</c> value it was sent with.</summary>
    public static int RetryAfterFor(HttpContext? http)
        => http?.Items.TryGetValue(RetryAfterItemsKey, out var stored) == true && stored is int seconds
            ? seconds
            : DefaultRetryAfterSeconds;

    /// <summary>
    /// Logged: support code, the limiter policy, the route template and the wait. Never the
    /// visitor IP — the partition key is an IP for the Web limiters (E2).
    /// </summary>
    private static void Log(HttpContext http, int retryAfterSeconds, string supportCode)
    {
        var logger = http.RequestServices
            .GetService<ILoggerFactory>()
            ?.CreateLogger("Jobsy.Web.RateLimiting");

        logger?.LogWarning(
            "Rate limit rejected {SupportCode} policy={Policy} route={Route} retryAfter={RetryAfter}s",
            supportCode,
            PolicyName(http),
            RouteTemplate(http),
            retryAfterSeconds);
    }

    private static string PolicyName(HttpContext http)
    {
        var metadata = http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        return metadata?.PolicyName ?? "global";
    }

    private static string RouteTemplate(HttpContext http)
    {
        if (http.GetEndpoint() is RouteEndpoint { RoutePattern: RoutePattern pattern }
            && !string.IsNullOrEmpty(pattern.RawText))
        {
            return pattern.RawText;
        }

        return http.Request.Path.Value ?? "/";
    }

    /// <summary>Shape of the 429 body for non-HTML Web requests; mirrors the API contract (§IA).</summary>
    private sealed class RateLimitProblem
    {
        public string Type { get; init; } = "https://tools.ietf.org/html/rfc6585#section-4";

        public string Title { get; init; } = "Too many requests";

        public int Status { get; init; }

        public string Code { get; init; } = RateLimitRejection.Code;

        public int RetryAfterSeconds { get; init; }

        public string? SupportCode { get; init; }
    }
}
