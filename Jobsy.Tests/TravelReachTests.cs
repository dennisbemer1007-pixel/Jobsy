using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Tests;

public class TravelReachTests
{
    [Fact]
    public void MaxCrowFliesKm_RespectsRadiusCap()
    {
        var fromTime = TravelReach.MaxCrowFliesKm(TransportMode.Bike, maxMinutes: 30);
        var capped = TravelReach.MaxCrowFliesKm(TransportMode.Bike, maxMinutes: 30, radiusKm: 5);
        Assert.True(fromTime > 5);
        Assert.Equal(5, capped);
    }

    [Fact]
    public void MaxCrowFliesKm_Bike30Min_IsCircuityAdjusted()
    {
        // 18 km/h / 1.7 * 0.5h * 1.25 buffer ≈ 6.62
        var km = TravelReach.MaxCrowFliesKm(TransportMode.Bike, 30);
        Assert.InRange(km, 6.2, 7.0);
    }

    [Fact]
    public void GeoDistance_Prefilter_ExcludesFarPoints()
    {
        var origin = new GeoPoint(52.0, 4.3);
        var near = new GeoPoint(52.02, 4.32);
        var far = new GeoPoint(52.5, 5.0);
        var reach = TravelReach.MaxCrowFliesKm(TransportMode.Bike, 30, radiusKm: 15);
        Assert.True(GeoDistance.IsWithinKm(origin, near, reach));
        Assert.False(GeoDistance.IsWithinKm(origin, far, reach));
    }

    [Fact]
    public void Bike_10_min_ring_is_tighter_than_straight_cruise()
    {
        var ringKm = TravelReach.RingRadiusMeters(TransportMode.Bike, 10) / 1000.0;
        var straightKm = TravelReach.SpeedKmPerHour(TransportMode.Bike) * 10 / 60.0;
        Assert.InRange(ringKm, 1.6, 1.9);
        Assert.True(ringKm < straightKm * 0.7);
        Assert.Equal(3.0, straightKm, 3);
    }

    [Fact]
    public void Bike_3km_crow_flies_is_about_17_minutes()
    {
        var origin = new GeoPoint(52.0, 4.3);
        var north = new GeoPoint(52.0 + (3.0 / 111.32), 4.3);
        var km = GeoDistance.HaversineKm(origin, north);
        Assert.InRange(km, 2.85, 3.15);

        var estimate = TravelReach.Estimate(
            origin.Latitude, origin.Longitude, north.Latitude, north.Longitude, TransportMode.Bike);
        Assert.InRange(estimate.TravelMinutes, 16, 18);

        Assert.True(km > TravelReach.RingRadiusMeters(TransportMode.Bike, 10) / 1000.0);
        Assert.True(estimate.TravelMinutes > 10);
    }

    [Fact]
    public void Ring_edge_matches_estimated_minutes()
    {
        var originLat = 52.07;
        var originLng = 4.30;
        var ringKm = TravelReach.RingRadiusMeters(TransportMode.Bike, 10) / 1000.0;
        var destLat = originLat + (ringKm / 111.32);
        var estimate = TravelReach.Estimate(originLat, originLng, destLat, originLng, TransportMode.Bike);
        Assert.InRange(estimate.TravelMinutes, 10, 11);
    }
}
