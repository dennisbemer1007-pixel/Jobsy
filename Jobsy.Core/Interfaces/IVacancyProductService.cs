using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IVacancyProductService
{
    Task<VacancyProductOutcome> PublishAsync(
        Vacancy vacancy,
        VacancyPublishOptions options,
        Guid? actorUserId,
        CancellationToken cancellationToken = default,
        bool allowPendingApproval = true);

    Task<VacancyProductOutcome> ApprovePublishAsync(
        Vacancy vacancy,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    Task<VacancyProductOutcome> HighlightAsync(
        Vacancy vacancy,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    Task<PushBomPreview> PreviewPushBomAsync(
        Vacancy vacancy,
        CancellationToken cancellationToken = default);

    Task<VacancyProductOutcome> PushBomAsync(
        Vacancy vacancy,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    Task<VacancyProductOutcome> ExtendAsync(
        Vacancy vacancy,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    Task<VacancyProductOutcome> DeactivateAsync(
        Vacancy vacancy,
        CancellationToken cancellationToken = default);
}

public sealed record VacancyPublishOptions(
    bool Highlight = false,
    bool PushBom = false,
    bool Extend = false);

public sealed record VacancyProductOutcome(
    bool Succeeded,
    string? ErrorMessage,
    Vacancy Vacancy,
    bool PendingApproval = false,
    int PushBomRecipientCount = 0,
    bool InsufficientTokens = false,
    decimal RequiredTokens = 0m,
    decimal Balance = 0m,
    Guid? SpendCompanyId = null);

public sealed record PushBomPreview(
    int CandidateCount,
    decimal CostTokens,
    double RadiusKm,
    int MaxTravelMinutes,
    bool HasPricing);
