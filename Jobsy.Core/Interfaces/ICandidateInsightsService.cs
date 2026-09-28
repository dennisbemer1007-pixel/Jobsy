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
}
