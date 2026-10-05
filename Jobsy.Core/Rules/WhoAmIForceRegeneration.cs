using System.Collections.Concurrent;

namespace Jobsy.Core.Rules;

/// <summary>
/// One-shot bypass of the 24-hour who-am-i back-off. Admin uses it for test accounts
/// so QA can measure story acceptance without waiting a day.
/// </summary>
public static class WhoAmIForceRegeneration
{
    private static readonly ConcurrentDictionary<Guid, byte> Pending = new();

    public static void Request(Guid userId) => Pending[userId] = 1;

    public static bool Consume(Guid userId) => Pending.TryRemove(userId, out _);

    public static bool ShouldGenerate(
        bool fingerprintMatches,
        bool fromOpenAi,
        bool storyOk,
        DateTime? lastAttemptUtc,
        DateTime utcNow,
        bool forced)
    {
        if (forced)
        {
            return true;
        }

        return CandidateInsightsFingerprint.ShouldGenerateWhoAmI(
            fingerprintMatches,
            fromOpenAi,
            storyOk,
            lastAttemptUtc,
            utcNow);
    }
}
