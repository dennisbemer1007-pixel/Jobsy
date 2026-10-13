namespace Jobsy.Core.Scholen;

public sealed record SchoolRetentionDryRunResult(
    DateOnly TodayAmsterdam,
    DateOnly CutoffDate,
    int ClassesWouldDelete,
    int CodesWouldDelete,
    int ResultsWouldDelete,
    IReadOnlyList<SchoolRetentionDryRunSchool> Schools);

public sealed record SchoolRetentionDryRunSchool(
    Guid SchoolId,
    string SchoolName,
    int Classes,
    int Codes,
    int Results,
    IReadOnlyList<int> SchoolYearStarts);

public sealed record SchoolRetentionRunResult(
    Guid RunId,
    DateOnly CutoffDate,
    int ClassesDeleted,
    int CodesDeleted,
    int ResultsDeleted,
    int AggregatesWritten,
    string Outcome);

public sealed record SchoolEarlyDeleteResult(
    int ClassesDeleted,
    int CodesDeleted,
    int ResultsDeleted,
    int AggregatesWritten);

/// <summary>
/// School-year retention: snapshot k≥5 aggregates then delete individual pupil data.
/// Always callable regardless of <c>SchoolsEnabled</c>.
/// </summary>
public interface ISchoolRetentionService
{
    Task<SchoolRetentionDryRunResult> DryRunAsync(CancellationToken cancellationToken = default);

    /// <summary>Full retention pass for all schools with ended school years.</summary>
    Task<SchoolRetentionRunResult> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Snapshot + delete all pupil/class data for one school + school year (SchoolAdmin early path).
    /// </summary>
    Task<SchoolEarlyDeleteResult> DeleteSchoolYearNowAsync(
        Guid schoolId,
        int schoolYearStart,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin danger path: snapshot all years, delete all classes/codes/results, remove staff, deactivate.
    /// Aggregates remain.
    /// </summary>
    Task<SchoolEarlyDeleteResult> DeleteSchoolNowAsync(
        Guid schoolId,
        string confirmName,
        CancellationToken cancellationToken = default);

    /// <summary>Classes that would be deleted on the next run if the given cutoff were saved.</summary>
    Task<int> CountClassesImpactedByCutoffAsync(
        int cutoffMonth,
        int cutoffDay,
        CancellationToken cancellationToken = default);
}
