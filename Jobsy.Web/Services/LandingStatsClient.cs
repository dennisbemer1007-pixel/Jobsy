using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Services;

/// <summary>
/// Fetches the one real landing number (active public vacancies) with a hard 300 ms timeout
/// and a 10-minute memory cache. Never throws to the page.
/// </summary>
public sealed class LandingStatsClient(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache)
{
    public const string HttpClientName = "JobsyLandingStats";
    public const string CacheKey = "Jobsy.Landing.ActiveVacancies";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromMilliseconds(300);

    /// <summary>Null when unavailable / timed out / below display threshold handling is left to the UI.</summary>
    public async Task<int?> GetActiveVacanciesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out int cached))
        {
            return cached;
        }

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(RequestTimeout);
            var http = httpClientFactory.CreateClient(HttpClientName);
            var dto = await http.GetFromJsonAsync<LandingStatsDto>("api/public/landing-stats", linked.Token);
            if (dto is null)
            {
                return null;
            }

            cache.Set(CacheKey, dto.ActiveVacancies, CacheDuration);
            return dto.ActiveVacancies;
        }
        catch
        {
            return null;
        }
    }

    private sealed record LandingStatsDto(int ActiveVacancies);
}

/// <summary>Formats D7: round down to tens; hide below 25.</summary>
public static class LandingVacancyCount
{
    public const int MinDisplay = 25;

    public static int? RoundDownToTens(int? raw)
    {
        if (raw is null || raw.Value < MinDisplay)
        {
            return null;
        }

        return (raw.Value / 10) * 10;
    }
}
