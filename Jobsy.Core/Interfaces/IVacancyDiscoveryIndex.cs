using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Warm in-memory index of publicly visible vacancies for the banenkaart.
/// Discover reads this instead of hitting the database on every map open.
/// </summary>
public interface IVacancyDiscoveryIndex
{
    /// <summary>UTC timestamp of the last successful index rebuild (null until first refresh).</summary>
    DateTime? LastRefreshedAtUtc { get; }

    /// <summary>Mark the snapshot stale so the next read (or the refresh job) rebuilds it.</summary>
    void Invalidate();

    /// <summary>
    /// Mark the snapshot stale after a company verification change so public channels
    /// pick it up within one refresh cycle (≤ 60 s).
    /// </summary>
    Task InvalidateCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Rebuild the snapshot from the database.</summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Return the current public snapshot, refreshing first when empty or stale.
    /// </summary>
    Task<IReadOnlyList<VacancyDiscoveryRecord>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Centroid + zoom of the current public snapshot, for MapLibre first paint.
    /// </summary>
    Task<VacancyMapView> GetMapViewAsync(CancellationToken cancellationToken = default);
}
