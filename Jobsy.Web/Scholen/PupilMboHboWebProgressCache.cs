using Jobsy.Core.Contracts.Scholen;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Scholen;

/// <summary>
/// Per-pupil MBO/HBO answer staging on the Web host so a 429 on save does not drop the
/// selected scale values, and a freshly saved next question can skip a redundant GET.
/// </summary>
public sealed class PupilMboHboWebProgressCache
{
    private const string PendingPrefix = "pupil-mbo-hbo-pending:";
    private const string QuestionPrefix = "pupil-mbo-hbo-q:";
    private const string SkipFetchPrefix = "pupil-mbo-hbo-skip:";

    private readonly IMemoryCache _cache;

    public PupilMboHboWebProgressCache(IMemoryCache cache) => _cache = cache;

    public void StagePendingAnswer(
        Guid pupilCodeId,
        string blockKey,
        string itemId,
        int? value,
        int? leuk,
        int? goed)
    {
        var key = PendingPrefix + pupilCodeId.ToString("D") + ":" + blockKey;
        _cache.Set(key, new PendingAnswer(itemId, value, leuk, goed), TimeSpan.FromMinutes(10));
    }

    public bool TryGetPendingAnswer(
        Guid pupilCodeId,
        string blockKey,
        string itemId,
        out PendingAnswer pending)
    {
        pending = default!;
        var key = PendingPrefix + pupilCodeId.ToString("D") + ":" + blockKey;
        if (!_cache.TryGetValue(key, out PendingAnswer? stored)
            || stored is null
            || !string.Equals(stored.ItemId, itemId, StringComparison.Ordinal))
        {
            return false;
        }

        pending = stored;
        return true;
    }

    public void ClearPendingAnswer(Guid pupilCodeId, string blockKey)
        => _cache.Remove(PendingPrefix + pupilCodeId.ToString("D") + ":" + blockKey);

    public void RememberQuestion(Guid pupilCodeId, string blockKey, PupilMboHboQuestionDto question)
        => _cache.Set(QuestionKey(pupilCodeId, blockKey), question, TimeSpan.FromMinutes(30));

    public void StageAfterAnswer(Guid pupilCodeId, string blockKey, PupilMboHboQuestionDto? nextQuestion)
    {
        if (nextQuestion is not null)
        {
            RememberQuestion(pupilCodeId, blockKey, nextQuestion);
        }

        _cache.Set(SkipKey(pupilCodeId, blockKey), true, TimeSpan.FromSeconds(45));
    }

    public bool TryConsumeSkipFetch(
        Guid pupilCodeId,
        string blockKey,
        out PupilMboHboQuestionDto? question)
    {
        question = null;
        var skipKey = SkipKey(pupilCodeId, blockKey);
        if (!_cache.TryGetValue(skipKey, out _))
        {
            return false;
        }

        _cache.Remove(skipKey);
        return _cache.TryGetValue(QuestionKey(pupilCodeId, blockKey), out question);
    }

    public void Invalidate(Guid pupilCodeId, string blockKey)
    {
        var id = pupilCodeId.ToString("D");
        _cache.Remove(PendingPrefix + id + ":" + blockKey);
        _cache.Remove(QuestionKey(pupilCodeId, blockKey));
        _cache.Remove(SkipKey(pupilCodeId, blockKey));
    }

    private static string QuestionKey(Guid pupilCodeId, string blockKey)
        => QuestionPrefix + pupilCodeId.ToString("D") + ":" + blockKey;

    private static string SkipKey(Guid pupilCodeId, string blockKey)
        => SkipFetchPrefix + pupilCodeId.ToString("D") + ":" + blockKey;

    public sealed record PendingAnswer(string ItemId, int? Value, int? Leuk, int? Goed);
}
