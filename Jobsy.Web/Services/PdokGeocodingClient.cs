using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Jobsy.Web.Services;

/// <summary>
/// PDOK Locatieserver free text search — official BAG addresses, no API key (file 03 / D5).
/// </summary>
public sealed class PdokGeocodingClient(HttpClient http)
{
    private static readonly Uri SuggestBase =
        new("https://api.pdok.nl/bzk/locatieserver/search/v3_1/free");

    private static readonly Dictionary<string, int> TypeRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["adres"] = 0,
        ["postcode"] = 1,
        ["weg"] = 2,
        ["woonplaats"] = 3,
    };

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
            + "&fq=" + Uri.EscapeDataString("type:(adres OR postcode OR weg OR woonplaats)")
            + "&rows=8"
            + "&fl=weergavenaam,type,centroide_ll,score,postcode,woonplaatsnaam";

        using var response = await http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<PdokResponse>(cancellationToken: cancellationToken);
        var docs = payload?.Response?.Docs ?? [];

        return docs
            .Select(ParseDoc)
            .Where(s => s is not null)
            .Cast<(AddressSuggestion Suggestion, string Type, double Score)>()
            .OrderBy(x => TypeRank.TryGetValue(x.Type, out var rank) ? rank : 99)
            .ThenByDescending(x => x.Score)
            .Select(x => x.Suggestion)
            .GroupBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(6)
            .ToList();
    }

    public static IReadOnlyList<AddressSuggestion> OrderFixtureDocs(
        IEnumerable<(string Label, string Type, double Lon, double Lat, double Score)> docs)
    {
        return docs
            .OrderBy(x => TypeRank.TryGetValue(x.Type, out var rank) ? rank : 99)
            .ThenByDescending(x => x.Score)
            .Select(x => new AddressSuggestion(x.Label, x.Lat, x.Lon))
            .GroupBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(6)
            .ToList();
    }

    private static (AddressSuggestion Suggestion, string Type, double Score)? ParseDoc(PdokDoc doc)
    {
        if (string.IsNullOrWhiteSpace(doc.Weergavenaam) || string.IsNullOrWhiteSpace(doc.CentroideLl))
        {
            return null;
        }

        var match = Regex.Match(
            doc.CentroideLl,
            @"POINT\s*\(\s*(?<lon>-?\d+(?:\.\d+)?)\s+(?<lat>-?\d+(?:\.\d+)?)\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        if (!double.TryParse(match.Groups["lon"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)
            || !double.TryParse(match.Groups["lat"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
        {
            return null;
        }

        var type = doc.Type?.Trim() ?? string.Empty;
        var label = LabelFor(doc, type);
        return (new AddressSuggestion(label, lat, lon), type, doc.Score);
    }

    private sealed class PdokResponse
    {
        [JsonPropertyName("response")]
        public PdokResponseBody? Response { get; set; }
    }

    private sealed class PdokResponseBody
    {
        [JsonPropertyName("docs")]
        public List<PdokDoc> Docs { get; set; } = [];
    }

    private sealed class PdokDoc
    {
        [JsonPropertyName("weergavenaam")]
        public string? Weergavenaam { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("centroide_ll")]
        public string? CentroideLl { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("postcode")]
        public string? Postcode { get; set; }

        [JsonPropertyName("woonplaatsnaam")]
        public string? Woonplaatsnaam { get; set; }
    }

    /// <summary>
    /// Postcode docs use a street in <c>weergavenaam</c> ("Stationsplein, 1012AB Amsterdam").
    /// Rewrite those to "1012AB Amsterdam" so the wizard can split code and city.
    /// </summary>
    private static string LabelFor(PdokDoc doc, string type)
    {
        var display = doc.Weergavenaam!.Trim();
        if (!type.Equals("postcode", StringComparison.OrdinalIgnoreCase))
        {
            return display;
        }

        var code = PostcodeMatch.Compact(doc.Postcode);
        var city = doc.Woonplaatsnaam?.Trim();
        return code is not null && !string.IsNullOrWhiteSpace(city)
            ? code + " " + city
            : display;
    }
}
