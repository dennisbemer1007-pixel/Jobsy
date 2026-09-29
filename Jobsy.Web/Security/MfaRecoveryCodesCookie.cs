namespace Jobsy.Web.Security;

/// <summary>One-time Data-Protection cookie that carries recovery codes from verify → SSR page.</summary>
public static class MfaRecoveryCodesCookie
{
    public const string Name = "Jobsy.MfaRecoveryCodes";
    public const string ProtectorPurpose = "Jobsy.MfaRecoveryCodes.v1";
}
