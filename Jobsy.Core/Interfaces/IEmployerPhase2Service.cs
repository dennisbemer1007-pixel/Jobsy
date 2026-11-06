using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IEmployerPhase2Service
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    Task<EmployerPhase2AcceptResult> AcceptApplicationAsync(
        Guid applicationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerPhase2PlacementResult> ChooseEmploymentModeAsync(
        Guid applicationId,
        PlacementEmploymentMode mode,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerPhase2WalletDto> GetWalletAsync(
        Guid billingCompanyId,
        CancellationToken cancellationToken = default);

    Task TryCreditMaqqieWeekOneAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}

public sealed record EmployerPhase2AcceptResult(
    bool Succeeded,
    string? ErrorCode,
    string? UserMessage,
    decimal? BalanceAfter);

public sealed record EmployerPhase2PlacementResult(
    bool Succeeded,
    string? ErrorCode,
    string? UserMessage);

public sealed record EmployerPhase2WalletDto(
    decimal BalanceTokens,
    bool PilotActive,
    decimal AcceptCostTokens,
    decimal AcceptCostEuro,
    IReadOnlyList<EmployerPhase2WalletLineDto> RecentLines,
    IReadOnlyList<EmployerPhase2PendingCreditDto> PendingCredits);

public sealed record EmployerPhase2WalletLineDto(
    string Kind,
    decimal TokenDelta,
    string Title,
    string Subtitle,
    DateTime OccurredAtUtc);

public sealed record EmployerPhase2PendingCreditDto(
    Guid ApplicationId,
    string CandidateLabel,
    string Detail,
    DateOnly? FirstWorkWeekStart);
