using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class TravelReachEstimateTests
{
    [Fact]
    public async Task Estimate_matches_mock_routing_service()
    {
        IRoutingService routing = new MockRoutingService();
        var route = await routing.GetRouteAsync(52.0705, 4.3007, 52.1133, 4.2812, TransportMode.Bike);
        var estimate = TravelReach.Estimate(52.0705, 4.3007, 52.1133, 4.2812, TransportMode.Bike);

        Assert.Equal((int)Math.Ceiling(route.DurationSeconds / 60.0), estimate.TravelMinutes);
        Assert.Equal(Math.Round(route.DistanceMeters / 1000.0, 2), estimate.DistanceKm);
    }
}

public class VacancyDiscoveryQueryTests
{
    [Fact]
    public void Filter_keeps_matching_work_type_and_hours()
    {
        var horeca = Record(WorkType.Horeca, minHours: 8, maxHours: 16);
        var zorg = Record(WorkType.Zorg, minHours: 32, maxHours: 40);

        var filtered = VacancyDiscoveryQuery.Filter(
            [horeca, zorg],
            workTypes: [WorkTypeLabels.Horeca],
            minHoursPerWeek: 0,
            maxHoursPerWeek: 20)
            .ToList();

        Assert.Single(filtered);
        Assert.Equal(horeca.Id, filtered[0].Id);
    }

    [Fact]
    public void Filter_matches_title_search()
    {
        var kok = Record(WorkType.Horeca, title: "Kokshulp");
        var picker = Record(WorkType.Logistiek, title: "Orderpicker");

        var filtered = VacancyDiscoveryQuery.Filter([kok, picker], searchQuery: "kok").ToList();
        Assert.Equal(kok.Id, Assert.Single(filtered).Id);
    }

    [Fact]
    public void Visibility_hides_expired_indexed_rows_on_read()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expired = Record(WorkType.Horeca, title: "Was live") with
        {
            StartDate = today.AddDays(-10),
            EndDate = today.AddDays(-1)
        };
        var live = Record(WorkType.Horeca, title: "Nog live") with
        {
            StartDate = today.AddDays(-1),
            EndDate = today.AddDays(5)
        };

        Assert.False(VacancyVisibilityRules.IsPubliclyVisible(expired, today));
        Assert.True(VacancyVisibilityRules.IsPubliclyVisible(live, today));
    }

    private static VacancyDiscoveryRecord Record(
        WorkType workType,
        string title = "Vacature",
        decimal? minHours = 8,
        decimal? maxHours = 24)
        => new(
            Guid.NewGuid(),
            title,
            "beschrijving",
            14.50m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            VacancyStatus.Active,
            Guid.NewGuid(),
            "Bedrijf",
            "Straat 1",
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
            minHours,
            maxHours,
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

public class VacancyDiscoveryIndexTests
{
    [Fact]
    public async Task GetActiveAsync_indexes_only_public_vacancies_and_rebuilds_after_invalidate()
    {
        var dbName = "DiscoveryIndex-" + Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var companyId = Guid.NewGuid();
        var liveId = Guid.NewGuid();
        var draftId = Guid.NewGuid();

        using (var seed = provider.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.Companies.Add(new Company
            {
                Id = companyId,
                Name = "Index Café",
                KvkNumber = "12345678",
                Address = "Plein 1",
                Location = new GeoPoint(52.07, 4.30)
            });
            db.Vacancies.AddRange(
                new Vacancy
                {
                    Id = liveId,
                    Title = "Barista",
                    Description = "Koffie",
                    CompanyId = companyId,
                    Status = VacancyStatus.Active,
                    HourlyWage = 14,
                    Location = new GeoPoint(52.07, 4.30),
                    StartDate = today,
                    EndDate = today.AddDays(20),
                    WorkTypes = WorkType.Horeca
                },
                new Vacancy
                {
                    Id = draftId,
                    Title = "Nog niet live",
                    Description = "concept",
                    CompanyId = companyId,
                    Status = VacancyStatus.Draft,
                    HourlyWage = 14,
                    Location = new GeoPoint(52.08, 4.31),
                    StartDate = today,
                    EndDate = today.AddDays(20)
                });
            await db.SaveChangesAsync();
        }

        var index = new VacancyDiscoveryIndex(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<VacancyDiscoveryIndex>.Instance);

        var first = await index.GetActiveAsync();
        Assert.Equal(liveId, Assert.Single(first).Id);
        Assert.Equal("Index Café", first[0].CompanyName);

        using (var mutate = provider.CreateScope())
        {
            var db = mutate.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var draft = await db.Vacancies.SingleAsync(v => v.Id == draftId);
            draft.Status = VacancyStatus.Active;
            await db.SaveChangesAsync();
        }

        // Warm snapshot stays until invalidate — then the new vacancy is indexed immediately.
        var stale = await index.GetActiveAsync();
        Assert.Single(stale);

        index.Invalidate();
        var fresh = await index.GetActiveAsync();
        Assert.Equal(2, fresh.Count);
        Assert.Contains(fresh, v => v.Id == draftId);
    }

    [Fact]
    public async Task Scheduled_refresh_rebuilds_without_invalidate()
    {
        var dbName = "DiscoveryIndexForce-" + Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var companyId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        using (var seed = provider.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.Companies.Add(new Company
            {
                Id = companyId,
                Name = "Refresh Café",
                KvkNumber = "87654321",
                Address = "Plein 2",
                Location = new GeoPoint(52.07, 4.30)
            });
            db.Vacancies.Add(new Vacancy
            {
                Id = firstId,
                Title = "Eerste",
                Description = "d",
                CompanyId = companyId,
                Status = VacancyStatus.Active,
                HourlyWage = 14,
                Location = new GeoPoint(52.07, 4.30),
                StartDate = today,
                EndDate = today.AddDays(10)
            });
            await db.SaveChangesAsync();
        }

        var index = new VacancyDiscoveryIndex(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<VacancyDiscoveryIndex>.Instance);
        Assert.Equal(firstId, Assert.Single(await index.GetActiveAsync()).Id);

        using (var mutate = provider.CreateScope())
        {
            var db = mutate.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.Vacancies.Add(new Vacancy
            {
                Id = secondId,
                Title = "Tweede",
                Description = "d",
                CompanyId = companyId,
                Status = VacancyStatus.Active,
                HourlyWage = 14,
                Location = new GeoPoint(52.08, 4.31),
                StartDate = today,
                EndDate = today.AddDays(10)
            });
            await db.SaveChangesAsync();
        }

        await index.RefreshAsync();
        var afterJob = await index.GetActiveAsync();
        Assert.Equal(2, afterJob.Count);
        Assert.Contains(afterJob, v => v.Id == secondId);
    }

    [Fact]
    public async Task GetMapViewAsync_uses_centroid_of_indexed_pins()
    {
        var dbName = "DiscoveryIndexMapView-" + Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var companyId = Guid.NewGuid();
        using (var seed = provider.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.Companies.Add(new Company
            {
                Id = companyId,
                Name = "Kaart Café",
                KvkNumber = "11223344",
                Address = "Plein 3",
                Location = new GeoPoint(52.00, 4.20)
            });
            db.Vacancies.AddRange(
                new Vacancy
                {
                    Id = Guid.NewGuid(),
                    Title = "West",
                    Description = "d",
                    CompanyId = companyId,
                    Status = VacancyStatus.Active,
                    HourlyWage = 14,
                    Location = new GeoPoint(52.00, 4.20),
                    StartDate = today,
                    EndDate = today.AddDays(10)
                },
                new Vacancy
                {
                    Id = Guid.NewGuid(),
                    Title = "East",
                    Description = "d",
                    CompanyId = companyId,
                    Status = VacancyStatus.Active,
                    HourlyWage = 14,
                    Location = new GeoPoint(52.02, 4.30),
                    StartDate = today,
                    EndDate = today.AddDays(10)
                });
            await db.SaveChangesAsync();
        }

        var index = new VacancyDiscoveryIndex(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<VacancyDiscoveryIndex>.Instance);
        var view = await index.GetMapViewAsync();
        Assert.Equal(2, view.PinCount);
        Assert.Equal(52.01, view.CenterLat, 2);
        Assert.Equal(4.25, view.CenterLng, 2);
        Assert.InRange(view.Zoom, 11, 13);
    }
}

public class VacancyMapViewCalculatorTests
{
    [Fact]
    public void Empty_points_use_netherlands_fallback()
    {
        var view = VacancyMapViewCalculator.FromPoints([]);
        Assert.Equal(VacancyMapViewCalculator.Fallback, view);
        Assert.False(view.HasPins);
        Assert.Equal(7, view.Zoom);
    }

    [Fact]
    public void Westland_cluster_is_centroid_not_country_zoom()
    {
        var view = VacancyMapViewCalculator.FromPoints(
        [
            (51.995, 4.167),
            (52.011, 4.221),
            (52.045, 4.330)
        ]);
        Assert.Equal(3, view.PinCount);
        Assert.InRange(view.CenterLat, 51.99, 52.03);
        Assert.InRange(view.CenterLng, 4.20, 4.26);
        Assert.True(view.Zoom >= 11, "Local clusters must not open at NL zoom 7–8.");
        Assert.Equal(VacancyMapViewCalculator.ZoomForSpan(0.163), view.Zoom);
    }

    [Theory]
    [InlineData(0.05, 13)]
    [InlineData(0.15, 12)]
    [InlineData(0.4, 11)]
    [InlineData(1.0, 10)]
    [InlineData(2.0, 9)]
    [InlineData(3.0, 8)]
    public void Zoom_matches_jobmap_span_heuristic(double span, int zoom)
        => Assert.Equal(zoom, VacancyMapViewCalculator.ZoomForSpan(span));

    [Fact]
    public void Filled_location_is_the_camera_center_at_local_zoom()
    {
        var view = VacancyMapViewCalculator.ForFilledLocation(52.01123, 4.22167, pinCount: 8);
        Assert.NotNull(view);
        Assert.Equal(52.01123, view.CenterLat);
        Assert.Equal(4.22167, view.CenterLng);
        Assert.Equal(VacancyMapViewCalculator.FilledLocationZoom, view.Zoom);
        Assert.Equal(11, view.Zoom);
        Assert.Equal(8, view.PinCount);
        Assert.True(view.HasPins);
    }

    [Fact]
    public void Opening_view_prefers_filled_origin_at_zoom_11_over_centroid()
    {
        var pins = VacancyMapViewCalculator.FromPoints([(52.0, 5.0), (53.0, 6.0)]);
        var view = VacancyMapViewCalculator.ResolveOpening(
            pins,
            originLat: 52.01123,
            originLng: 4.22167,
            regionLat: 51.92,
            regionLng: 4.48,
            companyFocus: false);
        Assert.Equal(52.01123, view.CenterLat);
        Assert.Equal(4.22167, view.CenterLng);
        Assert.Equal(11, view.Zoom);
    }

    [Fact]
    public void Opening_view_uses_region_focus_when_no_origin()
    {
        var pins = VacancyMapViewCalculator.FromPoints([(52.0, 5.0), (53.0, 6.0)]);
        var view = VacancyMapViewCalculator.ResolveOpening(
            pins,
            originLat: null,
            originLng: null,
            regionLat: 51.9225,
            regionLng: 4.47917,
            companyFocus: false);
        Assert.Equal(51.9225, view.CenterLat);
        Assert.Equal(4.47917, view.CenterLng);
        Assert.Equal(11, view.Zoom);
    }

    [Fact]
    public void Opening_view_keeps_pin_centroid_without_address_or_region()
    {
        var pins = VacancyMapViewCalculator.FromPoints([(52.0, 5.0), (53.0, 6.0)]);
        var view = VacancyMapViewCalculator.ResolveOpening(
            pins,
            originLat: null,
            originLng: null,
            regionLat: null,
            regionLng: null,
            companyFocus: false);
        Assert.Equal(pins, view);
    }

    [Fact]
    public void Opening_view_keeps_pin_centroid_for_company_focus()
    {
        var pins = VacancyMapViewCalculator.FromPoints([(52.0, 5.0), (53.0, 6.0)]);
        var view = VacancyMapViewCalculator.ResolveOpening(
            pins,
            originLat: 52.01123,
            originLng: 4.22167,
            regionLat: 51.92,
            regionLng: 4.48,
            companyFocus: true);
        Assert.Equal(pins, view);
    }

    [Theory]
    [InlineData(double.NaN, 4.2)]
    [InlineData(52.0, double.PositiveInfinity)]
    [InlineData(91, 4.2)]
    [InlineData(52.0, 181)]
    public void Invalid_filled_location_returns_null(double lat, double lng)
        => Assert.Null(VacancyMapViewCalculator.ForFilledLocation(lat, lng));
}
