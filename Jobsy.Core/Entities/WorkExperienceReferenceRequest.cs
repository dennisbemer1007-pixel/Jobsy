namespace Jobsy.Core.Entities;

/// <summary>
/// A candidate asks a referent to confirm one work-experience entry and optionally endorse competences.
/// Referent contact fields are wiped on profile reset and account deletion.
/// </summary>
public class WorkExperienceReferenceRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Stable id on the employer-history row in preferences JSON.</summary>
    public Guid WorkExperienceEntryId { get; set; }

    public string ReferentName { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;
    public string ReferentEmail { get; set; } = string.Empty;

    public string Status { get; set; } = WorkExperienceReferenceStatus.Pending;

    public string EmployerName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string? StartMonth { get; set; }
    public string? EndMonth { get; set; }
    public bool IsCurrent { get; set; }
    public string? DutiesText { get; set; }

    public bool? ConfirmedEmployer { get; set; }
    public bool? ConfirmedPeriod { get; set; }
    public bool? ConfirmedRole { get; set; }
    public bool? ConfirmedDuties { get; set; }
    public string? ReferentComment { get; set; }
    public bool UnknownPersonReported { get; set; }

    /// <summary>JSON array of competence dimension codes endorsed by the referent.</summary>
    public string? EndorsedCompetenceCodesJson { get; set; }

    public DateTime CandidateConsentAtUtc { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Candidate removed the verification from their profile.</summary>
    public bool RemovedByCandidate { get; set; }
}

public static class WorkExperienceReferenceStatus
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Declined = "declined";
    public const string Revoked = "revoked";
}

public class WorkExperienceReferenceToken
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public WorkExperienceReferenceRequest Request { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? ReminderSentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
