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
    DateTime GeneratedAtUtc,
    string ConsentVersion,
    bool IncludeFullAddress,
    bool IncludeContactDetails);

public sealed record LobsyCvEmployerEntry(
    string EmployerName,
    string? Role,
    int? Years,
    string? Description);

public sealed record LobsyCvCertificateEntry(
    string Name,
    int? Year);
