using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class BanenkaartTravelDiscoverTests
{
    private const double AmsLat = 52.3725;
    private const double AmsLng = 4.8921;
    private const double DenHaagLat = 52.0705;
    private const double DenHaagLng = 4.3007;

    [Fact]
    public void Auto_60_min_from_Amsterdam_includes_Den_Haag_vacancy()
    {
        var record = SampleVacancy(DenHaagLat, DenHaagLng);
        var within = TravelDiscoverCandidates.WithinTravelMinutes(
            [record],
            AmsLat,
            AmsLng,
            TransportMode.Car,
            TransportLabels.Car,
            maxMinutes: 60,
            radiusKm: null);

        var match = Assert.Single(within);
        Assert.Equal(record.Id, match.Record.Id);
        Assert.True(match.TravelMinutes <= 60);
    }

    [Fact]
    public void NearestReachable_orders_by_distance_when_outside_travel_window()
    {
        var near = SampleVacancy(52.35, 4.85);
        var far = SampleVacancy(DenHaagLat, DenHaagLng);
        var nearest = TravelDiscoverCandidates.NearestReachable(
            [far, near],
            AmsLat,
            AmsLng,
            TransportMode.Car,
            TransportLabels.Car);

        Assert.Equal(near.Id, nearest[0].Record.Id);
        Assert.True(nearest[0].DistanceKm < nearest[1].DistanceKm);
    }

    private static VacancyDiscoveryRecord SampleVacancy(double lat, double lng)
        => new(
            Guid.NewGuid(),
            "Vacature Den Haag",
            "beschrijving",
            14.50m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            VacancyStatus.Active,
            Guid.NewGuid(),
            "Bedrijf",
            "Den Haag",
            null,
            null,
            null,
            lat,
            lng,
            TransportMode.Bike,
            [TransportLabels.Bike],
            WorkType.Horeca,
            WorkTypeLabels.Horeca,
            [WorkTypeLabels.Horeca],
            false,
            null,
            0,
            null,
            [],
            null,
            null,
            null,
            null,
            VacancySource.Manual,
            8,
            40,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            VacancyKind.Regular,
            null,
            null,
            true,
            null,
            [],
            VacancyCategoryDefaults.RegulierId,
            "Regulier",
            "#64748b",
            false,
            null,
            null,
            true,
            false);
}
