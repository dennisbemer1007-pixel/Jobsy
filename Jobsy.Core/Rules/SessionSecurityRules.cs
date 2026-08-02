namespace Jobsy.Core.Rules;

/// <summary>
/// Platform-wide interactive session inactivity limits (admin-configurable).
/// </summary>
public static class SessionSecurityRules
{
    public const int DefaultInactivityTimeoutMinutes = 30;

    public const int MinInactivityTimeoutMinutes = 5;

    /// <summary>Hard ceiling — also used as absolute cookie lifetime when sliding.</summary>
    public const int MaxInactivityTimeoutMinutes = 480;

    public static int ClampTimeoutMinutes(int minutes) =>
        Math.Clamp(
            minutes <= 0 ? DefaultInactivityTimeoutMinutes : minutes,
            MinInactivityTimeoutMinutes,
            MaxInactivityTimeoutMinutes);
}
