using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class OnboardingEarlyNearbySignalTests
{
    [Fact]
    public void Compute_counts_vacancy_within_travel_and_hours()
    {
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 30,
            PreferredTransport: TransportLabels.Bike,
            MinHoursPerWeek: 16,
            MaxHoursPerWeek: 32,
            Availability: new Dictionary<string, string[]>
            {
                ["Ma"] = ["Ochtend", "Middag"]
            },
            Educations: ["VMBO"]);

        var vacancy = DiscoveryRecord(WorkType.Logistiek) with
        {
            Latitude = 52.09,
            Longitude = 5.12,
            MinHoursPerWeek = 20,
            MaxHoursPerWeek = 32,
            ScheduleJson = """{"flexibleTimes":false,"slots":{"Ma":["Ochtend"]}}"""
        };

        var result = OnboardingEarlyNearbySignal.Compute(
            52.07,
            5.10,
            prefs,
            [vacancy],
            candidateAgeYears: 20);

        Assert.False(result.MissingHomeLocation);
        Assert.Equal(1, result.Count);
        Assert.Single(result.Examples);
    }

    [Fact]
    public void Compute_excludes_vacancy_beyond_max_travel()
    {
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 10,
            PreferredTransport: TransportLabels.Bike,
            MinHoursPerWeek: 8,
            MaxHoursPerWeek: 40);

        var far = DiscoveryRecord(WorkType.Logistiek) with
        {
            Latitude = 52.5,
            Longitude = 5.5
        };
        var result = OnboardingEarlyNearbySignal.Compute(52.07, 5.10, prefs, [far], null);
        Assert.Equal(0, result.Count);
    }

    private static VacancyDiscoveryRecord DiscoveryRecord(WorkType workType)
        => new(
            Guid.NewGuid(),
            "Magazijnmedewerker",
            "beschrijving",
            14.50m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            VacancyStatus.Active,
            Guid.NewGuid(),
            "Test BV",
            "Industrieweg 1, Utrecht",
            null,
            null,
            null,
            52.07,
            4.30,
            TransportMode.Bike,
            [TransportLabels.Bike],
            workType,
            WorkTypeLabels.Expand(workType).FirstOrDefault(),
            WorkTypeLabels.Expand(workType),
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
            24,
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
            true);
}
