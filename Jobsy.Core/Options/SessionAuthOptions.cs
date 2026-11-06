namespace Jobsy.Core.Options;

/// <summary>
/// Interactive session and "remember this device" policy (appsettings / Render env).
/// Platform admin can still lower inactivity via <c>SessionInactivityTimeoutMinutes</c>
/// unless <see cref="InactivityTimeoutMinutes"/> is set here (host override).
/// </summary>
public sealed class SessionAuthOptions
{
    public const string SectionName = "JobsyAuth:Session";

    /// <summary>Sliding inactivity window when no host override is set (minutes, clamped 5–480).</summary>
    public int InactivityTimeoutMinutes { get; set; } = SessionAuthDefaults.InactivityTimeoutMinutes;

    /// <summary>
    /// When &gt; 0, overrides the platform-features inactivity timeout from the database.
    /// Use on Acceptatie (e.g. 480) without migrating admin settings.
    /// </summary>
    public int InactivityTimeoutOverrideMinutes { get; set; }

    /// <summary>Absolute sliding ceiling for <c>Jobsy.Auth</c> (minutes, clamped 5–480).</summary>
    public int AuthCookieLifetimeMinutes { get; set; } = SessionAuthDefaults.AuthCookieLifetimeMinutes;

    /// <summary>Remember-device refresh token lifetime (days).</summary>
    public int RememberDeviceLifetimeDays { get; set; } = SessionAuthDefaults.RememberDeviceLifetimeDays;

    /// <summary>HMAC local-session claim lifetime refreshed by session-activity (minutes).</summary>
    public int LocalSessionLifetimeMinutes { get; set; } = SessionAuthDefaults.LocalSessionLifetimeMinutes;
}

public static class SessionAuthDefaults
{
    public const int InactivityTimeoutMinutes = 480;
    public const int AuthCookieLifetimeMinutes = 480;
    public const int RememberDeviceLifetimeDays = 30;
    public const int LocalSessionLifetimeMinutes = 480;
}
