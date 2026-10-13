using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Employer-requested manual verification check (D16 / 06.5).</summary>
public class CompanyManualVerificationRequest
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Message { get; set; }

    /// <summary>Comma-separated attachment ids (existing upload service).</summary>
    public string? AttachmentIdsJson { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? DecidedAtUtc { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }

    public CompanyVerificationStatus? DecisionStatus { get; set; }
    public string? DecisionNote { get; set; }

    public bool IsOpen => DecidedAtUtc is null;
}
