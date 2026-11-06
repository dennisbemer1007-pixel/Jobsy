using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

/// <summary>
/// Acceptatie demo vacancies are Den Haag–centric; fiets 30 min from Amsterdam should not match them.
/// </summary>
public sealed class TravelDiscoverAmsterdamSeedTests
{
    [Fact]
    public void Fiets_30_min_from_Amsterdam_does_not_reach_Den_Haag_demo_coords()
    {
        const double amsterdamLat = 52.3676;
        const double amsterdamLng = 4.9041;
        const double denHaagLat = 52.0910;
        const double denHaagLng = 4.2815;

        var (minutes, km) = TravelReach.Estimate(
            amsterdamLat, amsterdamLng, denHaagLat, denHaagLng, TransportMode.Bike);

        Assert.True(minutes > 30, $"Expected >30 min bike, got {minutes} min ({km} km crow-flies).");
        Assert.True(km > TravelReach.MaxCrowFliesKm(TransportMode.Bike, 30),
            "Den Haag demo pins sit outside the fiets 30 min prefilter ring from Amsterdam.");
    }
}
