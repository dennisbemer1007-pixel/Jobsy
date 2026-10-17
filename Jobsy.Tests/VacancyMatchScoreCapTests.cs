using Jobsy.Api.Vacancies;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class VacancyMatchScoreCapTests
{
    [Fact]
    public void LimitCandidates_without_bbox_caps_at_MaxPinsWithoutBbox()
    {
        var records = Enumerable.Range(0, 400)
            .Select(i => Sample(i % 10, 52.0 + i * 0.001, 4.3))
            .ToList();
        var wrapped = records.Select(r => (Record: r, Travel: (int?)10)).ToList();

        var capped = VacancyMatchScoreCap.LimitCandidates(wrapped, bbox: null, zoom: null, c => c.Record);

        Assert.True(capped.Count <= 250);
        Assert.True(capped.Count >= 100);
    }

    private static VacancyDiscoveryRecord Sample(int i, double lat, double lng)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new VacancyDiscoveryRecord(
            Guid.NewGuid(),
            $"Vacature {i}",
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
