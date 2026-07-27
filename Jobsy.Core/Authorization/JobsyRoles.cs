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

    public static bool RequiresCompanyLink(UserRole role) => IsEmployer(role);
}
