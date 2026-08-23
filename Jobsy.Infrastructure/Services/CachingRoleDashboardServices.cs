using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

public sealed class CachingSalesManagerDashboardService : ISalesManagerDashboardService
{
    private readonly ISalesManagerDashboardService _inner;
    private readonly IDashboardCache _cache;
    private readonly IDashboardLiveOverlay _live;

    public CachingSalesManagerDashboardService(
        ISalesManagerDashboardService inner,
        IDashboardCache cache,
        IDashboardLiveOverlay live)
    {
        _inner = inner;
        _cache = cache;
        _live = live;
    }

    public async Task<SalesManagerDashboardDto?> GetDashboardAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = DashboardCacheKeys.Sales(salesManagerUserId);
        if (!_cache.TryGet(cacheKey, out SalesManagerDashboardDto? cached) || cached is null)
        {
            cached = await _inner.GetDashboardAsync(salesManagerUserId, cancellationToken);
            if (cached is null)
            {
                return null;
            }

            _cache.Set(
                cacheKey,
                cached,
                new DashboardCacheDescriptor(
                    DashboardCacheKind.SalesDashboard,
                    salesManagerUserId.ToString("D"),
                    Period: "live",
                    IncludePlatformOnly: false,
                    CompanyIds: null,
                    salesManagerUserId,
                    Take: 0));
        }

        return await _live.OverlaySalesAsync(cached, cancellationToken);
    }

    public Task<IReadOnlyList<SalesManagerListItemDto>> ListSalesManagersAsync(
        CancellationToken cancellationToken = default)
        => _inner.ListSalesManagersAsync(cancellationToken);
}

public sealed class CachingAmbassadeurDashboardService : IAmbassadeurDashboardService
{
    private readonly IAmbassadeurDashboardService _inner;
    private readonly IDashboardCache _cache;
    private readonly IDashboardLiveOverlay _live;

    public CachingAmbassadeurDashboardService(
        IAmbassadeurDashboardService inner,
        IDashboardCache cache,
        IDashboardLiveOverlay live)
    {
        _inner = inner;
        _cache = cache;
        _live = live;
    }

    public async Task<AmbassadeurDashboardDto?> GetDashboardAsync(
        Guid ambassadeurUserId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = DashboardCacheKeys.Ambassadeur(ambassadeurUserId);
        if (!_cache.TryGet(cacheKey, out AmbassadeurDashboardDto? cached) || cached is null)
        {
            cached = await _inner.GetDashboardAsync(ambassadeurUserId, cancellationToken);
            if (cached is null)
            {
                return null;
            }

            _cache.Set(
                cacheKey,
                cached,
                new DashboardCacheDescriptor(
                    DashboardCacheKind.AmbassadeurDashboard,
                    ambassadeurUserId.ToString("D"),
                    Period: "live",
                    IncludePlatformOnly: false,
                    CompanyIds: null,
                    ambassadeurUserId,
                    Take: 0));
        }

        return await _live.OverlayAmbassadeurAsync(cached, cancellationToken);
    }

    public Task<IReadOnlyList<AmbassadeurListItemDto>> ListAmbassadeursAsync(
        CancellationToken cancellationToken = default)
        => _inner.ListAmbassadeursAsync(cancellationToken);
}
