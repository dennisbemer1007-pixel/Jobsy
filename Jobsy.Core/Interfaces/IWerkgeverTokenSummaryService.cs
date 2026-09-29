namespace Jobsy.Core.Interfaces;

public sealed record WerkgeverTokenBranchUsageDto(
    Guid CompanyId,
    string Name,
    string? RegionName,
    decimal Allocated,
    decimal Used,
    decimal Remaining);

public sealed record WerkgeverTokenSummaryDto(
    decimal CentralBalance,
    decimal Unallocated,
    decimal AllocatedToBranches,
    int BranchCountWithAllocation,
    decimal UsedLast30Days,
    decimal UsedPrevious30Days,
    decimal ReservedForRequests,
    int OpenPublishRequestCount,
    int OpenTokenRequestCount,
    IReadOnlyList<WerkgeverTokenBranchUsageDto> BranchUsage);

public interface IWerkgeverTokenSummaryService
{
    Task<WerkgeverTokenSummaryDto> GetSummaryAsync(
        IReadOnlyList<Guid> companyIds,
        int periodDays = 30,
        CancellationToken cancellationToken = default);
}
