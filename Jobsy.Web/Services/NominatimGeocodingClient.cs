using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

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
public sealed class NominatimGeocodingClient(HttpClient http) : IGeocodingClient
{
    private static readonly Uri SuggestBase = new("https://nominatim.openstreetmap.org/search");
    private static readonly Uri ReverseBase = new("https://nominatim.openstreetmap.org/reverse");

    public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var q = query?.Trim() ?? string.Empty;
        if (q.Length < 3)
        {
            return [];
        }

        var url = $"{SuggestBase}?q={Uri.EscapeDataString(q)}"
            + "&format=json"
            + "&addressdetails=0"
            + "&countrycodes=nl"
            + "&limit=6"
            + "&accept-language=nl";

        var results = await http.GetFromJsonAsync<List<NominatimPlace>>(url, cancellationToken)
            ?? [];

        return results
            .Where(r => !string.IsNullOrWhiteSpace(r.DisplayName)
                        && double.TryParse(r.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                        && double.TryParse(r.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(r => new AddressSuggestion(
                r.DisplayName!,
                double.Parse(r.Lat!, CultureInfo.InvariantCulture),
                double.Parse(r.Lon!, CultureInfo.InvariantCulture)))
            .GroupBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public async Task<string?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var url = $"{ReverseBase}?lat={latitude.ToString(CultureInfo.InvariantCulture)}"
            + $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}"
            + "&format=json"
            + "&zoom=18"
            + "&addressdetails=0"
            + "&accept-language=nl";

        var place = await http.GetFromJsonAsync<NominatimPlace>(url, cancellationToken);
        return string.IsNullOrWhiteSpace(place?.DisplayName) ? null : place.DisplayName.Trim();
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
