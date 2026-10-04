namespace Jobsy.Core.Entities;

/// <summary>
/// A referee's answers about one reference. Facts the candidate asked for.
/// Nothing here is produced by a model.
/// </summary>
public class ReferenceConfirmation
{
    public Guid Id { get; set; }
    public Guid CandidateReferenceId { get; set; }
    public CandidateReference CandidateReference { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Job the candidate says they did. The candidate typed this.</summary>
    public string RoleTitle { get; set; } = string.Empty;

    public string Status { get; set; } = ReferenceConfirmationStatus.Pending;
    public DateTime CandidateConsentAtUtc { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;

    public bool? WorkedHere { get; set; }
    public string? PeriodText { get; set; }
    public string? DidWell { get; set; }
    public string? WorkAgain { get; set; }
    public string? ExtraText { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? DeclinedAtUtc { get; set; }

    /// <summary>Master switch for the partner passport. Off until the candidate turns it on.</summary>
    public bool ShowOnPartnerPassport { get; set; }
    public bool ShareWorkedHere { get; set; }
    public bool SharePeriod { get; set; }
    public bool ShareDidWell { get; set; }
    public bool ShareWorkAgain { get; set; }
    public bool ShareExtra { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public static class ReferenceConfirmationStatus
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Declined = "declined";
}
