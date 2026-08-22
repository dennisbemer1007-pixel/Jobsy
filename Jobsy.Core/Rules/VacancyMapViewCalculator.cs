using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Derives a stable MapLibre opening view from active vacancy coordinates.
/// Zoom thresholds must stay in sync with <c>zoomForPoints</c> in <c>jobMap.js</c>.
/// </summary>
public static class VacancyMapViewCalculator
{
    public static VacancyMapView Fallback { get; } = new(52.15, 5.2913, 7, 0);

    /// <summary>
    /// Fallback zoom that fits the default 30-min fiets ring on a typical viewport.
    /// Keep in sync with <c>FILLED_LOCATION_ZOOM</c> in <c>jobMap.js</c>.
    /// </summary>
    public const int FilledLocationZoom = 12;

    /// <summary>Padding so the outer travel ring (and its label) stay inside the viewport.</summary>
    public const double TravelRingPaddingFactor = 1.45;

    public static VacancyMapView? ForFilledLocation(
        double lat,
        double lng,
        int pinCount = 0,
        int maxMinutes = 30,
        string? transport = "Fiets",
        double? radiusKm = 15)
    {
        if (!double.IsFinite(lat) || !double.IsFinite(lng)
            || lat is < -90 or > 90 || lng is < -180 or > 180)
        {
            return null;
        }

        return new VacancyMapView(
            Math.Round(lat, 5, MidpointRounding.AwayFromZero),
            Math.Round(lng, 5, MidpointRounding.AwayFromZero),
            ZoomForTravelRing(maxMinutes, transport, radiusKm),
            Math.Max(0, pinCount));
    }

    /// <summary>
    /// Opening camera for the banenkaart, computed before HTML leaves the server.
    /// Address / default-region wins (zoom fits the travel ring); otherwise the marker-centroid view.
    /// Company deep-links keep the pin camera.
    /// </summary>
    public static VacancyMapView ResolveOpening(
        VacancyMapView pinCentroid,
        double? originLat,
        double? originLng,
        double? regionLat,
        double? regionLng,
        bool companyFocus,
        int maxMinutes = 30,
        string? transport = "Fiets",
        double? radiusKm = 15)
    {
        if (companyFocus)
        {
            return pinCentroid;
        }

        var fromOrigin = originLat is double oLat && originLng is double oLng
            ? ForFilledLocation(oLat, oLng, pinCentroid.PinCount, maxMinutes, transport, radiusKm)
            : null;
        if (fromOrigin is not null)
        {
            return fromOrigin;
        }

        var fromRegion = regionLat is double rLat && regionLng is double rLng
            ? ForFilledLocation(rLat, rLng, pinCentroid.PinCount, maxMinutes, transport, radiusKm)
            : null;
        return fromRegion ?? pinCentroid;
    }

    /// <summary>
    /// Zoom that keeps the outer travel-time ring fully visible, matching jobMap.js ring radius.
    /// </summary>
    public static int ZoomForTravelRing(int maxMinutes = 30, string? transport = "Fiets", double? radiusKm = 15)
    {
        if (!TransportModeParser.TryParseMany(transport, out var mode, out _) || mode == TransportMode.None)
        {
            mode = TransportMode.Bike;
        }

        var meters = TravelReach.RingRadiusMeters(mode, maxMinutes);
        if (radiusKm is > 0)
        {
            meters = Math.Min(meters, radiusKm.Value * 1000.0);
        }

        var paddedSpanDeg = meters * 2 * TravelRingPaddingFactor / 1000.0 / 111.32;
        return ZoomForSpan(paddedSpanDeg);
    }

    public static VacancyMapView FromRecords(IEnumerable<VacancyDiscoveryRecord> records)
        => FromPoints(records.Select(r => (r.Latitude, r.Longitude)));

    public static VacancyMapView FromPoints(IEnumerable<(double Lat, double Lng)> points)
    {
        double sumLat = 0;
        double sumLng = 0;
        var minLat = 90d;
        var maxLat = -90d;
        var minLng = 180d;
        var maxLng = -180d;
        var count = 0;

        foreach (var (lat, lng) in points)
        {
            if (!double.IsFinite(lat) || !double.IsFinite(lng))
            {
                continue;
            }

            if (lat is < -90 or > 90 || lng is < -180 or > 180)
            {
                continue;
            }

            count++;
            sumLat += lat;
            sumLng += lng;
            minLat = Math.Min(minLat, lat);
            maxLat = Math.Max(maxLat, lat);
            minLng = Math.Min(minLng, lng);
            maxLng = Math.Max(maxLng, lng);
        }

        if (count == 0)
        {
            return Fallback;
        }

        var zoom = ZoomForSpan(Math.Max(maxLng - minLng, maxLat - minLat));
        return new VacancyMapView(
            Math.Round(sumLat / count, 5, MidpointRounding.AwayFromZero),
            Math.Round(sumLng / count, 5, MidpointRounding.AwayFromZero),
            zoom,
            count);
    }

    public static int ZoomForSpan(double span)
    {
        if (span < 0.08) return 13;
        if (span < 0.2) return 12;
        if (span < 0.5) return 11;
        if (span < 1.2) return 10;
        if (span < 2.5) return 9;
        return 8;
    }
}
