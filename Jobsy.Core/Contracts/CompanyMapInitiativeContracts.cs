using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts;

public sealed record CompanyMapPinDto(
    Guid CompanyId,
    Guid? RootCompanyId,
    string Name,
    string? SectorLabel,
    string? City,
    double Latitude,
    double Longitude,
    bool OpenForInitiative,
    bool IsIntermediary,
    int VacancyCount,
    int OpenTeamCount,
    InitiativeMatchLabel? MatchLabel,
    int? AvgResponseDays,
    string? KvkNumber,
    string? PublicPath);

public sealed record CandidateInitiativeWeekDto(int Used, int Max, DateTime ResetsAtUtc);

public sealed record SendWorkInitiativeRequest(
    Guid RootCompanyId,
    Guid? BranchCompanyId,
    Guid? InitiativeTeamId,
    string Motivation,
    bool AttachPassport);

public sealed record SendWorkInitiativeResult(
    bool Sent,
    bool SavedAsDraft,
    Guid? InitiativeId,
    CandidateInitiativeWeekDto? Week,
    string? ErrorCode);

public sealed record WorkInitiativeListItemDto(
    Guid Id,
    string CompanyName,
    string? TeamName,
    WorkInitiativeStatus Status,
    InitiativeMatchLabel MatchLabel,
    DateTime CreatedAtUtc,
    DateTime ResponseDeadlineUtc,
    DateTime? RespondedAtUtc,
    DateTime? EmployerViewedAtUtc,
    bool IsInitiative);

public sealed record EmployerInitiativeInboxItemDto(
    Guid Id,
    string DisplayName,
    string RoleTitle,
    string RegionLabel,
    string AvailabilitySummary,
    int? HoursPerWeek,
    InitiativeMatchLabel MatchLabel,
    string WorkDnaSummary,
    WorkInitiativeStatus Status,
    DateTime CreatedAtUtc,
    DateTime ResponseDeadlineUtc,
    bool PiiRevealed,
    string? Motivation,
    string? CandidateName,
    string? CandidateEmail);

public sealed record InitiativeTeamDto(
    Guid Id,
    string Name,
    string Description,
    Guid? BranchCompanyId,
    string? BranchLabel,
    string ContactEmail,
    bool EmailConfirmed,
    int MaxPerWeek,
    bool IsOpen);

public sealed record PublicCompanyInitiativeViewDto(
    Guid RootCompanyId,
    bool OpenForInitiative,
    int OpenTeamCount,
    string? HubAboutText,
    string? HubCultureText,
    string? HubVideoUrl,
    IReadOnlyList<string> HubPhotoUrls,
    IReadOnlyList<PublicInitiativeTeamOptionDto> Teams,
    CandidateInitiativeWeekDto? Week);

public sealed record PublicInitiativeTeamOptionDto(
    Guid Id,
    string Name,
    string Description,
    string? BranchLabel,
    InitiativeMatchLabel? MatchLabel,
    bool IsOpen);

public sealed record CompanyInitiativeSettingsDto(
    bool IsEnabled,
    string ContactEmail,
    bool ContactEmailConfirmed,
    int MaxPerWeek,
    IReadOnlyList<InitiativeTeamDto> Teams);
