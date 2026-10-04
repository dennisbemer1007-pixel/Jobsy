using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Geo;

namespace Jobsy.Web.Services;

public sealed record AddressSuggestion(string Label, double Latitude, double Longitude);

public interface IGeocodingClient
{
    Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default);
    Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}

/// <summary>
/// OpenStreetMap Nominatim geocoding (NL-focused). Server-side to satisfy Nominatim User-Agent policy.
/// </summary>
public sealed class NominatimGeocodingClient : IGeocodingClient
{
    private static readonly Uri SuggestBase = new("https://nominatim.openstreetmap.org/search");
    private static readonly Uri ReverseBase = new("https://nominatim.openstreetmap.org/reverse");
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly HttpClient _http;
    private readonly NominatimPace _pace;
    private readonly GeoLookupCache _cache;

    public NominatimGeocodingClient(HttpClient http, NominatimPace? pace = null, GeoLookupCache? cache = null)
    {
        _http = http;
        _pace = pace ?? new NominatimPace();
        _cache = cache ?? new GeoLookupCache();
    }

    public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var q = query?.Trim() ?? string.Empty;
        if (q.Length < 3)
        {
            return [];
        }

        var cacheKey = "web-suggest:" + q;
        if (_cache.TryGet(cacheKey, out IReadOnlyList<AddressSuggestion>? cached) && cached is not null)
        {
            return cached;
        }

        await _pace.WaitAsync(cancellationToken);

        // Address layer first (file 03): prefer BAG-style addresses over POIs.
        var url = $"{SuggestBase}?q={Uri.EscapeDataString(q)}"
            + "&format=json"
            + "&addressdetails=1"
            + "&layer=address"
            + "&countrycodes=nl"
            + "&limit=8"
            + "&accept-language=nl";

        var results = await _http.GetFromJsonAsync<List<NominatimPlace>>(url, cancellationToken)
            ?? [];

        IReadOnlyList<AddressSuggestion> mapped = results
            .Where(r => !string.IsNullOrWhiteSpace(r.DisplayName)
                        && double.TryParse(r.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                        && double.TryParse(r.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .OrderByDescending(HasHouseNumber)
            .Select(r => new AddressSuggestion(
                FormatLabel(r) ?? r.DisplayName!,
                double.Parse(r.Lat!, CultureInfo.InvariantCulture),
                double.Parse(r.Lon!, CultureInfo.InvariantCulture)))
            .GroupBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(6)
            .ToList();
        _cache.Set(cacheKey, mapped, CacheTtl);
        return mapped;
    }

    private static bool HasHouseNumber(NominatimPlace place) =>
        !string.IsNullOrWhiteSpace(place.Address?.HouseNumber);

    public async Task<string?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var label = await ReverseOnceAsync(latitude, longitude, cancellationToken);
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        // Dutch addresses are sometimes stored with lat/lng swapped (lng ≈ 4–7, lat ≈ 50–54).
        if (LooksLikeSwappedNetherlands(latitude, longitude))
        {
            return await ReverseOnceAsync(longitude, latitude, cancellationToken);
        }

        return null;
    }

    private async Task<string?> ReverseOnceAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var url = $"{ReverseBase}?lat={latitude.ToString(CultureInfo.InvariantCulture)}"
            + $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}"
            + "&format=json"
            + "&zoom=18"
            + "&addressdetails=1"
            + "&accept-language=nl";

        var cacheKey = "web-reverse:"
            + latitude.ToString("0.00000", CultureInfo.InvariantCulture)
            + ","
            + longitude.ToString("0.00000", CultureInfo.InvariantCulture);
        if (_cache.TryGet(cacheKey, out string? cached))
        {
            return cached;
        }

        await _pace.WaitAsync(cancellationToken);
        using var response = await _http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var place = await response.Content.ReadFromJsonAsync<NominatimPlace>(cancellationToken: cancellationToken);
        var label = FormatLabel(place);
        if (!string.IsNullOrWhiteSpace(label))
        {
            _cache.Set(cacheKey, label, CacheTtl);
        }

        return label;
    }

    private static bool LooksLikeSwappedNetherlands(double latitude, double longitude) =>
        latitude is >= 3 and <= 8
        && longitude is >= 50 and <= 54;

    private static string? FormatLabel(NominatimPlace? place)
    {
        if (place is null)
        {
            return null;
        }

        var address = place.Address;
        if (address is not null)
        {
            var street = JoinParts(address.Road, address.HouseNumber);
            var city = FirstNonEmpty(
                address.City,
                address.Town,
                address.Village,
                address.Municipality,
                address.Suburb);
            var locality = JoinParts(address.Postcode, city);

            if (!string.IsNullOrWhiteSpace(street) && !string.IsNullOrWhiteSpace(locality))
            {
                return $"{street}, {locality}";
            }

            if (!string.IsNullOrWhiteSpace(street))
            {
                return street;
            }

            if (!string.IsNullOrWhiteSpace(locality))
            {
                return locality;
            }
        }

        return string.IsNullOrWhiteSpace(place.DisplayName) ? null : CompactDisplayName(place.DisplayName);
    }

    private static string CompactDisplayName(string displayName)
    {
        // Nominatim often repeats "Nederland"; keep the useful head of the label.
        var parts = displayName
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.Equals("Nederland", StringComparison.OrdinalIgnoreCase)
                        && !p.Equals("Netherlands", StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToArray();
        return parts.Length == 0 ? displayName.Trim() : string.Join(", ", parts);
    }

    private static string? JoinParts(params string?[] parts)
    {
        var values = parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()).ToArray();
        return values.Length == 0 ? null : string.Join(" ", values);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private sealed class NominatimPlace
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("lat")]
        public string? Lat { get; set; }

        [JsonPropertyName("lon")]
        public string? Lon { get; set; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("house_number")]
        public string? HouseNumber { get; set; }

        [JsonPropertyName("road")]
        public string? Road { get; set; }

        [JsonPropertyName("suburb")]
        public string? Suburb { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("postcode")]
        public string? Postcode { get; set; }
    }
}
