using Jobsy.Core.Admin;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.Services.AdminTodo;

/// <summary>
/// Merges all <see cref="IAdminTodoSource"/>s, sorts danger→warn→info then oldest first,
/// caches 60s, exposes nav-key counts for the sidebar.
/// </summary>
public sealed class AdminTodoService : IAdminTodoService
{
    public const string CacheKey = "admin:todo:snapshot";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private readonly IEnumerable<IAdminTodoSource> _sources;
    private readonly IMemoryCache _cache;

    // Slot (not built — README Scope): AVG request queue, background-job failures.

    public AdminTodoService(IEnumerable<IAdminTodoSource> sources, IMemoryCache cache)
    {
        _sources = sources;
        _cache = cache;
    }

    public async Task<AdminTodoSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out AdminTodoSnapshot? hit) && hit is not null)
        {
            return hit;
        }

        var all = new List<AdminTodoItem>();
        foreach (var source in _sources)
        {
            var items = await source.GetAsync(cancellationToken);
            all.AddRange(items);
        }

        var ordered = all
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.SinceUtc)
            .ToList();

        var counts = BuildCounts(ordered);
        var snapshot = new AdminTodoSnapshot(ordered, counts);
        _cache.Set(CacheKey, snapshot, CacheDuration);
        return snapshot;
    }

    public void Invalidate() => _cache.Remove(CacheKey);

    internal static IReadOnlyDictionary<string, int> BuildCounts(IReadOnlyList<AdminTodoItem> items)
    {
        var todo = items.Sum(i => Math.Max(1, i.Count));
        var org = items
            .Where(i => i.Key.StartsWith("kvk-failed", StringComparison.Ordinal)
                        || i.Key.StartsWith("takeovers", StringComparison.Ordinal))
            .Sum(i => Math.Max(1, i.Count));
        var mod = items
            .Where(i => i.Key.Equals("moderation", StringComparison.Ordinal)
                        || i.Key.StartsWith("moderation:", StringComparison.Ordinal))
            .Sum(i => Math.Max(1, i.Count));
        var payouts = items
            .Where(i => i.Key.Equals("open-payouts", StringComparison.Ordinal)
                        || i.Key.StartsWith("open-payouts:", StringComparison.Ordinal))
            .Sum(i => Math.Max(1, i.Count));
        var feedback = items
            .Where(i => i.Key.Equals("feedback", StringComparison.Ordinal)
                        || i.Key.StartsWith("feedback:", StringComparison.Ordinal))
            .Sum(i => Math.Max(1, i.Count));

        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [AdminTodoNavKeys.Todo] = todo,
            [AdminTodoNavKeys.OrgRequests] = org,
            [AdminTodoNavKeys.Moderation] = mod,
            [AdminTodoNavKeys.Payouts] = payouts,
            [AdminTodoNavKeys.Feedback] = feedback
        };
    }
}
