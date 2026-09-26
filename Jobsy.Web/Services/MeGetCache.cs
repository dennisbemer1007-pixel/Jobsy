using System.Collections.Concurrent;

namespace Jobsy.Web.Services;

/// <summary>
/// Circuit-scoped short TTL cache for me/kompas (and similar) so tab switches
/// and nested panels do not stampede the API.
/// </summary>
public sealed class MeGetCache
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(15);

    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public async Task<MeGetResult<T>> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<MeGetResult<T>>> factory,
        CancellationToken ct = default,
        TimeSpan? ttl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_entries.TryGetValue(key, out var existing)
            && existing is CacheEntry<T> typed
            && !typed.IsExpired)
        {
            return typed.Value;
        }

        var result = await factory(ct);
        // Only cache successful payloads — unavailable must be retried.
        if (!result.TemporarilyUnavailable)
        {
            _entries[key] = new CacheEntry<T>(result, DateTimeOffset.UtcNow.Add(ttl ?? DefaultTtl));
        }

        return result;
    }

    public void Invalidate(string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _entries.Clear();
            return;
        }

        _entries.TryRemove(key, out _);
    }

    private abstract class CacheEntry
    {
        public abstract bool IsExpired { get; }
    }

    private sealed class CacheEntry<T>(MeGetResult<T> value, DateTimeOffset expiresAt) : CacheEntry
    {
        public MeGetResult<T> Value { get; } = value;
        public override bool IsExpired => DateTimeOffset.UtcNow >= expiresAt;
    }
}
