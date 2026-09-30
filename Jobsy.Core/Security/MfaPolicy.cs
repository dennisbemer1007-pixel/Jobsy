using Jobsy.Core.Enums;

namespace Jobsy.Core.Security;

public static class MfaPolicy
{
    public static bool IsRequired(UserRole role)
        => role == UserRole.Admin
           || role is UserRole.BranchManager
               or UserRole.RegionalManager
               or UserRole.EnterpriseManager
               or UserRole.Intermediary
           || role is UserRole.SchoolAdmin
               or UserRole.Teacher
           || role == UserRole.SalesManager
           // Ambassadeur is parked (AmbassadorsEnabled = false); 2FA stays required when the role is re-enabled.
           || role == UserRole.Ambassadeur;
}
