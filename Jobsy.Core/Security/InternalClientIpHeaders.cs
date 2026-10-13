namespace Jobsy.Core.Security;

/// <summary>
/// Shared Web→API headers so rate limits partition by real visitor IP, not the hop.
/// </summary>
public static class InternalClientIpHeaders
{
    public const string ClientIpHeader = "X-Jobsy-Client-Ip";
    public const string InternalSecretHeader = "X-Jobsy-Internal-Secret";
    public const string ConfigKey = "JobsyAuth:InternalClientIpSecret";
}
