using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Re-reads operational fields from the database and overlays them on cached dashboard snapshots.
/// </summary>
public interface IDashboardLiveOverlay
{
    Task<IReadOnlyList<MetricCountDto>> OverlayMetricsAsync(
        IReadOnlyList<MetricCountDto> cached,
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default);

    Task<ClientPerformanceBoardDto> OverlayClientsAsync(
        ClientPerformanceBoardDto cached,
        IReadOnlyCollection<Guid>? companyIds,
        CancellationToken cancellationToken = default);

    Task<SalesManagerDashboardDto> OverlaySalesAsync(
        SalesManagerDashboardDto cached,
        CancellationToken cancellationToken = default);

    Task<AmbassadeurDashboardDto> OverlayAmbassadeurAsync(
        AmbassadeurDashboardDto cached,
        CancellationToken cancellationToken = default);
}
