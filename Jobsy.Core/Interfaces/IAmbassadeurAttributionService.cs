namespace Jobsy.Core.Interfaces;

public interface IAmbassadeurAttributionService
{
    /// <summary>
    /// Resolves an Ambassadeur tracking code to a completed profile user id, or null.
    /// </summary>
    Task<Guid?> ResolveAmbassadeurUserIdAsync(
        string? trackingCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attributes a candidate user to an Ambassadeur when not already attributed.
    /// Returns true when attribution was applied.
    /// </summary>
    Task<bool> TryAttributeCandidateAsync(
        Guid candidateUserId,
        string? trackingCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attributes a company to an Ambassadeur (entrepreneur flyer) and grants pending start-highlight.
    /// </summary>
    Task<bool> TryAttributeCompanyAsync(
        Guid companyId,
        string? trackingCode,
        CancellationToken cancellationToken = default);

    Task RecalculateAndPersistCurrentRateSnapshotAsync(
        Guid ambassadeurUserId,
        CancellationToken cancellationToken = default);
}
