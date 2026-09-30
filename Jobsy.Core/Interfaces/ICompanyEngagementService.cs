using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICompanyEngagementService
{
    Task<CompanyEngagementDto?> GetAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<CompanyEngagementDto> SaveAsync(
        Guid companyId,
        CompanyEngagementUpdate update,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminEngagementQueueItem>> ListAdminQueueAsync(
        string? filter,
        string? search,
        CancellationToken cancellationToken = default);

    Task CheckAsync(Guid claimId, Guid adminUserId, CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid claimId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default);

    Task ResetToSelfDeclaredAsync(
        Guid claimId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task ReportAsync(
        Guid companyId,
        string itemId,
        string message,
        string? reporterEmail,
        CancellationToken cancellationToken = default);
}

public sealed record CompanyEngagementDto(
    Guid CompanyId,
    Guid RootCompanyId,
    IReadOnlyList<CompanyEngagementClaimDto> Claims);

public sealed record CompanyEngagementClaimDto(
    string ItemId,
    string Status,
    string? ProofUrl,
    string? ProofText,
    string? CheckedSource,
    DateTime? CheckedAtUtc,
    DateTime? RemovedAtUtc,
    string? RemovedReason);

public sealed record CompanyEngagementUpdate(
    IReadOnlyList<CompanyEngagementClaimInput> Claims);

public sealed record CompanyEngagementClaimInput(
    string ItemId,
    string? ProofUrl = null,
    string? ProofText = null);

public sealed record AdminEngagementQueueItem(
    Guid ClaimId,
    Guid CompanyId,
    string CompanyName,
    string? KvkNumber,
    string ItemId,
    string Status,
    string? ProofUrl,
    string? ProofText,
    string? CheckedSource,
    DateTime UpdatedAtUtc,
    bool InQueue,
    int OpenReportCount,
    string? RemovedReason);

/// <summary>Public-facing claim (honest label) for company page / vacancy badges.</summary>
public sealed record PublicEngagementBadgeDto(
    string ItemId,
    string Status,
    string? CheckedSource,
    string? ProofUrl);
