using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Rules;

/// <summary>
/// Rough travel-time → distance bounds used to prefilter candidates before expensive routing.
/// Speeds stay aligned with <c>MockRoutingService</c> / jobMap.js.
/// </summary>
public static class TravelReach
{
    /// <summary>On-road cruise speed (km/h), not crow-flies.</summary>
    public static double SpeedKmPerHour(TransportMode mode)
    {
        if (mode.HasFlag(TransportMode.Bike)) return 18.0;
        if (mode.HasFlag(TransportMode.Car)) return 40.0;
        if (mode.HasFlag(TransportMode.PublicTransport)) return 25.0;
        if (mode.HasFlag(TransportMode.Walking)) return 5.0;
        return 18.0;
    }

    /// <summary>
    /// Typical road-network distance / crow-flies. Bike trips in NL detour around water and rail;
    /// 1.7 keeps a 10-min fiets ring close to OSRM cycling time (a 3 km straight line used to
    /// sit inside that ring but is ~17 min on the bike network).
    /// Keep in sync with <c>ROAD_CIRCUITY</c> in jobMap.js.
    /// </summary>
    public static double RoadCircuity(TransportMode mode)
    {
        if (mode.HasFlag(TransportMode.Bike)) return 1.7;
        if (mode.HasFlag(TransportMode.Car)) return 1.35;
        if (mode.HasFlag(TransportMode.PublicTransport)) return 1.5;
        if (mode.HasFlag(TransportMode.Walking)) return 1.4;
        return 1.7;
    }

    /// <summary>Crow-flies km/h that yields the same minutes as cruise speed on a typical detour.</summary>
    public static double CrowFliesKmPerHour(TransportMode mode)
        => SpeedKmPerHour(mode) / RoadCircuity(mode);

    /// <summary>
    /// Crow-flies meters for a travel-time circle on the CV / job map (circuity applied —
    /// matches jobMap.js ring radius so a 10-min bike circle is not a 17-min ride).
    /// </summary>
    public static double RingRadiusMeters(TransportMode mode, int minutes)
    {
        minutes = Math.Clamp(minutes, 1, 180);
        return CrowFliesKmPerHour(mode) * 1000.0 / 60.0 * minutes;
    }

    /// <summary>
    /// Upper-bound crow-flies km that could still finish within <paramref name="maxMinutes"/>
    /// (circuity plus a small buffer so rounding does not drop edge pins).
    /// </summary>
    public static double MaxCrowFliesKm(TransportMode mode, int maxMinutes, double? radiusKm = null)
    {
        maxMinutes = Math.Clamp(maxMinutes, 1, 180);
        var fromTime = CrowFliesKmPerHour(mode) * (maxMinutes / 60.0) * 1.25;
        if (radiusKm is > 0)
        {
            return Math.Min(fromTime, radiusKm.Value);
        }

        return fromTime;
    }

    /// <summary>
    /// Same Haversine + mode-speed + circuity estimate as <c>MockRoutingService</c> / discover.
    /// Synchronous so the banenkaart can filter the in-memory index without awaiting N routes.
    /// </summary>
    public static (int TravelMinutes, double DistanceKm) Estimate(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        TransportMode mode)
    {
        var distanceKm = GeoDistance.HaversineKm(
            new GeoPoint(fromLatitude, fromLongitude),
            new GeoPoint(toLatitude, toLongitude));
        var durationSeconds = (distanceKm * 1000.0) / (CrowFliesKmPerHour(mode) * 1000.0 / 3600.0);
        var travelMinutes = (int)Math.Ceiling(durationSeconds / 60.0);
        return (travelMinutes, Math.Round(distanceKm, 2));
    }
}
