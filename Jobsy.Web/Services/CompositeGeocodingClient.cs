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

        var normalized = PostcodeMatch.Normalize(query);
        var postcodeQuery = PostcodeMatch.IsNlPostcode(normalized);
        // PDOK can return street hits that do not identify the postcode. Nominatim
        // is the fallback when PDOK is empty or none of its labels match the code.
        if (results.Count == 0 || (postcodeQuery && !PostcodeMatch.HasLeadingMatch(results, normalized)))
        {
            try
            {
                var fallback = await nominatim.SuggestAsync(query, cancellationToken);
                if (fallback.Count > 0)
                {
                    results = fallback;
                }
            }
            catch
            {
                if (results.Count == 0)
                {
                    results = [];
                }
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
