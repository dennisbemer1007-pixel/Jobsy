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
    /// <summary>Not rendered on the PDF (AVG/age discrimination; beslissing 3).</summary>
    DateOnly? DateOfBirth,
    /// <summary>Not rendered on the PDF (AVG/age discrimination; beslissing 3).</summary>
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
    bool HasUploadedOwnCv = false,
    /// <summary>
    /// Not rendered. Callers may still carry the opt-in payload; the PDF never prints AI output (decision 22).
    /// </summary>
    LobsyCvWhoAmI? WhoAmI = null,
    /// <summary>Candidate-entered foreign-diploma evaluations. Fields only; never the uploaded file.</summary>
    IReadOnlyList<LobsyCvDiplomaEvaluationEntry>? DiplomaEvaluations = null,
    /// <summary>Spoken languages for the candidate's own CV. Not a street address.</summary>
    IReadOnlyList<string>? Languages = null,
    /// <summary>Rule-based test lines for the candidate's own CV. Not the AI story.</summary>
    IReadOnlyList<string>? TestHighlights = null,
    /// <summary>When the candidate accepted the terms. The policy version is not a date.</summary>
    DateTime? ConsentAcceptedAt = null,
    /// <summary>Study direction, printed next to the education level.</summary>
    string? EducationDirection = null);

public sealed record LobsyCvWhoAmI(
    string Story,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<LobsyCvScoreBar> Competencies,
    IReadOnlyList<LobsyCvScoreBar> Culture);

public sealed record LobsyCvScoreBar(string Label, int Percent);

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

/// <summary>Printed diploma evaluation. <see cref="EquivalentLevelText"/> is the candidate's wording, not a derived level.</summary>
public sealed record LobsyCvDiplomaEvaluationEntry(
    string? DiplomaTitle,
    string EquivalentLevelText,
    string Attribution,
    DateOnly EvaluationDate,
    string ReferenceNumber,
    string? EquivalentLevelCode);
