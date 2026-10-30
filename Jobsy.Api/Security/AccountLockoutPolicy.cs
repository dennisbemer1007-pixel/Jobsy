using Jobsy.Core.Security;

namespace Jobsy.Api.Security;

/// <summary>
/// Configurable lockout thresholds for the API. Production keeps <see cref="LoginLockoutRules"/> defaults.
/// CI sets <c>JobsyAuth:DisableAccountLockout</c> so shared demo logins in Playwright are not locked.
/// </summary>
public sealed class AccountLockoutPolicy(IConfiguration configuration)
{
    public bool LockoutEnabled =>
        !configuration.GetValue("JobsyAuth:DisableAccountLockout", false);

    public int FailedAttemptsBeforeLockout
    {
        get
        {
            if (!LockoutEnabled)
            {
                return int.MaxValue;
            }

            var configured = configuration.GetValue<int?>("JobsyAuth:FailedAttemptsBeforeLockout");
            return configured is > 0
                ? configured.Value
                : LoginLockoutRules.FailedAttemptsBeforeLockout;
        }
    }
}
