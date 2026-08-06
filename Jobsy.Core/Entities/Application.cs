using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
    public Guid? CandidateUserId { get; set; }
    public User? CandidateUser { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CandidateCity { get; set; }
    public string? CandidateAddress { get; set; }
    public string PreferredTransport { get; set; } = string.Empty;
    public int EstimatedTravelMinutes { get; set; }
    public double? DistanceKm { get; set; }
    public string? PreferencesSummary { get; set; }
    /// <summary>Age at apply (years). Safe to show to employers before Accept.</summary>
    public int? CandidateAgeYears { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime? ConsentAcceptedAt { get; set; }
    public string? ConsentVersion { get; set; }
    public bool WorkPermitConfirmed { get; set; }
    public string? EmailVerificationCode { get; set; }
    public DateTime? EmailVerificationExpiresAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    /// <summary>Failed OTP guesses for the current verification code; lockout after max attempts.</summary>
    public int EmailVerificationFailedAttempts { get; set; }
    public string? SnapshotAvailabilityJson { get; set; }
    public string? SnapshotDrivingLicenses { get; set; }
    public string? SnapshotEducations { get; set; }
    public string? SnapshotAboutMe { get; set; }
    public int CandidateEmployerCount { get; set; }

    /// <summary>Phone snapshot at apply (released with PII after Accept).</summary>
    public string? SnapshotPhoneNumber { get; set; }

    /// <summary>WhatsApp contact consent snapshot at apply.</summary>
    public bool SnapshotWhatsAppAllowed { get; set; }

    /// <summary>Home latitude at apply for Lobsy-CV map card.</summary>
    public double? SnapshotHomeLatitude { get; set; }

    /// <summary>Home longitude at apply for Lobsy-CV map card.</summary>
    public double? SnapshotHomeLongitude { get; set; }

    /// <summary>JSON array of { name, year } certificates/courses at apply.</summary>
    public string? SnapshotCertificatesJson { get; set; }

    /// <summary>Whether the candidate allows address/map on the Lobsy-CV.</summary>
    public bool SnapshotShowAddressOnCv { get; set; } = true;

    /// <summary>Optional free-text motivation on apply (separate from SnapshotAboutMe).</summary>
    public string? Motivation { get; set; }

    /// <summary>Studentnummer (verplicht bij exclusieve stageplek).</summary>
    public string? StudentNumber { get; set; }

    /// <summary>School e-mailadres (verplicht bij exclusieve stageplek).</summary>
    public string? SchoolEmail { get; set; }

    /// <summary>Opleiding (verplicht bij exclusieve stageplek).</summary>
    public string? StudyProgram { get; set; }

    /// <summary>Leerjaar (optioneel bij exclusieve stageplek).</summary>
    public string? StudyYear { get; set; }

    /// <summary>Uitkomst van exclusiviteitsvalidatie: Ok / Failed / NotApplicable.</summary>
    public string? ExclusivityValidationStatus { get; set; }

    /// <summary>True when candidate proceeded despite match score &lt; 50% (Gulden Middenweg).</summary>
    public bool ViaSafetyNet { get; set; }

    /// <summary>Persisted match percentage at apply time (0–100).</summary>
    public int? MatchPercent { get; set; }

    /// <summary>JSON snapshot of match score breakdown for employer/candidate UI.</summary>
    public string? MatchBreakdownJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
