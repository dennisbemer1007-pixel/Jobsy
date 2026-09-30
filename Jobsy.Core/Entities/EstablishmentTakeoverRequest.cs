using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Request to take over a vestiging that is already registered on Jobsy.
/// Ownership transfers (Kind = OwnershipTransfer) need a letter + admin (07.5).
/// </summary>
public class EstablishmentTakeoverRequest
{
    public Guid Id { get; set; }

    public Guid RegistrationId { get; set; }
    public CompanyRegistration Registration { get; set; } = null!;

    public Guid TargetCompanyId { get; set; }
    public Company TargetCompany { get; set; } = null!;

    public TakeoverRequestStatus Status { get; set; } = TakeoverRequestStatus.Pending;

    public TakeoverRequestKind Kind { get; set; } = TakeoverRequestKind.Colleague;

    public Guid? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }

    /// <summary>HMAC of the 8-char ownership-transfer letter code (never plain text).</summary>
    public string? LetterCodeHash { get; set; }

    public DateTime? LetterExpiresAtUtc { get; set; }
    public DateTime? LetterSentAtUtc { get; set; }
    public int LetterFailedAttempts { get; set; }
    public DateTime? LetterVerifiedAtUtc { get; set; }
    public DateTime? ManagersNotifiedAtUtc { get; set; }
}
