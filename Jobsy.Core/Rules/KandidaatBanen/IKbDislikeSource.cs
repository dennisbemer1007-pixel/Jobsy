namespace Jobsy.Core.Rules.KandidaatBanen;

/// <summary>
/// Candidate private dislikes used to down-rank (never hide) vacancies.
/// </summary>
public interface IKbDislikeSource
{
    /// <summary>
    /// Returns matching dislike reason codes for the vacancy, or an empty list when none.
    /// </summary>
    Task<IReadOnlyList<string>> GetMatchingDislikeCodesAsync(
        Guid candidateUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// KB-FALLBACK(D): paspoort 06 not landed — no down-rank and no "Staat lager" note.
/// Replaced when <c>CandidatePrivatePreferences</c> / <c>DislikeMatchRules</c> land.
/// </summary>
public sealed class KbNoDislikeSource : IKbDislikeSource
{
    public static KbNoDislikeSource Instance { get; } = new();

    public Task<IReadOnlyList<string>> GetMatchingDislikeCodesAsync(
        Guid candidateUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}
