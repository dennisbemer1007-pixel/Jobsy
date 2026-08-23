using System.Collections.Concurrent;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

public sealed class DashboardMemoryCache : IDashboardCache
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan DefaultIdleLimit = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, Entry> _items = new(StringComparer.Ordinal);
    private readonly TimeProvider _clock;

    public DashboardMemoryCache(TimeProvider? timeProvider = null)
    {
        _clock = timeProvider ?? TimeProvider.System;
    }

    public TimeSpan TimeToLive => DefaultTtl;

    public bool TryGet<T>(string key, out T? value) where T : class
    {
        var now = _clock.GetUtcNow();
        if (_items.TryGetValue(key, out var entry) && entry.ExpiresAt > now && entry.Value is T typed)
        {
            entry.LastAccessAt = now;
            value = typed;
            return true;
        }

        if (entry is not null && entry.ExpiresAt <= now)
        {
            _items.TryRemove(key, out _);
        }

        value = default;
        return false;
    }

    public void Set<T>(string key, T value, DashboardCacheDescriptor descriptor) where T : class
    {
        var now = _clock.GetUtcNow();
        _items[key] = new Entry
        {
            Value = value,
            Descriptor = descriptor,
            ExpiresAt = now + DefaultTtl,
            LastAccessAt = now
        };
    }

    public void Remove(string key) => _items.TryRemove(key, out _);

    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _items.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _items.TryRemove(key, out _);
            }
        }
    }

    public IReadOnlyList<(string Key, DashboardCacheDescriptor Descriptor)> GetTracked(TimeSpan maxIdle)
    {
        var now = _clock.GetUtcNow();
        var tracked = new List<(string, DashboardCacheDescriptor)>();
        foreach (var pair in _items)
        {
            if (now - pair.Value.LastAccessAt > maxIdle)
            {
                _items.TryRemove(pair.Key, out _);
                continue;
            }

            tracked.Add((pair.Key, pair.Value.Descriptor));
        }

        return tracked;
    }

    private sealed class Entry
    {
        public required object Value { get; init; }
        public required DashboardCacheDescriptor Descriptor { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
        public DateTimeOffset LastAccessAt { get; set; }
    }
}

public static class DashboardCacheKeys
{
    public static string Scope(IReadOnlyCollection<Guid>? companyIds)
    {
        if (companyIds is null)
        {
            return "all";
        }

        if (companyIds.Count == 0)
        {
            return "none";
        }

        return string.Join(',', companyIds.Distinct().OrderBy(id => id));
    }

    public static string Metrics(string scope, bool includePlatformOnly, string period) =>
        $"metrics:{scope}:{(includePlatformOnly ? "1" : "0")}:{NormalizePeriod(period)}";

    public static string Vacancy(string scope, string period, int take) =>
        $"vacancy:{scope}:{NormalizePeriod(period)}:{take}";

    public static string Client(string scope, string period) =>
        $"client:{scope}:{NormalizePeriod(period)}";

    public static string Sales(Guid userId) => $"sales:{userId:D}";

    public static string Ambassadeur(Guid userId) => $"ambassadeur:{userId:D}";

    public static string ScopePrefix(string scope) => $"{scope}:";

    public static string MetricsPrefix(string scope) => $"metrics:{scope}:";

    public static string VacancyPrefix(string scope) => $"vacancy:{scope}:";

    public static string ClientPrefix(string scope) => $"client:{scope}:";

    public static string NormalizePeriod(string? period)
    {
        var trimmed = period?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(trimmed) ? "week" : trimmed;
    }
}

public static class DashboardLiveMetricKeys
{
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        "applications_pending",
        "active_vacancies",
        "active_vacancies_employers",
        "active_vacancies_intermediaries",
        "active_boosts",
        "users_open_for_work",
        "users_active",
        "errors",
        "unpublished_vacancies"
    };
}
