using Jobsy.Core.Enums;

namespace Jobsy.Core.Security;

public static class MfaPolicy
{
    public static bool IsRequired(UserRole role)
        => role == UserRole.Admin
           || role is UserRole.BranchManager
               or UserRole.RegionalManager
               or UserRole.EnterpriseManager
               or UserRole.Intermediary;
}
