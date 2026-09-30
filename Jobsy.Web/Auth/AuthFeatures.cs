namespace Jobsy.Web.Auth;

/// <summary>
/// Feature flags for auth UI that land across the stack.
/// <see cref="PasswordResetAvailable"/> stays false until file 05.
/// </summary>
public static class AuthFeatures
{
    /// <summary>When true, login/pause show "Wachtwoord vergeten?" / "Nieuw wachtwoord kiezen".</summary>
    public const bool PasswordResetAvailable = true;
}
