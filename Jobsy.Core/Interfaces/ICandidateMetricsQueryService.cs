using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ICandidateMetricsQueryService
{
    Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
        Guid candidateUserId,
        string period,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
        Guid candidateUserId,
        string key,
        string period,
        CancellationToken cancellationToken = default);
}
