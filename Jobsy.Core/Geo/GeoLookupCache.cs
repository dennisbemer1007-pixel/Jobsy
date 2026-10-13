using System.Collections.Concurrent;

namespace Jobsy.Core.Geo;

/// <summary>Short memory cache so repeated address lookups do not hit Nominatim again.</summary>
public sealed class GeoLookupCache
{
    private readonly ConcurrentDictionary<string, Entry> _items = new(StringComparer.Ordinal);

    public bool TryGet<T>(string key, out T? value)
    {
        value = default;
        if (!_items.TryGetValue(key, out var entry) || entry.ExpiresUtc <= DateTime.UtcNow)
        {
            return false;
        }

        if (entry.Value is T typed)
        {
            value = typed;
            return true;
        }

        return false;
    }

    public void Set<T>(string key, T? value, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(key) || ttl <= TimeSpan.Zero)
        {
            return;
        }

        _items[key] = new Entry(value, DateTime.UtcNow.Add(ttl));
    }

    private sealed record Entry(object? Value, DateTime ExpiresUtc);
}
