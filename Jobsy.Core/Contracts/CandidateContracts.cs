namespace Jobsy.Core.Contracts;

public record CandidatePreferencesDto(
    IReadOnlyList<string> Roles,
    int? MaxTravelMinutes,
    string? PreferredTransport,
    string? Language = null,
    int? AgeYears = null,
    string? AboutMe = null,
    /// <summary>Default apply motivation; prefilled on apply, editable per vacancy.</summary>
    string? DefaultMotivation = null,
    IReadOnlyList<string>? DrivingLicenses = null,
    // Concrete string[] values — IReadOnlyList as dictionary values can fail System.Text.Json.
    IReadOnlyDictionary<string, string[]>? Availability = null,
    IReadOnlyList<CandidateEmployerHistoryDto>? Employers = null,
    IReadOnlyList<string>? Educations = null,
    string? HomeAddress = null,
    decimal? MinHoursPerWeek = null,
    decimal? MaxHoursPerWeek = null,
    bool? FlexibleTimes = null,
    IReadOnlyList<CandidateCertificateDto>? Certificates = null,
    /// <summary>Legacy flag; candidate home is never shown on Lobsy-CV regardless.</summary>
    bool? ShowAddressOnCv = null,
    /// <summary>Wizard/profile: candidate indicated they have no work experience yet.</summary>
    bool? NoWorkExperience = null,
    /// <summary>Optional study direction (e.g. E&amp;M) paired with education level.</summary>
    string? EducationDirection = null,
    /// <summary>Selected availability preset codes from onboarding wizard v2.</summary>
    IReadOnlyList<string>? AvailabilityPresets = null,
    /// <summary>True when the candidate manually overrode preset-computed hours/day-parts.</summary>
    bool? AvailabilityPresetsOverridden = null,
    /// <summary>Spoken languages (ISO 639-1 + optional CEFR-style level). Max 8.</summary>
    IReadOnlyList<CandidateLanguageDto>? SpokenLanguages = null,
    /// <summary>Separate Dutch level for B1 support: beginner|basis|goed|vloeiend|moedertaal.</summary>
    string? DutchLevel = null,
    /// <summary>Self-knowledge employer preference codes (DiscoveryCatalogs).</summary>
    IReadOnlyList<string>? EmployerPreferences = null,
    /// <summary>Free-text learning goals. Max 5 × 60 chars.</summary>
    IReadOnlyList<string>? LearningGoals = null,
    /// <summary>Hobby catalog codes and/or free text. Max 10.</summary>
    IReadOnlyList<string>? Hobbies = null);

public record CandidateLanguageDto(string Code, string? Level = null);

/// <summary>Candidate-only private preferences (dislikes). Never employer-facing.</summary>
public record CandidatePrivatePreferencesDto(
    IReadOnlyList<string> Dislikes,
    IReadOnlyList<string> CustomDislikes,
    DateTime? UpdatedAtUtc = null);

public record UpdateCandidatePrivatePreferencesRequest(
    IReadOnlyList<string>? Dislikes = null,
    IReadOnlyList<string>? CustomDislikes = null);

public record CandidateEmployerHistoryDto(
    string EmployerName,
    string? Role = null,
    int? Years = null,
    string? Description = null,
    /// <summary>Start month as yyyy-MM. End empty means currently employed.</summary>
    string? StartMonth = null,
    string? EndMonth = null);

public record CandidateCertificateDto(
    string Name,
    int? Year = null);

public record CandidateVacancyEngagementDto(
    Guid Id,
    Guid VacancyId,
    string VacancyTitle,
    string CompanyName,
    DateTime CreatedAt,
    string? Channel = null,
    string? ImageUrl = null,
    string? CompanyLogoUrl = null);
