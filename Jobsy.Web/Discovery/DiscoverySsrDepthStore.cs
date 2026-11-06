using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Discovery;

/// <summary>Per-candidate depth choice on the shed (survives 303 without Blazor state).</summary>
internal sealed class DiscoverySsrDepthStore(IMemoryCache cache)
{
    private static string Key(string userId, int shedStep) => $"discovery-shed-depth:{userId}:{shedStep}";

    public int GetOrDefault(string userId, int shedStep, int fallback)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return fallback;
        }

        return cache.TryGetValue(Key(userId, shedStep), out int level) ? level : fallback;
    }

    public void Set(string userId, int shedStep, int level)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        cache.Set(Key(userId, shedStep), level, TimeSpan.FromHours(12));
    }
}
