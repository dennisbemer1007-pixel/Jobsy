using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ICompanyMapInitiativeService
{
    Task<IReadOnlyList<CompanyMapPinDto>> GetMapPinsAsync(
        double? originLat,
        double? originLng,
        int radiusKm,
        Guid? candidateUserId,
        CancellationToken cancellationToken = default);

    Task<CandidateInitiativeWeekDto> GetCandidateWeekAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default);

    Task<SendWorkInitiativeResult> SendAsync(
        Guid candidateUserId,
        SendWorkInitiativeRequest request,
        CancellationToken cancellationToken = default);

    Task<SendWorkInitiativeResult> SendDraftAsync(
        Guid candidateUserId,
        Guid draftId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkInitiativeListItemDto>> ListForCandidateAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployerInitiativeInboxItemDto>> ListForEmployerAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<CompanyInitiativeSettingsDto?> GetSettingsAsync(
        Guid rootCompanyId,
        CancellationToken cancellationToken = default);

    Task<CompanyInitiativeSettingsDto> SaveSettingsAsync(
        Guid rootCompanyId,
        CompanyInitiativeSettingsDto dto,
        CancellationToken cancellationToken = default);

    Task<InitiativeTeamDto> SaveTeamAsync(
        Guid rootCompanyId,
        InitiativeTeamDto team,
        CancellationToken cancellationToken = default);

    Task ResendTeamEmailConfirmationAsync(Guid rootCompanyId, Guid teamId, CancellationToken cancellationToken = default);

    Task ResendGeneralEmailConfirmationAsync(Guid rootCompanyId, CancellationToken cancellationToken = default);

    Task ConfirmTeamEmailAsync(Guid teamId, string code, CancellationToken cancellationToken = default);

    Task ConfirmGeneralEmailAsync(Guid rootCompanyId, string code, CancellationToken cancellationToken = default);

    Task AcceptAsync(Guid employerUserId, Guid initiativeId, CancellationToken cancellationToken = default);

    Task DeclineAsync(
        Guid employerUserId,
        Guid initiativeId,
        bool keepOpen,
        int? reducedMaxPerWeek,
        bool closeTeam,
        CancellationToken cancellationToken = default);

    Task MarkViewedAsync(Guid employerUserId, Guid initiativeId, CancellationToken cancellationToken = default);

    Task WithdrawAsync(Guid candidateUserId, Guid initiativeId, CancellationToken cancellationToken = default);

    Task<int> AutoDeclineOverdueAsync(CancellationToken cancellationToken = default);
}
