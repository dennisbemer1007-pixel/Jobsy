namespace Jobsy.Core.Security;

public static class LoginLockoutRules
{
    public const int FailedAttemptsBeforeLockout = 5;
    public static readonly TimeSpan LockoutWindow = TimeSpan.FromHours(24);
    public static readonly TimeSpan MailCooldown = TimeSpan.FromHours(24);

    /// <summary>
    /// Duration for the N-th lockout in the 24 h window (1-based): 15, 30, 60, 120, then 240 min.
    /// </summary>
    public static TimeSpan LockoutDuration(int lockoutsInWindow)
    {
        if (lockoutsInWindow <= 0)
        {
            return TimeSpan.Zero;
        }

        var index = Math.Min(lockoutsInWindow, 5) - 1;
        return TimeSpan.FromMinutes(15 * Math.Pow(2, index));
    }
}
