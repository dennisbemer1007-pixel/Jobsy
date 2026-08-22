using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Exact vacancy-detail routing: OSRM for bike/car/walk, MOTIS (Transitous) for OV.
/// </summary>
public sealed class OsrmRoutingService : IExactRoutingService
{
    public const string OsrmClientName = "OsrmRouting";
    public const string TransitClientName = "MotisTransit";

    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.Ordinal);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly IHttpClientFactory _http;
    private readonly ILogger<OsrmRoutingService> _logger;
    private readonly string _osrmBase;
    private readonly string _transitPlanUrl;

    public OsrmRoutingService(
        IHttpClientFactory http,
        IConfiguration configuration,
        ILogger<OsrmRoutingService> logger)
    {
        _http = http;
        _logger = logger;
        _osrmBase = TrimBase(configuration["Routing:OsrmBaseUrl"], "https://router.project-osrm.org");
        var transitBase = TrimBase(configuration["Routing:TransitBaseUrl"], "https://api.transitous.org");
        var planPath = configuration["Routing:TransitPlanPath"];
        if (string.IsNullOrWhiteSpace(planPath))
        {
            planPath = "/api/v5/plan";
        }

        if (!planPath.StartsWith('/'))
        {
            planPath = "/" + planPath;
        }

        _transitPlanUrl = transitBase + planPath;
    }

    public async Task<RouteResult?> TryGetRouteAsync(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        TransportMode transportMode,
        CancellationToken cancellationToken = default)
    {
        if (!IsFinitePoint(fromLatitude, fromLongitude) || !IsFinitePoint(toLatitude, toLongitude))
        {
            return null;
        }

        var mode = NormalizeMode(transportMode);
        var cacheKey = CacheKey(fromLatitude, fromLongitude, toLatitude, toLongitude, mode);
        if (Cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresUtc > DateTime.UtcNow)
        {
            return cached.Result;
        }

        RouteResult? result = null;
        try
        {
            result = mode == TransportMode.PublicTransport
                ? await GetTransitAsync(fromLatitude, fromLongitude, toLatitude, toLongitude, cancellationToken)
                    .ConfigureAwait(false)
                : await GetOsrmAsync(fromLatitude, fromLongitude, toLatitude, toLongitude, mode, cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Exact routing failed for {Mode}.", mode);
        }

        if (result is not null)
        {
            Cache[cacheKey] = new CacheEntry(result, DateTime.UtcNow.Add(CacheTtl));
        }

        return result;
    }

    private async Task<RouteResult?> GetOsrmAsync(
        double fromLat,
        double fromLng,
        double toLat,
        double toLng,
        TransportMode mode,
        CancellationToken cancellationToken)
    {
        var profile = mode switch
        {
            TransportMode.Car => "driving",
            TransportMode.Walking => "walking",
            _ => "cycling"
        };

        var inv = CultureInfo.InvariantCulture;
        var url =
            $"{_osrmBase}/route/v1/{profile}/" +
            $"{fromLng.ToString("0.######", inv)},{fromLat.ToString("0.######", inv)};" +
            $"{toLng.ToString("0.######", inv)},{toLat.ToString("0.######", inv)}" +
            "?overview=false&alternatives=false";

        var client = _http.CreateClient(OsrmClientName);
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OSRM {Mode} returned {Status}.", mode, (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = doc.RootElement;
        if (!root.TryGetProperty("code", out var codeEl)
            || !string.Equals(codeEl.GetString(), "Ok", StringComparison.OrdinalIgnoreCase)
            || !root.TryGetProperty("routes", out var routes)
            || routes.ValueKind != JsonValueKind.Array
            || routes.GetArrayLength() == 0)
        {
            return null;
        }

        var route = routes[0];
        if (!route.TryGetProperty("distance", out var distEl)
            || !route.TryGetProperty("duration", out var durEl))
        {
            return null;
        }

        var distance = distEl.GetDouble();
        var duration = durEl.GetDouble();
        if (!double.IsFinite(distance) || !double.IsFinite(duration) || distance < 0 || duration < 0)
        {
            return null;
        }

        return new RouteResult(distance, duration, mode);
    }

    private async Task<RouteResult?> GetTransitAsync(
        double fromLat,
        double fromLng,
        double toLat,
        double toLng,
        CancellationToken cancellationToken)
    {
        var inv = CultureInfo.InvariantCulture;
        var url =
            _transitPlanUrl
            + "?fromPlace=" + Uri.EscapeDataString($"{fromLat.ToString("0.######", inv)},{fromLng.ToString("0.######", inv)}")
            + "&toPlace=" + Uri.EscapeDataString($"{toLat.ToString("0.######", inv)},{toLng.ToString("0.######", inv)}")
            + "&numItineraries=1";

        var client = _http.CreateClient(TransitClientName);
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MOTIS transit returned {Status}.", (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!TryReadBestItinerary(doc.RootElement, out var durationSeconds, out var distanceMeters))
        {
            return null;
        }

        return new RouteResult(distanceMeters, durationSeconds, TransportMode.PublicTransport);
    }

    internal static bool TryReadBestItinerary(JsonElement root, out double durationSeconds, out double distanceMeters)
    {
        durationSeconds = 0;
        distanceMeters = 0;
        JsonElement? best = TryFastest(root, "itineraries") ?? TryFastest(root, "direct");
        if (best is null)
        {
            return false;
        }

        durationSeconds = best.Value.GetProperty("duration").GetDouble();
        distanceMeters = SumItineraryDistanceMeters(best.Value);
        return durationSeconds >= 0 && distanceMeters > 0;
    }

    private static JsonElement? TryFastest(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        JsonElement? best = null;
        var bestDuration = double.MaxValue;
        foreach (var itinerary in list.EnumerateArray())
        {
            if (!itinerary.TryGetProperty("duration", out var durEl) || durEl.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var duration = durEl.GetDouble();
            if (!double.IsFinite(duration) || duration < 0 || duration >= bestDuration)
            {
                continue;
            }

            bestDuration = duration;
            best = itinerary;
        }

        return best;
    }

    internal static double SumItineraryDistanceMeters(JsonElement itinerary)
    {
        if (!itinerary.TryGetProperty("legs", out var legs) || legs.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var meters = 0.0;
        foreach (var leg in legs.EnumerateArray())
        {
            if (leg.TryGetProperty("distance", out var distEl)
                && distEl.ValueKind == JsonValueKind.Number
                && distEl.GetDouble() is var d
                && double.IsFinite(d)
                && d > 0)
            {
                meters += d;
                continue;
            }

            if (leg.TryGetProperty("legGeometry", out var geom) && geom.ValueKind == JsonValueKind.Object)
            {
                var precision = 6;
                if (geom.TryGetProperty("precision", out var precEl)
                    && precEl.ValueKind == JsonValueKind.Number)
                {
                    precision = precEl.GetInt32();
                }

                if (geom.TryGetProperty("points", out var pointsEl))
                {
                    meters += EncodedPolyline.LengthMeters(pointsEl.GetString(), precision);
                }
            }
        }

        return meters;
    }

    private static string CacheKey(double fromLat, double fromLng, double toLat, double toLng, TransportMode mode)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{mode}:{Math.Round(fromLat, 5)}:{Math.Round(fromLng, 5)}:{Math.Round(toLat, 5)}:{Math.Round(toLng, 5)}");

    private static string TrimBase(string? value, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return text.TrimEnd('/');
    }

    private static bool IsFinitePoint(double lat, double lng)
        => double.IsFinite(lat)
           && double.IsFinite(lng)
           && Math.Abs(lat) <= 90
           && Math.Abs(lng) <= 180
           && !(lat == 0 && lng == 0);

    private static TransportMode NormalizeMode(TransportMode mode)
    {
        if (mode.HasFlag(TransportMode.Bike)) return TransportMode.Bike;
        if (mode.HasFlag(TransportMode.Car)) return TransportMode.Car;
        if (mode.HasFlag(TransportMode.PublicTransport)) return TransportMode.PublicTransport;
        if (mode.HasFlag(TransportMode.Walking)) return TransportMode.Walking;
        return TransportMode.Bike;
    }

    private readonly record struct CacheEntry(RouteResult Result, DateTime ExpiresUtc);

    public static void ConfigureClient(HttpClient client, TimeSpan timeout)
    {
        client.Timeout = timeout;
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Lobsy/1.0 (+https://lobsy.nl)");
    }
}
