namespace Jobsy.Core.Contracts;

/// <summary>Input for QuestPDF Lobsy-CV rendering (no EF entities).</summary>
public sealed record LobsyCvModel(
    string FullName,
    string? Email,
    string? City,
    string? Address,
    string? AboutMe,
    string? Motivation,
    string? PreferredTransport,
    int? MaxTravelMinutes,
    int? EstimatedTravelMinutes,
    decimal? MinHoursPerWeek,
    decimal? MaxHoursPerWeek,
    bool FlexibleTimes,
    string? AvailabilitySummary,
    IReadOnlyList<string> DrivingLicenses,
    IReadOnlyList<string> Educations,
    IReadOnlyList<LobsyCvEmployerEntry> Employers,
    int? MatchPercent,
    string? VacancyTitle,
    string? CompanyName,
    DateTime GeneratedAtUtc,
    string ConsentVersion,
    bool IncludeFullAddress,
    bool IncludeContactEmail);

public sealed record LobsyCvEmployerEntry(
    string EmployerName,
    string? Role,
    int? Years,
    string? Description);
