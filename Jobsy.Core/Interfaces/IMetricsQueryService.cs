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

    /// <summary>
    /// Same Uitsplitsing rows as <see cref="GetDrilldownAsync"/>, limited to one vacancy.
    /// Admin vacatures use this so the drilldown does not depend on the employers feature flag.
    /// </summary>
    Task<IReadOnlyList<MetricDrilldownItemDto>> GetVacancyDrilldownAsync(
        string key,
        Guid vacancyId,
        string period,
        CancellationToken cancellationToken = default)
        => GetDrilldownAsync(key, includePlatformOnly: true, companyIds: null, period, cancellationToken);

    Task<VacancyPerformanceBoardDto> GetVacancyPerformanceAsync(
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        int take = 3,
        CancellationToken cancellationToken = default);

    Task<ClientPerformanceBoardDto> GetClientPerformanceAsync(
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default);
}
