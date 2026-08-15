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
}
