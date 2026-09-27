using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Valhalla <c>/isochrone</c> provider. Cached ≥7 days; fair-use public instance by default.
/// </summary>
public sealed class ValhallaIsochroneService : IIsochroneService
{
    public const string HttpClientName = "ValhallaIsochrone";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    private readonly IHttpClientFactory _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ValhallaIsochroneService> _logger;
    private readonly string _baseUrl;

    public ValhallaIsochroneService(
        IHttpClientFactory http,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<ValhallaIsochroneService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
        _baseUrl = TrimBase(
            configuration["Routing:IsochroneBaseUrl"],
            "https://valhalla1.openstreetmap.de");
    }

    public static void ConfigureClient(HttpClient client, TimeSpan timeout)
    {
        client.Timeout = timeout;
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LobsyJobsy/1.0 (+https://lobsy.nl; isochrones)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string?> TryGetIsochronesAsync(
        double latitude,
        double longitude,
        TransportMode mode,
        IReadOnlyList<int> minutes,
        CancellationToken cancellationToken = default)
    {
        if (!IsFinite(latitude) || !IsFinite(longitude) || minutes is null || minutes.Count == 0)
        {
            return null;
        }

        // Public FOSSGIS instance has no transit costing — OV falls back to circles client-side.
        if (mode.HasFlag(TransportMode.PublicTransport))
        {
            return null;
        }

        var costing = ToCosting(mode);
        if (costing is null)
        {
            return null;
        }

        var ordered = minutes
            .Where(m => m > 0 && m <= 120)
            .Distinct()
            .OrderByDescending(m => m)
            .ToList();
        if (ordered.Count == 0)
        {
            return null;
        }

        var lat3 = Math.Round(latitude, 3, MidpointRounding.AwayFromZero);
        var lng3 = Math.Round(longitude, 3, MidpointRounding.AwayFromZero);
        var key = $"iso:{lat3.ToString(CultureInfo.InvariantCulture)}:{lng3.ToString(CultureInfo.InvariantCulture)}:{costing}:{string.Join(',', ordered)}";
        if (_cache.TryGetValue(key, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        try
        {
            var payload = BuildRequest(lat3, lng3, costing, ordered);
            var client = _http.CreateClient(HttpClientName);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client
                .PostAsync(_baseUrl.TrimEnd('/') + "/isochrone", content, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Valhalla isochrone HTTP {Status} for {Lat},{Lng} {Costing}",
                    (int)response.StatusCode, lat3, lng3, costing);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var normalized = NormalizeFeatureCollection(doc.RootElement, ordered);
            if (normalized is null)
            {
                _logger.LogWarning("Valhalla isochrone returned no polygons for {Lat},{Lng}", lat3, lng3);
                return null;
            }

            _cache.Set(key, normalized, CacheTtl);
            return normalized;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException)
        {
            _logger.LogWarning(ex, "Valhalla isochrone failed for {Lat},{Lng} {Costing}", lat3, lng3, costing);
            return null;
        }
    }

    private static string BuildRequest(double lat, double lng, string costing, IReadOnlyList<int> minutes)
    {
        var contours = minutes
            .OrderBy(m => m)
            .Select(m => new { time = m })
            .ToArray();
        var body = new
        {
            locations = new[] { new { lat, lon = lng } },
            costing,
            contours,
            polygons = true,
            denoise = 0.5,
            generalize = 50
        };
        return JsonSerializer.Serialize(body);
    }

    private static string? NormalizeFeatureCollection(JsonElement root, IReadOnlyList<int> orderedMinutes)
    {
        if (!root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var byMinutes = new Dictionary<int, JsonElement>();
        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("properties", out var props))
            {
                continue;
            }

            var mins = ReadMinutes(props);
            if (mins is null)
            {
                continue;
            }

            if (!feature.TryGetProperty("geometry", out var geom) || geom.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            byMinutes[mins.Value] = feature;
        }

        if (byMinutes.Count == 0)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WritePropertyName("features");
            writer.WriteStartArray();
            foreach (var mins in orderedMinutes)
            {
                if (!byMinutes.TryGetValue(mins, out var feature))
                {
                    continue;
                }

                writer.WriteStartObject();
                writer.WriteString("type", "Feature");
                writer.WritePropertyName("properties");
                writer.WriteStartObject();
                writer.WriteNumber("minutes", mins);
                writer.WriteEndObject();
                writer.WritePropertyName("geometry");
                feature.GetProperty("geometry").WriteTo(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static int? ReadMinutes(JsonElement props)
    {
        if (props.TryGetProperty("contour", out var contour) && contour.TryGetInt32(out var c))
        {
            return c;
        }

        if (props.TryGetProperty("minutes", out var minutes) && minutes.TryGetInt32(out var m))
        {
            return m;
        }

        if (props.TryGetProperty("time", out var time) && time.TryGetInt32(out var t))
        {
            return t;
        }

        return null;
    }

    private static string? ToCosting(TransportMode mode)
    {
        if (mode.HasFlag(TransportMode.Bike)) return "bicycle";
        if (mode.HasFlag(TransportMode.Car)) return "auto";
        if (mode.HasFlag(TransportMode.Walking)) return "pedestrian";
        return null;
    }

    private static string TrimBase(string? configured, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
        return value.TrimEnd('/');
    }

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
