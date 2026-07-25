namespace Jobsy.Core.Contracts;

public record CandidatePreferencesDto(
    IReadOnlyList<string> Roles,
    int? MaxTravelMinutes,
    string? PreferredTransport,
    string? Language = null,
    int? AgeYears = null);

public record CandidateVacancyEngagementDto(
    Guid Id,
    Guid VacancyId,
    string VacancyTitle,
    string CompanyName,
    DateTime CreatedAt,
    string? Channel = null);
