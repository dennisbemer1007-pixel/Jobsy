namespace Jobsy.Web.Auth;

/// <summary>Mapped from <c>/login?error=</c> (and <c>setup=done</c>) for <c>LoginStatusBlock</c>.</summary>
public enum LoginState
{
    None = 0,
    Invalid,
    Locked,
    TooMany,
    Unavailable,
    SessionExpired,
    MfaRequired,
    AdminProvider,
    Retry,
    SetupDone,
    EntraNotConfigured,
    EntraFailed,
    GoogleNotConfigured,
    GoogleFailed,
    UnknownProvider,
    Generic,
    AmbassadorsPaused
}

public static class LoginStateMapping
{
    public static LoginState FromQuery(string? errorCode, bool setupDone)
    {
        if (setupDone)
        {
            return LoginState.SetupDone;
        }

        return errorCode?.ToLowerInvariant() switch
        {
            "invalid" => LoginState.Invalid,
            "locked" => LoginState.Locked,
            "too-many" => LoginState.TooMany,
            "unavailable" => LoginState.Unavailable,
            "session-expired" => LoginState.SessionExpired,
            "mfa-required" => LoginState.MfaRequired,
            "admin-provider" => LoginState.AdminProvider,
            "retry" => LoginState.Retry,
            "entra-not-configured" => LoginState.EntraNotConfigured,
            "entra-failed" => LoginState.EntraFailed,
            "google-not-configured" => LoginState.GoogleNotConfigured,
            "google-failed" => LoginState.GoogleFailed,
            "unknown-provider" => LoginState.UnknownProvider,
            "ambassadors-paused" => LoginState.AmbassadorsPaused,
            { Length: > 0 } => LoginState.Generic,
            _ => LoginState.None
        };
    }
}
