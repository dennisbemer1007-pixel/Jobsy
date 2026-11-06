namespace Jobsy.Core.Options;

/// <summary>
/// Host auth policy toggles (appsettings). Production defaults keep MFA mandatory for everyone.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// When true, local-password sign-in and session enforcement skip MFA for <c>@jobsy.local</c> seed accounts.
    /// Intended for Acceptatie only; default false.
    /// </summary>
    public bool SkipMfaForSeedAccounts { get; set; }
}
