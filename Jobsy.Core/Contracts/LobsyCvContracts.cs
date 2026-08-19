namespace Jobsy.Core.Contracts;

/// <summary>Input for QuestPDF Lobsy-CV rendering (no EF entities).</summary>
public sealed record LobsyCvModel(
    string FullName,
    string? Email,
    string? PhoneNumber,
    bool WhatsAppContactAllowed,
    string? City,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? AboutMe,
    string? Motivation,
    string? PreferredTransport,
    int? MaxTravelMinutes,
    int? EstimatedTravelMinutes,
    decimal? MinHoursPerWeek,
    decimal? MaxHoursPerWeek,
    bool FlexibleTimes,
    string? AvailabilitySummary,
    IReadOnlyDictionary<string, string[]>? AvailabilitySlots,
    IReadOnlyList<string> DrivingLicenses,
    IReadOnlyList<string> Educations,
    IReadOnlyList<LobsyCvEmployerEntry> Employers,
    IReadOnlyList<LobsyCvCertificateEntry> Certificates,
    int? MatchPercent,
    string? VacancyTitle,
    string? CompanyName,
    DateOnly? DateOfBirth,
    int? AgeYears,
    DateTime GeneratedAtUtc,
    string ConsentVersion,
    bool IncludeFullAddress,
    bool IncludeContactDetails,
    /// <summary>Workplace pin (employer). Candidate home is never plotted.</summary>
    double? WorkplaceLatitude = null,
    double? WorkplaceLongitude = null,
    string? WorkplaceAddress = null,
    /// <summary>Minutes used for the privacy reach circle / caption (usually estimated travel).</summary>
    int? ReachTravelMinutes = null,
    /// <summary>Crow-flies km candidate ↔ workplace; drives circle radius when set.</summary>
    double? DistanceKm = null,
    /// <summary>When true, the PDF banner states that the candidate also uploaded their own CV.</summary>
    bool HasUploadedOwnCv = false);

public sealed record LobsyCvEmployerEntry(
    string EmployerName,
    string? Role,
    int? Years,
    string? Description,
    string? StartMonth = null,
    string? EndMonth = null);

public sealed record LobsyCvCertificateEntry(
    string Name,
    int? Year);
