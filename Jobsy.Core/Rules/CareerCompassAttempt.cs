using System.Collections.Concurrent;

namespace Jobsy.Core.Rules;

/// <summary>
/// One model call per score fingerprint per 24 hours, same window as the who-am-i story.
/// A second caller in the same process waits out the in-flight call instead of starting another.
/// </summary>
public static class CareerCompassAttempt
{
    private static readonly ConcurrentDictionary<string, byte> Inflight = new();

    public static bool TryBegin(Guid userId, string? storedJson, string scoresKey, DateTime utcNow)
    {
        var stored = string.IsNullOrWhiteSpace(storedJson)
            ? null
            : CareerCompassJson.TryDeserialize(storedJson);
        var match = stored is not null
                    && string.Equals(stored.ScoresFingerprint, scoresKey, StringComparison.Ordinal);
        var fromOpenAi = stored?.FromOpenAi == true;
        var accepted = fromOpenAi && stored!.HasOccupations;
        if (!CandidateInsightsFingerprint.ShouldGenerateWhoAmI(
                match,
                fromOpenAi,
                accepted,
                stored?.ModelAttemptUtc,
                utcNow))
        {
            return false;
        }

        return Inflight.TryAdd(Key(userId, scoresKey), 0);
    }

    public static void End(Guid userId, string scoresKey)
        => Inflight.TryRemove(Key(userId, scoresKey), out _);

    private static string Key(Guid userId, string scoresKey) => userId.ToString("N") + "|" + scoresKey;
}
