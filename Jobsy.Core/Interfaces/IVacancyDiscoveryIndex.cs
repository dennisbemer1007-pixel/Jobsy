using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Warm in-memory index of publicly visible vacancies for the banenkaart.
/// Discover reads this instead of hitting the database on every map open.
/// </summary>
public interface IVacancyDiscoveryIndex
{
    /// <summary>Mark the snapshot stale so the next read (or the refresh job) rebuilds it.</summary>
    void Invalidate();

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
