using System.Security.Claims;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ICandidateInsightsService
{
    Task<CandidateInsightsDto> GetInsightsAsync(
        ClaimsPrincipal principal,
        Guid? branchId,
        int radiusKm,
        int periodDays,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CandidateInsightsBranchDto>> GetBranchesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<CandidateInsightsUnlockResultDto> UnlockAsync(
        ClaimsPrincipal principal,
        string scope,
        Guid? branchId,
        string idempotencyKey,
        Guid? unlockRequestId = null,
        CancellationToken cancellationToken = default);

    Task<CandidateInsightsUnlockRequestDto> CreateUnlockRequestAsync(
        ClaimsPrincipal principal,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<CandidateInsightsUnlockRequestDto> RejectUnlockRequestAsync(
        ClaimsPrincipal principal,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<(string FileName, string Csv)> ExportCsvAsync(
        ClaimsPrincipal principal,
        Guid? branchId,
        int radiusKm,
        int periodDays,
        CancellationToken cancellationToken = default);

    Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken = default);
}
