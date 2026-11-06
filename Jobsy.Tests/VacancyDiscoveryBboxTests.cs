using Jobsy.Api.Vacancies;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class VacancyDiscoveryBboxTests
{
    [Fact]
    public void Bbox_filter_keeps_points_inside()
    {
        var box = new GeoBbox(52.0, 4.0, 52.1, 4.1);
        var inside = SampleRecord(52.05, 4.05);
        var outside = SampleRecord(52.2, 4.05);
        var filtered = VacancyDiscoveryBbox.Filter([inside, outside], box).ToList();
        Assert.Single(filtered);
        Assert.Equal(inside.Id, filtered[0].Id);
    }

    [Fact]
    public void CapPins_grid_reduces_large_sets()
    {
        var records = new List<VacancyDiscoveryRecord>(400);
        for (var row = 0; row < 20; row++)
        {
            for (var col = 0; col < 20; col++)
            {
                records.Add(SampleRecord(52.0 + row * 0.005, 4.0 + col * 0.005));
            }
        }

        var box = new GeoBbox(52.0, 4.0, 52.1, 4.1);
        var capped = VacancyDiscoveryBbox.CapPins(records, box, maxCount: 80, zoom: 9);
        Assert.True(capped.Count <= 80);
        Assert.True(capped.Count >= 40, $"Expected grid sample to cover viewport, got {capped.Count}");
    }

    private static VacancyDiscoveryRecord SampleRecord(double lat, double lng)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new VacancyDiscoveryRecord(
            Guid.NewGuid(),
            "Vacature",
            "",
            15m,
            today,
            today.AddMonths(1),
            VacancyStatus.Active,
            Guid.NewGuid(),
            "Co",
            "Stad",
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
            false,
            null,
            [],
            null,
            null,
            null,
            false,
            null,
            null,
            true,
            false);
    }
}
