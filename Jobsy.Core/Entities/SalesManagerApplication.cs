namespace Jobsy.Core.Entities;

/// <summary>
/// Recommendation from an active (tier-0) salesmanager for a new salesmanager candidate.
/// Admin must approve before an account is provisioned. One recruitment tier only.
/// </summary>
public class SalesManagerApplication
{
    public Guid Id { get; set; }

    public Guid ReferrerSalesManagerUserId { get; set; }
    public User ReferrerSalesManagerUser { get; set; } = null!;

    /// <summary>Tracking code of the referrer at submission time (audit).</summary>
    public string ReferrerTrackingCode { get; set; } = string.Empty;

    public string CandidateEmail { get; set; } = string.Empty;
    public string CandidateFullName { get; set; } = string.Empty;

    /// <summary>Short motivation / recommendation text from the referrer.</summary>
    public string Motivation { get; set; } = string.Empty;

    public SalesManagerApplicationStatus Status { get; set; } = SalesManagerApplicationStatus.Pending;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByAdminUserId { get; set; }
    public User? ReviewedByAdminUser { get; set; }

    /// <summary>User provisioned on approval (if any).</summary>
    public Guid? ProvisionedUserId { get; set; }
    public User? ProvisionedUser { get; set; }

    public string? RejectionReason { get; set; }
}

public enum SalesManagerApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
