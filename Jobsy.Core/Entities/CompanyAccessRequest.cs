using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Request from someone whose company is already on Lobsy to join as a colleague (07.2).
/// Never exposes manager PII to the requester.
/// </summary>
public class CompanyAccessRequest
{
    public Guid Id { get; set; }

    public Guid TargetCompanyId { get; set; }
    public Company TargetCompany { get; set; } = null!;

    /// <summary>Normalized 8-digit KvK number.</summary>
    public string KvkNumber { get; set; } = string.Empty;

    /// <summary>JSON array of requested vestiging company ids (and/or KvK establishment ids).</summary>
    public string RequestedVestigingIdsJson { get; set; } = "[]";

    public UserRole RequestedRole { get; set; } = UserRole.BranchManager;

    public string RequesterName { get; set; } = string.Empty;
    public string? RequesterFunction { get; set; }
    public string RequesterEmail { get; set; } = string.Empty;
    public string? RequesterPhone { get; set; }
    public string? Message { get; set; }

    public string? EmailConfirmationCodeHash { get; set; }
    public DateTime? EmailConfirmationExpiresAtUtc { get; set; }
    public int EmailConfirmationFailedAttempts { get; set; }
    public DateTime? EmailConfirmedAtUtc { get; set; }

    /// <summary>Opaque token for withdraw / confirm links in e-mail.</summary>
    public string RequesterToken { get; set; } = string.Empty;

    public CompanyAccessRequestStatus Status { get; set; } = CompanyAccessRequestStatus.AwaitingEmail;

    public Guid? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? DecisionReason { get; set; }

    /// <summary>Role actually granted (may be lowered by the deciding manager).</summary>
    public UserRole? GrantedRole { get; set; }

    public string? GrantedVestigingIdsJson { get; set; }

    public DateTime? ReminderSentAtUtc { get; set; }
    public DateTime? EscalatedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
