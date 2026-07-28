namespace Jobsy.Core.Contracts;

public record CandidatePreferencesDto(
    IReadOnlyList<string> Roles,
    int? MaxTravelMinutes,
    string? PreferredTransport,
    string? Language = null,
    int? AgeYears = null,
    string? AboutMe = null,
    IReadOnlyList<string>? DrivingLicenses = null,
    // Concrete string[] values — IReadOnlyList as dictionary values can fail System.Text.Json.
    IReadOnlyDictionary<string, string[]>? Availability = null,
    IReadOnlyList<CandidateEmployerHistoryDto>? Employers = null,
    IReadOnlyList<string>? Educations = null);

public record CandidateEmployerHistoryDto(
    string EmployerName,
    string? Role = null,
    int? Years = null);

public record CandidateVacancyEngagementDto(
    Guid Id,
    Guid VacancyId,
    string VacancyTitle,
    string CompanyName,
    DateTime CreatedAt,
    string? Channel = null,
    string? ImageUrl = null);
