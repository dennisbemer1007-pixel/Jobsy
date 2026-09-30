namespace Jobsy.Core.Entities;

/// <summary>
/// Employer-declared maatschappelijke betrokkenheid claim on the root organisation (D13 / 09).
/// Vestigingen display the organisation's claims.
/// </summary>
public class CompanyEngagementClaim
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>Stable id from <see cref="Rules.EngagementCatalog"/>.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Optional https proof URL (≤ 500).</summary>
    public string? ProofUrl { get; set; }

    /// <summary>Optional short plain-text proof (≤ 300).</summary>
    public string? ProofText { get; set; }

    /// <summary><c>SelfDeclared</c> / <c>Checked</c> / <c>Removed</c>.</summary>
    public string Status { get; set; } = Rules.CompanyEngagementStatuses.SelfDeclared;

    /// <summary><c>Admin</c> or <c>Sbb</c> when Checked.</summary>
    public string? CheckedSource { get; set; }

    public Guid? CheckedByUserId { get; set; }
    public User? CheckedByUser { get; set; }
    public DateTime? CheckedAtUtc { get; set; }

    public string? RemovedReason { get; set; }
    public DateTime? RemovedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
