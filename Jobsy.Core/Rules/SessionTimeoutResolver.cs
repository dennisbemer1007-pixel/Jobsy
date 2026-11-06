using Jobsy.Core.Options;

namespace Jobsy.Core.Rules;

/// <summary>
/// Resolves effective inactivity timeout: host override, then platform value, then default.
/// </summary>
public static class SessionTimeoutResolver
{
    public static int ResolveInactivityMinutes(
        SessionAuthOptions? sessionOptions,
        int platformTimeoutMinutes)
    {
        if (sessionOptions?.InactivityTimeoutOverrideMinutes > 0)
        {
            return SessionSecurityRules.ClampTimeoutMinutes(sessionOptions.InactivityTimeoutOverrideMinutes);
        }

        if (platformTimeoutMinutes > 0)
        {
            return SessionSecurityRules.ClampTimeoutMinutes(platformTimeoutMinutes);
        }

        var configured = sessionOptions?.InactivityTimeoutMinutes
                         ?? SessionAuthDefaults.InactivityTimeoutMinutes;
        return SessionSecurityRules.ClampTimeoutMinutes(configured);
    }

    public static int ResolveAuthCookieLifetimeMinutes(SessionAuthOptions? sessionOptions)
        => SessionSecurityRules.ClampTimeoutMinutes(
            sessionOptions?.AuthCookieLifetimeMinutes ?? SessionAuthDefaults.AuthCookieLifetimeMinutes);

    public static TimeSpan ResolveRememberDeviceLifetime(SessionAuthOptions? sessionOptions)
    {
        var days = sessionOptions?.RememberDeviceLifetimeDays ?? SessionAuthDefaults.RememberDeviceLifetimeDays;
        days = Math.Clamp(days, 1, 90);
        return TimeSpan.FromDays(days);
    }

    public static TimeSpan ResolveLocalSessionLifetime(SessionAuthOptions? sessionOptions)
    {
        var minutes = sessionOptions?.LocalSessionLifetimeMinutes
                      ?? SessionAuthDefaults.LocalSessionLifetimeMinutes;
        minutes = SessionSecurityRules.ClampTimeoutMinutes(minutes);
        return TimeSpan.FromMinutes(minutes);
    }
}
