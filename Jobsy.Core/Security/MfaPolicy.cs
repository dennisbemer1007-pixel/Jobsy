using Jobsy.Core.Enums;

namespace Jobsy.Core.Security;

public static class MfaPolicy
{
    public const string JobsyLocalSeedEmailSuffix = "@jobsy.local";

    /// <summary>
    /// Demo / seed accounts created by <c>DemoUsersSeeder</c> and related fixtures.
    /// </summary>
    public static bool IsJobsyLocalSeedEmail(string? email)
        => !string.IsNullOrWhiteSpace(email)
           && email.Trim().EndsWith(JobsyLocalSeedEmailSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Acceptatie-only bypass when <see cref="Options.AuthOptions.SkipMfaForSeedAccounts"/> is enabled.
    /// </summary>
    public static bool ShouldSkipMandatoryMfaForSeedAccount(string? email, bool skipMfaForSeedAccounts)
        => skipMfaForSeedAccounts && IsJobsyLocalSeedEmail(email);

    /// <summary>
    /// Local-password MFA is required for privileged roles. External IdP logins skip Lobsy 2FA.
    /// Ambassadeur is intentionally out (Dennis D5).
    /// </summary>
    public static bool IsRequired(UserRole role)
        => role is UserRole.Admin
            or UserRole.BranchManager
            or UserRole.RegionalManager
            or UserRole.EnterpriseManager
            or UserRole.Intermediary
            or UserRole.SchoolAdmin
            or UserRole.Teacher
            or UserRole.SalesManager;

    /// <summary>
    /// Same as <see cref="IsRequired"/>, but test accounts skip MFA only while the
    /// acceptatie test-accounts runtime guard is active.
    /// </summary>
    public static bool IsRequiredFor(UserRole role, bool isTestAccount, bool testAccountsActive)
        => !(isTestAccount && testAccountsActive) && IsRequired(role);

    /// <summary>
    /// Whether a local-password sign-in must enter the MFA challenge (enroll or verify).
    /// System admin accounts follow <paramref name="systemAdminRequireTwoFactor"/> only.
    /// </summary>
    public static bool IsLocalPasswordMfaStepRequired(
        bool isSystemAdminAccount,
        UserRole role,
        bool isTestAccount,
        bool testAccountsActive,
        bool authenticatorEnabled,
        bool systemAdminRequireTwoFactor)
    {
        if (isSystemAdminAccount)
        {
            return systemAdminRequireTwoFactor;
        }

        return authenticatorEnabled || IsRequiredFor(role, isTestAccount, testAccountsActive);
    }
}
