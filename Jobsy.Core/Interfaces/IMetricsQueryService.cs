using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface IMetricsQueryService
{
    Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
        string key,
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default);

    Task<VacancyPerformanceBoardDto> GetVacancyPerformanceAsync(
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        int take = 3,
        CancellationToken cancellationToken = default);
}
