namespace Jobsy.Core.Scholen;

/// <summary>
/// Writes anonymous k≥5 aggregates for a school year. No FK to classes/codes.
/// Idempotent per (school, school year).
/// </summary>
public interface ISchoolAggregateSnapshotter
{
    /// <summary>
    /// Snapshots one school + school year (class rows where completed ≥ 5, year row when school total ≥ 5)
    /// and rebuilds the platform-wide year row. Returns number of aggregate rows written.
    /// </summary>
    Task<int> SnapshotSchoolYearAsync(
        Guid schoolId,
        int schoolYearStart,
        CancellationToken cancellationToken = default);

    /// <summary>Snapshots every school that has classes for <paramref name="schoolYearStart"/>.</summary>
    Task<int> SnapshotAllSchoolsForYearAsync(
        int schoolYearStart,
        CancellationToken cancellationToken = default);

    /// <summary>Snapshots the given school for every school year that still has live classes.</summary>
    Task<int> SnapshotAllYearsForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);
}
