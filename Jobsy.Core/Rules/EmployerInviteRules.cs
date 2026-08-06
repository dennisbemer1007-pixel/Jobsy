using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class EmployerInviteRules
{
    /// <summary>Higher number = more privileged. Caller may only invite strictly lower ranks.</summary>
    public static int Rank(UserRole role) => role switch
    {
        UserRole.Admin => 100,
        UserRole.EnterpriseManager => 40,
        UserRole.Intermediary => 30,
        UserRole.RegionalManager => 25,
        UserRole.BranchManager => 10,
        _ => 0
    };

    public static bool CanAssignRole(UserRole callerRole, UserRole targetRole)
    {
        if (!Jobsy.Core.Authorization.JobsyRoles.IsEmployer(targetRole))
        {
            return false;
        }

        if (callerRole == UserRole.Admin)
        {
            return true;
        }

        // Bedrijfsmanagers may invite only EM, Regional and Branch roles for their organization.
        if (callerRole == UserRole.EnterpriseManager)
        {
            return targetRole is UserRole.EnterpriseManager
                or UserRole.RegionalManager
                or UserRole.BranchManager;
        }

        // Intermediairs may invite only colleague intermediairs on the same organization.
        if (callerRole == UserRole.Intermediary)
        {
            return targetRole == UserRole.Intermediary;
        }

        return Rank(callerRole) > Rank(targetRole);
    }

    /// <summary>
    /// Existing employer may only be re-invited when all of their company links are within the caller's scope.
    /// </summary>
    public static bool IsWithinCallerScope(
        Guid? primaryCompanyId,
        IEnumerable<Guid> membershipCompanyIds,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool callerIsAdmin)
    {
        if (callerIsAdmin || accessibleCompanyIds is null)
        {
            return true;
        }

        if (primaryCompanyId is Guid primary && !accessibleCompanyIds.Contains(primary))
        {
            return false;
        }

        return membershipCompanyIds.All(accessibleCompanyIds.Contains);
    }
}
