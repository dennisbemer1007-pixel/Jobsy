using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Admin approve/reject audit row for company verification (D16).</summary>
public class CompanyVerificationDecision
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string KvkNumber { get; set; } = string.Empty;

    public Guid AdminUserId { get; set; }
    public User? AdminUser { get; set; }

    /// <summary>Verified or Rejected.</summary>
    public CompanyVerificationStatus Outcome { get; set; }

    public CompanyVerificationMethod Method { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
