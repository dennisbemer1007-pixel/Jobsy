using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>
/// Process-local lockout counters for unknown e-mails (same thresholds as known accounts).
/// Keys are HMAC of the normalized e-mail so plaintext addresses are not retained.
/// </summary>
public sealed class UnknownAccountLockoutTracker
{
    public const int MaxEntries = 100_000;

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly byte[] _hmacKey;

    public UnknownAccountLockoutTracker(string? signingKey)
    {
        var material = string.IsNullOrWhiteSpace(signingKey)
            ? "jobsy-dev-unknown-lockout"
            : signingKey.Trim();
        _hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes(material));
    }

    public string KeyForEmail(string normalizedEmail)
    {
        var hash = HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(normalizedEmail));
        return Convert.ToHexString(hash);
    }

    public bool IsLocked(string key, DateTime utcNow, out DateTime? retryAtUtc)
    {
        retryAtUtc = null;
        if (!_entries.TryGetValue(key, out var entry))
        {
            return false;
        }

        if (entry.LockoutUntilUtc is DateTime until && until > utcNow)
        {
            retryAtUtc = until;
            return true;
        }

        return false;
    }

    public DateTime? RecordFailure(string key, DateTime utcNow)
    {
        EvictIfNeeded(utcNow);

        var entry = _entries.AddOrUpdate(
            key,
            _ => NewFailure(utcNow),
            (_, existing) => ApplyFailure(existing, utcNow));

        return entry.LockoutUntilUtc is DateTime until && until > utcNow ? until : null;
    }

    public void Clear(string key) => _entries.TryRemove(key, out _);

    private static Entry NewFailure(DateTime utcNow)
        => ApplyFailure(new Entry(), utcNow);

    private static Entry ApplyFailure(Entry existing, DateTime utcNow)
    {
        // Pause over → reset counter (and lockout count after 24 h window).
        if (existing.LockoutUntilUtc is DateTime until && until <= utcNow)
        {
            existing.FailedCount = 0;
            existing.LockoutUntilUtc = null;
            if (existing.LastLockoutAtUtc is DateTime last
                && last < utcNow - LoginLockoutRules.LockoutWindow)
            {
                existing.LockoutCount = 0;
            }
        }

        if (existing.LockoutUntilUtc is DateTime stillLocked && stillLocked > utcNow)
        {
            return existing;
        }

        existing.FailedCount++;
        existing.TouchedAtUtc = utcNow;
        if (existing.FailedCount >= LoginLockoutRules.FailedAttemptsBeforeLockout)
        {
            existing.LockoutCount++;
            existing.LastLockoutAtUtc = utcNow;
            existing.LockoutUntilUtc = utcNow.Add(
                LoginLockoutRules.LockoutDuration(existing.LockoutCount));
            existing.FailedCount = 0;
        }

        return existing;
    }

    private void EvictIfNeeded(DateTime utcNow)
    {
        if (_entries.Count < MaxEntries)
        {
            return;
        }

        var cutoff = utcNow - LoginLockoutRules.LockoutWindow;
        foreach (var pair in _entries)
        {
            if (pair.Value.TouchedAtUtc < cutoff
                && (pair.Value.LockoutUntilUtc is null || pair.Value.LockoutUntilUtc <= utcNow))
            {
                _entries.TryRemove(pair.Key, out _);
            }
        }

        if (_entries.Count < MaxEntries)
        {
            return;
        }

        foreach (var oldest in _entries.OrderBy(e => e.Value.TouchedAtUtc).Take(_entries.Count - MaxEntries + 1_000))
        {
            _entries.TryRemove(oldest.Key, out _);
        }
    }

    private sealed class Entry
    {
        public int FailedCount;
        public int LockoutCount;
        public DateTime? LockoutUntilUtc;
        public DateTime? LastLockoutAtUtc;
        public DateTime TouchedAtUtc = DateTime.UtcNow;
    }
}
