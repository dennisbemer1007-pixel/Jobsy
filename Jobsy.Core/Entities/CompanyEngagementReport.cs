namespace Jobsy.Core.Entities;

/// <summary>
/// Candidate (or anonymous) report that a public engagement claim may be incorrect (09.5).
/// </summary>
public class CompanyEngagementReport
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public CompanyEngagementClaim Claim { get; set; } = null!;

    public Guid CompanyId { get; set; }
    public string ItemId { get; set; } = string.Empty;

    public string? ReporterEmail { get; set; }
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
