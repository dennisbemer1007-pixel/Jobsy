using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>
/// Process-local rate limit for password-reset requests (3 per e-mail per hour).
/// Keys are HMAC of the normalized e-mail (same pattern as <see cref="UnknownAccountLockoutTracker"/>).
/// </summary>
public sealed class PasswordResetRequestLimiter
{
    public const int MaxPerHour = 3;
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly byte[] _hmacKey;

    public PasswordResetRequestLimiter(string? signingKey)
    {
        var material = string.IsNullOrWhiteSpace(signingKey)
            ? "jobsy-dev-password-reset"
            : signingKey.Trim();
        _hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes(material));
    }

    public string KeyForEmail(string normalizedEmail)
    {
        var hash = HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(normalizedEmail));
        return Convert.ToHexString(hash);
    }

    /// <summary>Returns true when the request is allowed (and records it).</summary>
    public bool TryAllow(string key, DateTime utcNow)
    {
        var cutoff = utcNow - Window;
        var entry = _entries.AddOrUpdate(
            key,
            _ => new Entry { Stamps = [utcNow] },
            (_, existing) =>
            {
                existing.Stamps.RemoveAll(t => t < cutoff);
                return existing;
            });

        lock (entry)
        {
            entry.Stamps.RemoveAll(t => t < cutoff);
            if (entry.Stamps.Count >= MaxPerHour)
            {
                return false;
            }

            entry.Stamps.Add(utcNow);
            return true;
        }
    }

    private sealed class Entry
    {
        public List<DateTime> Stamps { get; init; } = [];
    }
}
