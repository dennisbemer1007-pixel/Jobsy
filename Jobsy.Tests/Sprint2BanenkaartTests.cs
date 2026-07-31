using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class WageVisibilityRulesTests
{
    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void CanShowWage_matches_sprint2_rules(
        bool authenticated, bool candidate, bool hasDob, bool expected)
        => Assert.Equal(expected, WageVisibilityRules.CanShowWage(authenticated, candidate, hasDob));
}

public class VacancyWageResolverTests
{
    [Fact]
    public void ResolveHourlyWage_uses_highest_band_at_or_below_age()
    {
        var rates = new[]
        {
            new Jobsy.Core.Entities.CompanySalaryRate { AgeYears = 18, HourlyRate = 8.00m },
            new Jobsy.Core.Entities.CompanySalaryRate { AgeYears = 21, HourlyRate = 14.50m },
            new Jobsy.Core.Entities.CompanySalaryRate { AgeYears = 16, HourlyRate = 5.20m }
        };

        Assert.Equal(5.20m, VacancyWageResolver.ResolveHourlyWage(13.20m, rates, 16));
        Assert.Equal(8.00m, VacancyWageResolver.ResolveHourlyWage(13.20m, rates, 19));
        Assert.Equal(14.50m, VacancyWageResolver.ResolveHourlyWage(13.20m, rates, 25));
        Assert.Equal(5.20m, VacancyWageResolver.ResolveHourlyWage(13.20m, rates, 15));
    }

    [Fact]
    public void ResolveHourlyWage_without_table_scales_flat_adult_wage_by_age()
    {
        // Flat HourlyWage is the 21+ rate; youth ages use default WML-style fractions.
        Assert.Equal(6.60m, VacancyWageResolver.ResolveHourlyWage(13.20m, null, 18)); // 50%
        Assert.Equal(4.55m, VacancyWageResolver.ResolveHourlyWage(13.20m, null, 16)); // 34.5%
        Assert.Equal(13.20m, VacancyWageResolver.ResolveHourlyWage(13.20m, null, 21));
        Assert.Equal(13.20m, VacancyWageResolver.ResolveHourlyWage(13.20m, null, 25));
    }

    [Fact]
    public void ResolveHourlyWage_kokshulp_style_flat_wage_uses_youth_rate_for_filter_age()
    {
        // Seed vacancy …027 "Kokshulp" uses flat 14.60 without a salary table.
        Assert.Equal(5.04m, VacancyWageResolver.ResolveHourlyWage(14.60m, null, 16)); // 34.5%
        Assert.Equal(14.60m, VacancyWageResolver.ResolveHourlyWage(14.60m, null, 21));
    }

    [Fact]
    public void GetWageBands_returns_ordered_labels()
    {
        var rates = new[]
        {
            new Jobsy.Core.Entities.CompanySalaryRate { AgeYears = 21, HourlyRate = 14.50m, Label = "21+" },
            new Jobsy.Core.Entities.CompanySalaryRate { AgeYears = 18, HourlyRate = 8.00m, Label = "18" }
        };

        var bands = VacancyWageResolver.GetWageBands(13.20m, rates);
        Assert.Equal(2, bands.Count);
        Assert.Equal(18, bands[0].AgeYears);
        Assert.Equal(21, bands[1].AgeYears);
    }

    [Fact]
    public void GetWageBands_without_table_returns_scaled_youth_bands()
    {
        var bands = VacancyWageResolver.GetWageBands(14.50m, null);
        Assert.Equal(7, bands.Count);
        Assert.Equal("15", bands[0].Label);
        Assert.Equal("21+", bands[^1].Label);
        Assert.Equal(14.50m, bands[^1].HourlyRate);
        Assert.True(bands[0].HourlyRate < bands[^1].HourlyRate);
    }
}


public class WorkTypeLabelsTests
{
    [Fact]
    public void Expand_and_parse_roundtrip()
    {
        var types = WorkType.Horeca | WorkType.Logistiek;
        var labels = WorkTypeLabels.Expand(types);
        Assert.Contains(WorkTypeLabels.Horeca, labels);
        Assert.Contains(WorkTypeLabels.Logistiek, labels);
        Assert.Equal(types, WorkTypeLabels.Combine(labels));
    }

    [Theory]
    [InlineData(WorkType.Horeca, true)]
    [InlineData(WorkType.Horeca | WorkType.Winkel, true)]
    [InlineData(WorkType.None, false)]
    [InlineData(WorkType.Horeca | WorkType.Winkel | WorkType.Zorg, false)]
    public void IsValidSelection_enforces_one_or_two(WorkType types, bool expected)
        => Assert.Equal(expected, WorkTypeLabels.IsValidSelection(types));

    [Fact]
    public void MatchesFilter_checks_flag()
    {
        var types = WorkType.Winkel | WorkType.Horeca;
        Assert.True(WorkTypeLabels.MatchesFilter(types, "Winkel"));
        Assert.True(WorkTypeLabels.MatchesFilter(types, "retail"));
        Assert.False(WorkTypeLabels.MatchesFilter(types, "Logistiek"));
        Assert.True(WorkTypeLabels.MatchesFilter(types, null));
    }

    [Fact]
    public void MatchesFilter_accepts_any_of_multiple_labels()
    {
        var types = WorkType.Winkel | WorkType.Horeca;
        Assert.True(WorkTypeLabels.MatchesFilter(types, null, ["Logistiek", "Winkel"]));
        Assert.True(WorkTypeLabels.MatchesFilter(types, null, ["Horeca,Zorg"]));
        Assert.False(WorkTypeLabels.MatchesFilter(types, null, ["Logistiek", "Zorg"]));
        Assert.True(WorkTypeLabels.MatchesFilter(types, null, Array.Empty<string>()));
    }
}

public class TransportLabelsTests
{
    [Theory]
    [InlineData("Fiets", TransportMode.Bike)]
    [InlineData("Auto", TransportMode.Car)]
    [InlineData("OV", TransportMode.PublicTransport)]
    [InlineData("Lopend", TransportMode.Walking)]
    public void Parse_maps_ui_labels(string label, TransportMode expected)
        => Assert.Equal(expected, TransportLabels.Parse(label));

    [Fact]
    public void Expand_includes_flagged_modes()
    {
        var labels = TransportLabels.Expand(TransportMode.Bike | TransportMode.Car);
        Assert.Contains(TransportLabels.Bike, labels);
        Assert.Contains(TransportLabels.Car, labels);
        Assert.DoesNotContain(TransportLabels.Walking, labels);
    }
}

public class MockRoutingServiceTests
{
    [Fact]
    public async Task GetRouteAsync_returns_positive_duration()
    {
        IRoutingService sut = new MockRoutingService();
        // ~Den Haag city centre to Scheveningen-ish
        var result = await sut.GetRouteAsync(52.0705, 4.3007, 52.1133, 4.2812, TransportMode.Bike);
        Assert.True(result.DistanceMeters > 0);
        Assert.True(result.DurationSeconds > 0);
        Assert.Equal(TransportMode.Bike, result.TransportMode);
    }
}
