using System.Collections.Concurrent;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Per-candidate match results (display scoring uses the same calculator output).
/// Short TTL; keyed by profile revision so updates invalidate automatically.
/// </summary>
public sealed class CandidateMatchScoreCache
{
    public static readonly TimeSpan EntryTtl = TimeSpan.FromSeconds(120);
    private readonly IMemoryCache _cache;

    public CandidateMatchScoreCache(IMemoryCache cache) => _cache = cache;

    public bool TryGet(
        Guid userId,
        string profileRevision,
        bool matchCalibrationV1,
        bool employerFitQuestionnaires,
        Guid vacancyId,
        out ProfileVacancyMatch? match)
    {
        match = null;
        if (string.IsNullOrWhiteSpace(profileRevision))
        {
            return false;
        }

        if (!_cache.TryGetValue<ScoreStore>(
                StoreKey(userId, profileRevision, matchCalibrationV1, employerFitQuestionnaires),
                out var store)
            || store is null)
        {
            return false;
        }

        return store.Matches.TryGetValue(vacancyId, out match);
    }

    public void PutMany(
        Guid userId,
        string profileRevision,
        bool matchCalibrationV1,
        bool employerFitQuestionnaires,
        IEnumerable<KeyValuePair<Guid, ProfileVacancyMatch>> scores)
    {
        if (string.IsNullOrWhiteSpace(profileRevision))
        {
            return;
        }

        var key = StoreKey(userId, profileRevision, matchCalibrationV1, employerFitQuestionnaires);
        var store = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = EntryTtl;
            entry.SlidingExpiration = TimeSpan.FromSeconds(45);
            return new ScoreStore();
        })!;

        foreach (var (vacancyId, match) in scores)
        {
            store.Matches[vacancyId] = match;
        }
    }

    private static string StoreKey(
        Guid userId,
        string profileRevision,
        bool matchCalibrationV1,
        bool employerFitQuestionnaires)
        => $"cand-match:{userId:N}:{profileRevision}:{(matchCalibrationV1 ? "v1" : "leg")}:{(employerFitQuestionnaires ? "ef" : "noef")}";

    private sealed class ScoreStore
    {
        public ConcurrentDictionary<Guid, ProfileVacancyMatch> Matches { get; } = new();
    }
}
