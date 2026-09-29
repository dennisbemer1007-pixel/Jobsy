using Jobsy.Core.Enums;

namespace Jobsy.Core.Authorization;

/// <summary>
/// Role claim values aligned with <see cref="UserRole"/> (Microsoft Entra app roles / claim mapping).
/// </summary>
public static class JobsyRoles
{
    public const string Candidate = nameof(UserRole.Candidate);
    public const string BranchManager = nameof(UserRole.BranchManager);
    public const string RegionalManager = nameof(UserRole.RegionalManager);
    public const string EnterpriseManager = nameof(UserRole.EnterpriseManager);
    public const string Intermediary = nameof(UserRole.Intermediary);
    public const string Admin = nameof(UserRole.Admin);
    public const string SalesManager = nameof(UserRole.SalesManager);
    public const string Ambassadeur = nameof(UserRole.Ambassadeur);
    public const string SchoolAdmin = nameof(UserRole.SchoolAdmin);
    public const string Teacher = nameof(UserRole.Teacher);

    /// <summary>SchoolAdmin and Teacher — school staff (not employer).</summary>
    public const string SchoolStaff = $"{SchoolAdmin},{Teacher}";

    public static readonly string[] EmployerRoles =
    [
        BranchManager,
        RegionalManager,
        EnterpriseManager,
        Intermediary
    ];

    public static bool IsEmployer(UserRole role) =>
        role is UserRole.BranchManager
            or UserRole.RegionalManager
            or UserRole.EnterpriseManager
            or UserRole.Intermediary;

    public static bool IsSchoolStaff(UserRole role) =>
        role is UserRole.SchoolAdmin or UserRole.Teacher;

    /// <summary>
    /// Branch, enterprise, intermediary and admin may create/publish/highlight/pushbom/extend/deactivate.
    /// Regional managers have a read-only vacancy view.
    /// </summary>
    public static bool CanManageVacancyLifecycle(UserRole role) =>
        role is UserRole.BranchManager
            or UserRole.EnterpriseManager
            or UserRole.Intermediary
            or UserRole.Admin;

    /// <summary>Roles allowed to mutate vacancy lifecycle (API Authorize attribute).</summary>
    public const string VacancyLifecycleRoles =
        $"{BranchManager},{EnterpriseManager},{Intermediary},{Admin}";

    /// <summary>Regional managers may view applications but not accept/reject.</summary>
    public static bool CanReactToApplications(UserRole role) =>
        role is UserRole.BranchManager
            or UserRole.EnterpriseManager
            or UserRole.Intermediary
            or UserRole.Admin;

    public const string ApplicationReactRoles =
        $"{BranchManager},{EnterpriseManager},{Intermediary},{Admin}";

    /// <summary>
    /// Branch managers may purchase only when the vestiging is not under enterprise token management.
    /// Enterprise managers buy into the organisation pot; intermediaries/admins always may purchase.
    /// </summary>
    public static bool CanPurchaseTokens(UserRole role) =>
        role is UserRole.BranchManager
            or UserRole.EnterpriseManager
            or UserRole.Intermediary
            or UserRole.Admin;

    public static bool CanAllocateTokens(UserRole role) =>
        role is UserRole.EnterpriseManager or UserRole.Admin;

    public const string TokenPurchaseRoles =
        $"{BranchManager},{EnterpriseManager},{Intermediary},{Admin}";

    public const string TokenAllocateRoles =
        $"{EnterpriseManager},{Admin}";

    /// <summary>
    /// Employer roles that may mutate talent unlocks, culture, onboarding checkout, etc.
    /// RegionalManager is read-only.
    /// </summary>
    public const string EmployerMutateRoles =
        $"{BranchManager},{EnterpriseManager},{Intermediary}";

    /// <summary>
    /// Same as <see cref="EmployerMutateRoles"/> plus Admin (e.g. supplier onboarding).
    /// RegionalManager is read-only.
    /// </summary>
    public const string EmployerMutateRolesWithAdmin =
        $"{BranchManager},{EnterpriseManager},{Intermediary},{Admin}";

    /// <summary>RegionalManager is read-only for employer data mutations.</summary>
    public static bool CanMutateEmployerData(UserRole role) =>
        role is UserRole.BranchManager
            or UserRole.EnterpriseManager
            or UserRole.Intermediary
            or UserRole.Admin;

}
