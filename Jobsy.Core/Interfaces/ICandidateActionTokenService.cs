using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public sealed record CandidateActionTokenIssueResult(
    CandidateActionToken Token,
    string PlaintextToken,
    string RelativeActionPath);

public interface ICandidateActionTokenService
{
    Task<CandidateActionTokenIssueResult> IssueAsync(
        Guid userId,
        string purpose,
        Guid? relatedApplicationId = null,
        TimeSpan? lifetime = null,
        CancellationToken cancellationToken = default);

    Task<CandidateActionToken?> FindValidAsync(
        string plaintextToken,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims a valid unused token (sets UsedAtUtc). Returns null if already used/expired/invalid.
    /// </summary>
    Task<CandidateActionToken?> TryConsumeAsync(
        string plaintextToken,
        string purpose,
        CancellationToken cancellationToken = default);
}
