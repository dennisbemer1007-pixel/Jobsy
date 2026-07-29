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
    public void MaxCrowFliesKm_Bike30Min_IsAtLeastNineKm()
    {
        // 18 km/h * 0.5h * 1.25 buffer = 11.25
        var km = TravelReach.MaxCrowFliesKm(TransportMode.Bike, 30);
        Assert.InRange(km, 10, 12);
    }

    [Fact]
    public void GeoDistance_Prefilter_ExcludesFarPoints()
    {
        var origin = new GeoPoint(52.0, 4.3);
        var near = new GeoPoint(52.05, 4.35);
        var far = new GeoPoint(52.5, 5.0);
        var reach = TravelReach.MaxCrowFliesKm(TransportMode.Bike, 30, radiusKm: 15);
        Assert.True(GeoDistance.IsWithinKm(origin, near, reach));
        Assert.False(GeoDistance.IsWithinKm(origin, far, reach));
    }
}
