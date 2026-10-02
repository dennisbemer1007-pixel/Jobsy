using System.Collections.Concurrent;

namespace Jobsy.Web.Services;

/// <summary>
/// PDOK first, Nominatim (address layer) fallback. Reverse stays on Nominatim.
/// In-memory suggestion cache per normalised query for 24 h.
/// </summary>
public sealed class CompositeGeocodingClient(
    PdokGeocodingClient pdok,
    NominatimGeocodingClient nominatim) : IGeocodingClient
{
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.Ordinal);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var key = Normalize(query);
        if (key.Length < 3)
        {
            return [];
        }

        if (Cache.TryGetValue(key, out var hit) && hit.ExpiresAtUtc > DateTime.UtcNow)
        {
            return hit.Suggestions;
        }

        IReadOnlyList<AddressSuggestion> results;
        try
        {
            results = await pdok.SuggestAsync(query, cancellationToken);
        }
        catch
        {
            results = [];
        }

        if (results.Count == 0)
        {
            try
            {
                results = await nominatim.SuggestAsync(query, cancellationToken);
            }
            catch
            {
                results = [];
            }
        }

        if (results.Count > 0)
        {
            Cache[key] = new CacheEntry(results, DateTime.UtcNow.Add(CacheTtl));
        }

        return results;
    }

    public Task<string?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
        => nominatim.ReverseAsync(latitude, longitude, cancellationToken);

    private static string Normalize(string? query) =>
        string.Join(' ', (query ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private sealed record CacheEntry(IReadOnlyList<AddressSuggestion> Suggestions, DateTime ExpiresAtUtc);
}
