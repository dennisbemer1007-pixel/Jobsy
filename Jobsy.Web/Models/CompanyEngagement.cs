namespace Jobsy.Web.Models;

public sealed class CompanyEngagementState
{
    public Guid CompanyId { get; set; }
    public Guid RootCompanyId { get; set; }
    public List<CompanyEngagementClaimEdit> Claims { get; set; } = [];
}

public sealed class CompanyEngagementClaimEdit
{
    public string ItemId { get; set; } = "";
    public string Status { get; set; } = "SelfDeclared";
    public string? ProofUrl { get; set; }
    public string? ProofText { get; set; }
    public string? CheckedSource { get; set; }
    public DateTime? CheckedAtUtc { get; set; }
    public DateTime? RemovedAtUtc { get; set; }
    public string? RemovedReason { get; set; }
}

public sealed class PublicEngagementBadge
{
    public string ItemId { get; set; } = "";
    public string Status { get; set; } = "SelfDeclared";
    public string? CheckedSource { get; set; }
    public string? ProofUrl { get; set; }
}

public sealed class AdminEngagementItem
{
    public Guid ClaimId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string? KvkNumber { get; set; }
    public string ItemId { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ProofUrl { get; set; }
    public string? ProofText { get; set; }
    public string? CheckedSource { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool InQueue { get; set; }
    public int OpenReportCount { get; set; }
    public string? RemovedReason { get; set; }
}
