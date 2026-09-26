using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateMatchSnapshotService
{
    /// <summary>Read stored matches; enqueue when missing/stale. Never scores vacancies.</summary>
    Task<(IReadOnlyList<CandidateMatchedVacancyDto> Matches, string InsightsStatus)> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Compute and persist matches (worker only — may score vacancies).</summary>
    Task SaveComputedAsync(
        Guid userId,
        IReadOnlyList<CandidateMatchedVacancyDto> matches,
        string inputFingerprint,
        CancellationToken cancellationToken = default);

    Task<string> ComputeInputFingerprintAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Live score against the discovery index (worker / write path only).</summary>
    Task<IReadOnlyList<CandidateMatchedVacancyDto>> ComputeLiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void InvalidateContextCache(Guid userId);
}
