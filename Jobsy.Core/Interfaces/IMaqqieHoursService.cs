using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IMaqqieHoursService
{
    Task<bool> CandidateHasActiveMaqqieContractAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default);

    Task<MaqqieHoursOverviewDto?> GetOverviewForCandidateAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default);

    Task<MaqqieHoursWeekDto?> UpsertDraftWeekAsync(
        Guid candidateUserId,
        DateOnly weekStart,
        IReadOnlyDictionary<int, decimal> dailyHours,
        CancellationToken cancellationToken = default);

    Task<bool> SubmitWeekAsync(Guid candidateUserId, Guid weekId, CancellationToken cancellationToken = default);

    Task<bool> EmployerApproveWeekAsync(
        Guid applicationId,
        Guid weekId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> EmployerReturnWeekAsync(
        Guid applicationId,
        Guid weekId,
        Guid actorUserId,
        string note,
        CancellationToken cancellationToken = default);

    Task<bool> EmployerHasMaqqiePlacementsAsync(
        Guid employerUserId,
        IReadOnlySet<Guid>? accessibleCompanyIds,
        CancellationToken cancellationToken = default);

    Task<MaqqieHoursEmployerOverviewDto?> GetEmployerOverviewAsync(
        Guid employerUserId,
        IReadOnlySet<Guid>? accessibleCompanyIds,
        CancellationToken cancellationToken = default);
}

public sealed record MaqqieHoursOverviewDto(
    string EmployerName,
    string VacancyTitle,
    MaqqieHoursWeekDto? CurrentWeek,
    IReadOnlyList<MaqqieHoursWeekSummaryDto> PreviousWeeks);

public sealed record MaqqieHoursWeekDto(
    Guid Id,
    DateOnly WeekStart,
    MaqqieHoursWeekStatus Status,
    IReadOnlyDictionary<int, decimal> DailyHours,
    decimal TotalHours,
    string? EmployerReturnNote = null);

public sealed record MaqqieHoursWeekSummaryDto(
    Guid Id,
    DateOnly WeekStart,
    decimal TotalHours,
    MaqqieHoursWeekStatus Status);

public sealed record MaqqieHoursEmployerOverviewDto(
    IReadOnlyList<MaqqieHoursEmployerWeekRowDto> Weeks);

public sealed record MaqqieHoursEmployerWeekRowDto(
    Guid ApplicationId,
    Guid WeekId,
    DateOnly WeekStart,
    decimal TotalHours,
    string CandidateName,
    string VacancyTitle,
    MaqqieHoursWeekStatus Status);
