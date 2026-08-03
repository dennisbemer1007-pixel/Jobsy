namespace Jobsy.Core.Enums;

/// <summary>
/// Platform roles. Claim values use the enum name (e.g. "BranchManager").
/// </summary>
public enum UserRole
{
    Candidate = 0,
    BranchManager = 1,
    RegionalManager = 2,
    EnterpriseManager = 3,
    Intermediary = 4,
    Admin = 5,
    SalesManager = 6,
    /// <summary>Candidate-acquisition partner with tiered commission and flyers.</summary>
    Ambassadeur = 7
}
