using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Geo;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Nominatim-backed geocoder for registration manual address entry (NL).
/// When PDOK lands with the intermediair stack, swap the implementation behind IGeocodingService.
/// </summary>
public sealed class NominatimGeocodingService : IGeocodingService
{
    private static readonly Uri SuggestBase = new("https://nominatim.openstreetmap.org/search");

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly HttpClient _http;
    private readonly ILogger<NominatimGeocodingService> _logger;
    private readonly NominatimPace _pace;
    private readonly GeoLookupCache _cache;

    public NominatimGeocodingService(
        HttpClient http,
        ILogger<NominatimGeocodingService> logger,
        NominatimPace? pace = null,
        GeoLookupCache? cache = null)
    {
        _http = http;
        _logger = logger;
        _pace = pace ?? new NominatimPace();
        _cache = cache ?? new GeoLookupCache();
    }

    public async Task<GeocodingResult?> GeocodeAsync(
        string addressQuery,
        CancellationToken cancellationToken = default)
    {
        var q = addressQuery?.Trim() ?? string.Empty;
        if (q.Length < 3)
        {
            return null;
        }

        var cacheKey = "api-geocode:" + q;
        if (_cache.TryGet(cacheKey, out GeocodingResult? cached))
        {
            return cached;
        }

        try
        {
            await _pace.WaitAsync(cancellationToken);
            var url = $"{SuggestBase}?q={Uri.EscapeDataString(q)}"
                      + "&format=json"
                      + "&addressdetails=0"
                      + "&countrycodes=nl"
                      + "&limit=1"
                      + "&accept-language=nl";

            var results = await _http.GetFromJsonAsync<List<NominatimPlace>>(url, cancellationToken)
                          ?? [];
            var first = results.FirstOrDefault(r =>
                double.TryParse(r.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                && double.TryParse(r.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out _));
            if (first is null)
            {
                return null;
            }

            var found = new GeocodingResult(
                double.Parse(first.Lat!, CultureInfo.InvariantCulture),
                double.Parse(first.Lon!, CultureInfo.InvariantCulture),
                first.DisplayName);
            _cache.Set(cacheKey, found, CacheTtl);
            return found;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Geocode failed for manual registration address");
            return null;
        }
    }

    private sealed class NominatimPlace
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("lat")]
        public string? Lat { get; set; }

        [JsonPropertyName("lon")]
        public string? Lon { get; set; }
    }
}
