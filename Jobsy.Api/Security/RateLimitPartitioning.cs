using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Jobsy.Core.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Security;

/// <summary>
/// Partition keys for public API rate limits. Prefer end-user identity over the
/// Web→API hop IP (which is shared for all Blazor Server circuits).
/// </summary>
public static class RateLimitPartitioning
{
    public const string ClientIpHeader = "X-Jobsy-Client-Ip";
    public const string InternalSecretHeader = "X-Jobsy-Internal-Secret";
    public const string ConfigKey = "JobsyAuth:InternalClientIpSecret";
    public const string PartitionItemKey = "Jobsy.RateLimit.Partition";

    /// <summary>
    /// Order: JWT <c>client_ip</c> claim → authenticated user id → trusted
    /// <see cref="ClientIpHeader"/> (secret via FixedTimeEquals) → RemoteIpAddress.
    /// </summary>
    public static string ResolvePartitionKey(HttpContext httpContext, string? internalSecret)
    {
        var clientIpClaim = httpContext.User.FindFirst(JobsyAccessToken.ClientIpClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(clientIpClaim))
        {
            return Store(httpContext, "cip:" + clientIpClaim.Trim());
        }

        var userId = ResolveUserId(httpContext.User);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return Store(httpContext, "uid:" + userId);
        }

        if (TryReadTrustedClientIp(httpContext, internalSecret, out var trustedIp))
        {
            return Store(httpContext, "cip:" + trustedIp);
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

        var logger = http.RequestServices
            .GetService<ILoggerFactory>()
            ?.CreateLogger("Jobsy.Api.RateLimiting");
        logger?.LogWarning(
            "Rate limit rejected route={Route} partition={Partition} method={Method}",
            route,
            partition,
            http.Request.Method);

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        http.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        http.Response.ContentType = "application/json; charset=utf-8";
        await http.Response.WriteAsync(
            """{"title":"Te veel verzoeken","detail":"Probeer het zo opnieuw."}""",
            cancellationToken);
    }

    private static string? ResolveUserId(ClaimsPrincipal user)
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
