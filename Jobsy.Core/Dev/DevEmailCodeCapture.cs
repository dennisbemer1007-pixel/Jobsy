using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Dev;

/// <summary>
/// Local E2E only: last numeric sign-up/sign-in code per normalized e-mail when
/// <c>JOBSY_E2E_CAPTURE_EMAIL_CODES=1</c>. Never used in production hosts.
/// </summary>
public static class DevEmailCodeCapture
{
    private static readonly Regex SixDigits = new(@"\b(\d{6})\b", RegexOptions.Compiled);

    private static readonly ConcurrentDictionary<string, CaptureEntry> ByEmail = new(StringComparer.OrdinalIgnoreCase);
    private static long _sequence;

    public sealed record CaptureEntry(string Code, long Sequence, Guid? ChallengeId, DateTime CapturedAtUtc);

    public static bool Enabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_CAPTURE_EMAIL_CODES"),
            "1",
            StringComparison.Ordinal);

    public static void TryCapture(string? to, string? htmlOrText)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(htmlOrText))
        {
            return;
        }

        var match = SixDigits.Match(htmlOrText);
        if (!match.Success)
        {
            return;
        }

        Record(to, match.Groups[1].Value, challengeId: null);
    }

    /// <summary>Stores the plaintext code for E2E (API path; avoids relying on HTML parsing alone).</summary>
    public static void Record(string? email, string code, Guid? challengeId)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var normalized = email.Trim().ToLowerInvariant();
        var seq = Interlocked.Increment(ref _sequence);
        if (challengeId is null && ByEmail.TryGetValue(normalized, out var previous))
        {
            // Email stub capture runs after AuthController.Record; keep the challenge id.
            challengeId = previous.ChallengeId;
        }

        ByEmail[normalized] = new CaptureEntry(code.Trim(), seq, challengeId, DateTime.UtcNow);
    }

    public static bool TryGet(string email, out string code)
    {
        if (TryGetEntry(email, out var entry))
        {
            code = entry.Code;
            return true;
        }

        code = "";
        return false;
    }

    public static bool TryGetEntry(string email, out CaptureEntry entry)
    {
        entry = default!;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return ByEmail.TryGetValue(email.Trim().ToLowerInvariant(), out entry!);
    }

    public static void Clear()
    {
        ByEmail.Clear();
        Interlocked.Exchange(ref _sequence, 0);
    }
}
