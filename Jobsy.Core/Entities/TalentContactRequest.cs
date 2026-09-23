using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Employer unlock of an anonymous talent-pool candidate (1 token).
/// Refundable when the candidate does not respond within 48h or declines as already placed.
/// </summary>
public class TalentContactRequest
{
    public const int ResponseWindowHours = 48;

    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid EmployerUserId { get; set; }
    public User EmployerUser { get; set; } = null!;

    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;

    public TalentContactStatus Status { get; set; } = TalentContactStatus.Pending;

    /// <summary>Secure message / invitation shown to the candidate.</summary>
    public string Message { get; set; } = string.Empty;

    public Guid? SpendTransactionId { get; set; }
    public TokenTransaction? SpendTransaction { get; set; }

    public Guid? RefundTransactionId { get; set; }
    public TokenTransaction? RefundTransaction { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime RespondByUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime? WithdrawnAtUtc { get; set; }
    public DateTime? ContactSharedAtUtc { get; set; }
}
