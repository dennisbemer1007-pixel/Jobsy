using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

public sealed class CachingMetricsQueryService : IMetricsQueryService
{
    private readonly IMetricsQueryService _inner;
    private readonly IDashboardCache _cache;
    private readonly IDashboardLiveOverlay _live;

    public CachingMetricsQueryService(
        IMetricsQueryService inner,
        IDashboardCache cache,
        IDashboardLiveOverlay live)
    {
        _inner = inner;
        _cache = cache;
        _live = live;
    }

    public async Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default)
    {
        var scope = DashboardCacheKeys.Scope(companyIds);
        var periodKey = DashboardCacheKeys.NormalizePeriod(period);
        var cacheKey = DashboardCacheKeys.Metrics(scope, includePlatformOnly, periodKey);
        if (!_cache.TryGet(cacheKey, out IReadOnlyList<MetricCountDto>? cached) || cached is null)
        {
            cached = await _inner.GetSummaryAsync(includePlatformOnly, companyIds, period, cancellationToken);
            _cache.Set(
                cacheKey,
                (IReadOnlyList<MetricCountDto>)cached.ToList(),
                new DashboardCacheDescriptor(
                    DashboardCacheKind.MetricsSummary,
                    scope,
                    periodKey,
                    includePlatformOnly,
                    companyIds?.ToArray(),
                    UserId: null,
                    Take: 0));
        }

        return await _live.OverlayMetricsAsync(
            cached,
            includePlatformOnly,
            companyIds,
            periodKey,
            cancellationToken);
    }

    public Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
        string key,
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default)
        => _inner.GetDrilldownAsync(key, includePlatformOnly, companyIds, period, cancellationToken);

    public async Task<VacancyPerformanceBoardDto> GetVacancyPerformanceAsync(
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        int take = 3,
        CancellationToken cancellationToken = default)
    {
        var scope = DashboardCacheKeys.Scope(companyIds);
        var periodKey = DashboardCacheKeys.NormalizePeriod(period);
        var cacheKey = DashboardCacheKeys.Vacancy(scope, periodKey, take);
        if (_cache.TryGet(cacheKey, out VacancyPerformanceBoardDto? cached) && cached is not null)
        {
            return cached;
        }

        var board = await _inner.GetVacancyPerformanceAsync(companyIds, period, take, cancellationToken);
        _cache.Set(
            cacheKey,
            board,
            new DashboardCacheDescriptor(
                DashboardCacheKind.VacancyPerformance,
                scope,
                periodKey,
                IncludePlatformOnly: false,
                companyIds?.ToArray(),
                UserId: null,
                take));
        return board;
    }

    public async Task<ClientPerformanceBoardDto> GetClientPerformanceAsync(
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default)
    {
        var scope = DashboardCacheKeys.Scope(companyIds);
        var periodKey = DashboardCacheKeys.NormalizePeriod(period);
        var cacheKey = DashboardCacheKeys.Client(scope, periodKey);
        if (!_cache.TryGet(cacheKey, out ClientPerformanceBoardDto? cached) || cached is null)
        {
            cached = await _inner.GetClientPerformanceAsync(companyIds, period, cancellationToken);
            _cache.Set(
                cacheKey,
                cached,
                new DashboardCacheDescriptor(
                    DashboardCacheKind.ClientPerformance,
                    scope,
                    periodKey,
                    IncludePlatformOnly: false,
                    companyIds?.ToArray(),
                    UserId: null,
                    Take: 0));
        }

        return await _live.OverlayClientsAsync(cached, companyIds, cancellationToken);
    }
}
