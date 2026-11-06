using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Pupil login abuse limits (04.3). Class pause and per-code lock are DB-backed (multi-instance).
/// Per-(class, client-partition) failure buckets are process-local — same assumption as
/// <c>LoginProtectionRateLimiter</c> for staff login.
/// </summary>
public interface IPupilLoginProtection
{
    string ClientPartition(string? ip, string? userAgent);

    /// <summary>Returns false when the (class, partition) bucket is in cooldown.</summary>
    bool TryAcquireClassPartition(Guid classId, string partition);

    void RecordFailure(Guid classId, string partition);

    void ClearFailures(Guid classId, string partition);

    bool IsClassPartitionCoolingDown(Guid classId, string partition, out TimeSpan remaining);

    /// <summary>Class-wide failure count in the rolling hour (process-local).</summary>
    int RecordClassHourFailure(Guid classId);

    /// <summary>Successful logins on one code in the rolling hour (process-local).</summary>
    int RecordCodeSuccess(Guid pupilCodeId);
}

public sealed class PupilLoginProtection : IPupilLoginProtection
{
    public const int ClassPartitionFailLimit = 10;
    public static readonly TimeSpan ClassPartitionWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ClassPartitionCooldown = TimeSpan.FromMinutes(15);

    public const int ClassHourFailLimit = 50;
    public static readonly TimeSpan ClassHourWindow = TimeSpan.FromHours(1);
    public static readonly TimeSpan ClassPauseDuration = TimeSpan.FromMinutes(30);

    public const int CodeSuccessLimit = 20;
    public static readonly TimeSpan CodeSuccessWindow = TimeSpan.FromHours(1);
    public static readonly TimeSpan CodeLockDuration = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Bucket> _buckets = new(StringComparer.Ordinal);
    private readonly byte[] _hmacKey;
    private readonly TimeProvider _clock;

    public PupilLoginProtection(TimeProvider? clock = null, byte[]? hmacKey = null)
    {
        _clock = clock ?? TimeProvider.System;
        _hmacKey = hmacKey ?? Encoding.UTF8.GetBytes("Jobsy.PupilLoginProtection.v1");
    }

    public string ClientPartition(string? ip, string? userAgent)
    {
        var family = UserAgentFamily(userAgent);
        var material = $"{ip ?? "unknown"}|{family}";
        var hash = HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }

    public bool TryAcquireClassPartition(Guid classId, string partition)
    {
        if (IsClassPartitionCoolingDown(classId, partition, out _))
        {
            return false;
        }

        return true;
    }

    public void RecordFailure(Guid classId, string partition)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var key = $"cp:{classId:D}:{partition}";
        var bucket = _buckets.GetOrAdd(key, _ => new Bucket());
        lock (bucket)
        {
            if (bucket.CooldownUntilUtc is DateTime until && until > now)
            {
                return;
            }

            if (now - bucket.WindowStartUtc >= ClassPartitionWindow)
            {
                bucket.WindowStartUtc = now;
                bucket.Count = 0;
            }

            bucket.Count++;
            if (bucket.Count >= ClassPartitionFailLimit)
            {
                bucket.CooldownUntilUtc = now.Add(ClassPartitionCooldown);
                bucket.Count = 0;
                bucket.WindowStartUtc = now;
            }
        }
    }

    public void ClearFailures(Guid classId, string partition)
    {
        var key = $"cp:{classId:D}:{partition}";
        _buckets.TryRemove(key, out _);
    }

    public bool IsClassPartitionCoolingDown(Guid classId, string partition, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;
        var key = $"cp:{classId:D}:{partition}";
        if (!_buckets.TryGetValue(key, out var bucket))
        {
            return false;
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        lock (bucket)
        {
            if (bucket.CooldownUntilUtc is DateTime until && until > now)
            {
                remaining = until - now;
                return true;
            }
        }

        return false;
    }

    public int RecordClassHourFailure(Guid classId)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var key = $"ch:{classId:D}";
        var bucket = _buckets.GetOrAdd(key, _ => new Bucket());
        lock (bucket)
        {
            if (now - bucket.WindowStartUtc >= ClassHourWindow)
            {
                bucket.WindowStartUtc = now;
                bucket.Count = 0;
            }

            return ++bucket.Count;
        }
    }

    public int RecordCodeSuccess(Guid pupilCodeId)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var key = $"cs:{pupilCodeId:D}";
        var bucket = _buckets.GetOrAdd(key, _ => new Bucket());
        lock (bucket)
        {
            if (now - bucket.WindowStartUtc >= CodeSuccessWindow)
            {
                bucket.WindowStartUtc = now;
                bucket.Count = 0;
            }

            return ++bucket.Count;
        }
    }

    private static string UserAgentFamily(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua))
        {
            return "unknown";
        }

        var s = ua.ToLowerInvariant();
        if (s.Contains("edg/", StringComparison.Ordinal))
        {
            return "edge";
        }

        if (s.Contains("chrome/", StringComparison.Ordinal) && !s.Contains("edg/", StringComparison.Ordinal))
        {
            return "chrome";
        }

        if (s.Contains("firefox/", StringComparison.Ordinal))
        {
            return "firefox";
        }

        if (s.Contains("safari/", StringComparison.Ordinal) && !s.Contains("chrome/", StringComparison.Ordinal))
        {
            return "safari";
        }

        return "other";
    }

    private sealed class Bucket
    {
        public DateTime WindowStartUtc = DateTime.MinValue;
        public int Count;
        public DateTime? CooldownUntilUtc;
    }
}
