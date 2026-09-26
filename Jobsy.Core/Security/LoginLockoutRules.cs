namespace Jobsy.Core.Security;

public static class LoginLockoutRules
{
    public const int FailedAttemptsBeforeLockout = 5;

    public static TimeSpan LockoutDuration(int failedLoginCount)
    {
        if (failedLoginCount < FailedAttemptsBeforeLockout)
        {
            return TimeSpan.Zero;
        }

        var escalation = Math.Min(failedLoginCount - FailedAttemptsBeforeLockout, 4);
        return TimeSpan.FromMinutes(15 * Math.Pow(2, escalation));
    }
}
