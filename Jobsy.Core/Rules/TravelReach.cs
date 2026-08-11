using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Rough travel-time → distance bounds used to prefilter candidates before expensive routing.
/// Speeds stay aligned with <c>MockRoutingService</c> / jobMap.js.
/// </summary>
public static class TravelReach
{
    public static double SpeedKmPerHour(TransportMode mode)
    {
        if (mode.HasFlag(TransportMode.Bike)) return 18.0;
        if (mode.HasFlag(TransportMode.Car)) return 40.0;
        if (mode.HasFlag(TransportMode.PublicTransport)) return 25.0;
        if (mode.HasFlag(TransportMode.Walking)) return 5.0;
        return 18.0;
    }

    /// <summary>
    /// Crow-flies meters for a travel-time circle on the CV map (no routing buffer —
    /// matches jobMap.js ring radius so a 5-min bike circle stays readable when zoomed).
    /// </summary>
    public static double RingRadiusMeters(TransportMode mode, int minutes)
    {
        minutes = Math.Clamp(minutes, 1, 180);
        return SpeedKmPerHour(mode) * 1000.0 / 60.0 * minutes;
    }

    /// <summary>
    /// Upper-bound crow-flies km that could still finish within <paramref name="maxMinutes"/>
    /// (includes a small buffer for non-straight routes).
    /// </summary>
    public static double MaxCrowFliesKm(TransportMode mode, int maxMinutes, double? radiusKm = null)
    {
        maxMinutes = Math.Clamp(maxMinutes, 1, 180);
        var fromTime = SpeedKmPerHour(mode) * (maxMinutes / 60.0) * 1.25;
        if (radiusKm is > 0)
        {
            return Math.Min(fromTime, radiusKm.Value);
        }

        return fromTime;
    }
}
