using Jobsy.Core.Enums;

namespace Jobsy.Core.Security;

public static class MfaPolicy
{
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
}
