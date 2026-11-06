namespace Jobsy.Core.Security;

/// <summary>
/// Shared Web→API headers so rate limits partition by real visitor IP, not the hop.
/// </summary>
public static class InternalClientIpHeaders
{
    public const string ClientIpHeader = "X-Jobsy-Client-Ip";
    public const string InternalSecretHeader = "X-Jobsy-Internal-Secret";
    /// <summary>
    /// Web→API: signed-in staff/candidate user id so rate limits partition per user, not visitor IP.
    /// Only honored together with <see cref="InternalSecretHeader"/>.
    /// </summary>
    public const string RateLimitUserIdHeader = "X-Jobsy-RateLimit-User-Id";
    public const string ConfigKey = "JobsyAuth:InternalClientIpSecret";
}
