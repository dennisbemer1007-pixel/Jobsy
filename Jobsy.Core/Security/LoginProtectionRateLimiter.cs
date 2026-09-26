using System.Collections.Concurrent;

namespace Jobsy.Core.Security;

/// <summary>
/// Process-local dual limiter for sensitive anonymous actions. Each request consumes an
/// IP bucket and an account bucket, so one IP cannot spray accounts and one account cannot
/// be exhausted by a distributed attack.
/// </summary>
public sealed class LoginProtectionRateLimiter
{
    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);

    public bool TryAcquire(string operation, string ip, string? account)
    {
        var now = DateTime.UtcNow;
        var accountKey = string.IsNullOrWhiteSpace(account)
            ? "missing"
            : account.Trim().ToLowerInvariant();
        return TryAcquireBucket($"{operation}:ip:{ip}", 20, now)
               && TryAcquireBucket($"{operation}:account:{accountKey}", 8, now);
    }

    private bool TryAcquireBucket(string key, int limit, DateTime now)
    {
        var window = _windows.GetOrAdd(key, _ => new Window(now));
        lock (window)
        {
            if (now - window.StartUtc >= TimeSpan.FromMinutes(1))
            {
                window.StartUtc = now;
                window.Count = 0;
            }

            if (window.Count >= limit)
            {
                return false;
            }

            window.Count++;
            return true;
        }
    }

    private sealed class Window(DateTime startUtc)
    {
        public DateTime StartUtc = startUtc;
        public int Count;
    }
}
