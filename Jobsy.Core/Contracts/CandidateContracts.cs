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
    bool? ShowAddressOnCv = null);

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
