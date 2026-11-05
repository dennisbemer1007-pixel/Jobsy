using Jobsy.Core.Contracts.Scholen;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Scholen;

/// <summary>
/// Per-pupil progress snapshot on the Web host to skip redundant GET api/pupil/progress after
/// an answer save (SSR + leerling-journey-ssr.js still re-render /leerling/reis).
/// </summary>
public sealed class PupilWebProgressCache
{
    private const string SnapshotPrefix = "pupil-web-progress:snap:";
    private const string SkipFetchPrefix = "pupil-web-progress:skip:";

    private readonly IMemoryCache _cache;

    public PupilWebProgressCache(IMemoryCache cache) => _cache = cache;

    public void RememberSnapshot(Guid pupilCodeId, PupilProgressStateDto progress)
        => _cache.Set(SnapshotPrefix + pupilCodeId.ToString("D"), progress, TimeSpan.FromMinutes(30));

    public bool TryGetSnapshot(Guid pupilCodeId, out PupilProgressStateDto? progress)
        => _cache.TryGetValue(SnapshotPrefix + pupilCodeId.ToString("D"), out progress);

    public void StageAfterAnswer(Guid pupilCodeId, PupilProgressStateDto progress)
    {
        RememberSnapshot(pupilCodeId, progress);
        _cache.Set(SkipFetchPrefix + pupilCodeId.ToString("D"), true, TimeSpan.FromSeconds(45));
    }

    public bool TryConsumeSkipFetch(Guid pupilCodeId, out PupilProgressStateDto? progress)
    {
        progress = null;
        var skipKey = SkipFetchPrefix + pupilCodeId.ToString("D");
        if (!_cache.TryGetValue(skipKey, out _))
        {
            return false;
        }

        _cache.Remove(skipKey);
        return TryGetSnapshot(pupilCodeId, out progress);
    }

    public void Invalidate(Guid pupilCodeId)
    {
        var id = pupilCodeId.ToString("D");
        _cache.Remove(SnapshotPrefix + id);
        _cache.Remove(SkipFetchPrefix + id);
    }
}
