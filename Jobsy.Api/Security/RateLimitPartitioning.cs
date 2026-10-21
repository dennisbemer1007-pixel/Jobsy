using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Jobsy.Api.Extensions;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Localization;
using Jobsy.Core.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Security;

/// <summary>
/// Partition keys for public API rate limits. Prefer end-user identity over the
/// Web→API hop IP (which is shared for all Blazor Server / map-proxy traffic).
/// </summary>
public static class RateLimitPartitioning
{
    public const string ClientIpHeader = InternalClientIpHeaders.ClientIpHeader;
    public const string InternalSecretHeader = InternalClientIpHeaders.InternalSecretHeader;
    public const string ConfigKey = InternalClientIpHeaders.ConfigKey;
    public const string PartitionItemKey = "Jobsy.RateLimit.Partition";

    /// <summary>Machine-readable ProblemDetails code the Web client maps to a friendly message.</summary>
    public const string RateLimitCode = "rate_limited";

    public const string CandidateApplyRateLimitedCode = "candidate_apply_rate_limited";
    public const string CandidateApplyRateLimitedBurstCode = "candidate_apply_rate_limited_burst";
    public const string CandidateApplyRateLimitedDailyCode = "candidate_apply_rate_limited_daily";

    private static readonly JsonSerializerOptions ProblemJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Order: authenticated user id → trusted visitor IP (header + secret via
    /// FixedTimeEquals, or JWT <c>client_ip</c> claim) → RemoteIpAddress.
    /// </summary>
    public static string ResolvePartitionKey(HttpContext httpContext, string? internalSecret)
    {
        var userId = ResolveUserId(httpContext.User);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return Store(httpContext, "uid:" + userId);
        }

        if (TryReadTrustedClientIp(httpContext, internalSecret, out var trustedIp))
        {
            return Store(httpContext, "cip:" + trustedIp);
        }

        var clientIpClaim = httpContext.User.FindFirst(JobsyAccessToken.ClientIpClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(clientIpClaim))
        {
            return Store(httpContext, "cip:" + clientIpClaim.Trim());
        }

        var remote = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return Store(httpContext, "ip:" + remote);
    }

    public static bool TryReadTrustedClientIp(
        HttpContext httpContext,
        string? internalSecret,
        out string clientIp)
    {
        clientIp = string.Empty;
        if (string.IsNullOrWhiteSpace(internalSecret))
        {
            return false;
        }

        if (!httpContext.Request.Headers.TryGetValue(InternalSecretHeader, out var secretValues)
            || !httpContext.Request.Headers.TryGetValue(ClientIpHeader, out var ipValues))
        {
            return false;
        }

        var provided = secretValues.ToString() ?? string.Empty;
        var expectedBytes = Encoding.UTF8.GetBytes(internalSecret.Trim());
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        if (expectedBytes.Length == 0
            || providedBytes.Length != expectedBytes.Length
            || !CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            return false;
        }

        var ip = ipValues.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ip)
            || !System.Net.IPAddress.TryParse(ip, out _))
        {
            return false;
        }

        clientIp = ip;
        return true;
    }

    public static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        var partition = http.Items.TryGetValue(PartitionItemKey, out var stored)
            ? stored?.ToString() ?? "unknown"
            : "unknown";
        var route = http.Request.Path.Value ?? "/";

        var supportCode = SupportCodeGenerator.Create();

        var logger = http.RequestServices?
            .GetService<ILoggerFactory>()
            ?.CreateLogger("Jobsy.Api.RateLimiting");
        logger?.LogWarning(
            "Rate limit rejected {SupportCode} route={Route} partition={Partition} method={Method}",
            supportCode,
            route,
            partition,
            http.Request.Method);

        if (route.StartsWith("/api/assistant", StringComparison.OrdinalIgnoreCase))
        {
            var platformLog = http.RequestServices?.GetService<IPlatformErrorLog>();
            if (platformLog is not null)
            {
                try
                {
                    await platformLog.WriteAsync(
                        "ai.chat",
                        "Assistant chat was rate limited.",
                        supportCode,
                        detail: null,
                        Jobsy.Core.Enums.PlatformLogLevel.Warning,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Assistant rate-limit log failed.");
                }
            }
        }

        if (http.Response.HasStarted)
        {
            return;
        }

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        http.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        http.Response.ContentType = "application/problem+json; charset=utf-8";

        var isCandidateApply = route.Equals("/api/applications", StringComparison.OrdinalIgnoreCase)
                               && HttpMethods.IsPost(http.Request.Method);
        var applyWindow = isCandidateApply
                           && context.Lease.TryGetMetadata(TripleWindowRateLimiter.LimitWindowMetadata, out var windowObj)
                           && windowObj is string window
            ? window
            : null;

        // errors 04 §04.2: ProblemDetails with a machine-readable code the Web client maps to
        // ApiErrorException.RateLimited, plus the same LB-XXXX support code the error pages show.
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Title = "Too many requests",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = isCandidateApply
                ? CandidateApplyRateLimitDetail(http, applyWindow)
                : RetryDetail(http)
        };
        problem.Extensions["code"] = isCandidateApply
            ? CandidateApplyRateLimitCode(applyWindow)
            : RateLimitCode;
        problem.Extensions["retryAfterSeconds"] = retryAfterSeconds;
        problem.Extensions["supportCode"] = supportCode;

        await http.Response.WriteAsync(
            JsonSerializer.Serialize(problem, ProblemJson),
            cancellationToken);
    }

    private static string RetryDetail(HttpContext http)
    {
        var lang = JobsyLanguages.Normalize(http.GetJobsyLanguage());
        return lang switch
        {
            "en" => "Try again in a moment.",
            "pl" => "Spróbuj za chwilę jeszcze raz.",
            "ro" => "Mai încearcă peste puțin timp.",
            "ar" => "جرّب مرة أخرى بعد قليل.",
            _ => "Probeer het zo opnieuw."
        };
    }

    private static string CandidateApplyRateLimitCode(string? window)
        => window switch
        {
            "minute" => CandidateApplyRateLimitedBurstCode,
            "day" => CandidateApplyRateLimitedDailyCode,
            _ => CandidateApplyRateLimitedCode
        };

    private static string CandidateApplyRateLimitDetail(HttpContext http, string? window)
    {
        var lang = JobsyLanguages.Normalize(http.GetJobsyLanguage());
        return window switch
        {
            "minute" => lang switch
            {
                "en" => "You are applying very quickly. Wait a moment and try again.",
                "pl" => "Aplikujesz bardzo szybko. Odczekaj chwilę i spróbuj ponownie.",
                "ro" => "Aplici foarte repede. Așteaptă un moment și încearcă din nou.",
                "ar" => "تتقدم بسرعة كبيرة. انتظر لحظة ثم حاول مرة أخرى.",
                _ => "Je solliciteert erg snel achter elkaar. Wacht even en probeer het opnieuw."
            },
            "day" => lang switch
            {
                "en" => "You have applied to many vacancies today. Try again tomorrow.",
                "pl" => "Aplikowałeś/aś dziś na wiele ofert. Spróbuj jutro ponownie.",
                "ro" => "Ai aplicat la multe joburi astăzi. Încearcă din nou mâine.",
                "ar" => "تقدمت إلى عدد كبير من الوظائف اليوم. حاول مرة أخرى غدًا.",
                _ => "Je hebt vandaag al op veel vacatures gesolliciteerd. Probeer het morgen opnieuw."
            },
            _ => lang switch
            {
                "en" => "You have applied to many vacancies in a short time. Try again in about an hour.",
                "pl" => "Aplikowałeś/aś na wiele ofert w krótkim czasie. Spróbuj ponownie za około godzinę.",
                "ro" => "Ai aplicat la multe joburi într-un timp scurt. Încearcă din nou peste aproximativ o oră.",
                "ar" => "تقدمت إلى عدد كبير من الوظائف في وقت قصير. حاول مرة أخرى بعد حوالي ساعة.",
                _ => "Je hebt veel gesolliciteerd in korte tijd. Probeer het over een uurtje opnieuw."
            }
        };
    }

    public static string? ResolveUserId(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirst("sub")?.Value
               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static string Store(HttpContext httpContext, string key)
    {
        httpContext.Items[PartitionItemKey] = key;
        return key;
    }
}
