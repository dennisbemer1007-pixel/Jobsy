using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class SeedVacancyCategoryMixTests
{
    [Fact]
    public void Resolve_cycles_all_seven_built_in_categories()
    {
        var ids = Enumerable.Range(1, 7).Select(i => SeedVacancyCategoryMix.Resolve(i).CategoryId).ToHashSet();
        Assert.Equal(VacancyCategoryDefaults.All.Count, ids.Count);
        Assert.Contains(VacancyCategoryDefaults.RegulierId, ids);
        Assert.Contains(VacancyCategoryDefaults.UitzendbureauId, ids);
        Assert.Contains(VacancyCategoryDefaults.HighlightId, ids);
        Assert.Contains(VacancyCategoryDefaults.InclusiefId, ids);
        Assert.Contains(VacancyCategoryDefaults.VolunteerId, ids);
        Assert.Contains(VacancyCategoryDefaults.InternshipId, ids);
        Assert.Contains(VacancyCategoryDefaults.SeniorLightId, ids);

        Assert.Equal(VacancyKind.Volunteer, SeedVacancyCategoryMix.Resolve(5).Kind);
        Assert.Equal(VacancyKind.Internship, SeedVacancyCategoryMix.Resolve(6).Kind);
        Assert.False(SeedVacancyCategoryMix.Resolve(2).PreferHighlight);
        Assert.True(SeedVacancyCategoryMix.Resolve(3).PreferHighlight);
        Assert.True(SeedVacancyCategoryMix.Resolve(8).SuitableFor65Plus); // second Regulier cycle
    }

    [Fact]
    public async Task Westland_seed_covers_all_vacancy_categories()
    {
        await using var db = CreateDb();
        SeedMinimumWageRates(db);

        await WestlandVacanciesSeeder.SeedWestlandBanenkaartAsync(db, NullLogger.Instance);

        var categoryIds = await db.Vacancies
            .Where(v => v.Status == VacancyStatus.Active)
            .Select(v => v.CategoryId)
            .Distinct()
            .ToListAsync();

        foreach (var seed in VacancyCategoryDefaults.All)
        {
            Assert.Contains(seed.Id, categoryIds);
        }

        Assert.Contains(await db.Vacancies.ToListAsync(), v => v.Kind == VacancyKind.Internship);
        Assert.Contains(await db.Vacancies.ToListAsync(), v => v.Kind == VacancyKind.Volunteer);
        Assert.Contains(await db.Vacancies.ToListAsync(), v => v.SuitableFor65Plus);
        Assert.Contains(await db.Vacancies.ToListAsync(), v => v.IsHighlighted);
    }

    [Fact]
    public async Task Haaglanden_seed_covers_all_vacancy_categories()
    {
        await using var db = CreateDb();
        SeedMinimumWageRates(db);

        await HaaglandenVacanciesSeeder.SeedHaaglandenBanenkaartAsync(db, NullLogger.Instance);

        var categoryIds = await db.Vacancies
            .Select(v => v.CategoryId)
            .Distinct()
            .ToListAsync();

        foreach (var seed in VacancyCategoryDefaults.All)
        {
            Assert.Contains(seed.Id, categoryIds);
        }
    }

    [Fact]
    public async Task EnsureAsync_upgrades_existing_seed_vacancies_idempotently()
    {
        await using var db = CreateDb();
        SeedMinimumWageRates(db);
        await WestlandVacanciesSeeder.SeedWestlandBanenkaartAsync(db, NullLogger.Instance);

        // Simulate legacy backfill: everything Regulier.
        foreach (var v in db.Vacancies)
        {
            v.CategoryId = VacancyCategoryDefaults.RegulierId;
            v.Kind = VacancyKind.Regular;
            v.SuitableFor65Plus = false;
        }

        await db.SaveChangesAsync();

        await SeedVacancyCategoryMix.EnsureAsync(db, NullLogger.Instance);
        var after = await db.Vacancies.Select(v => v.CategoryId).Distinct().ToListAsync();
        Assert.Equal(VacancyCategoryDefaults.All.Count, after.Count);

        var logs = await db.PlatformLogs.CountAsync(l =>
            l.Category == "Seed" && l.Message == SeedVacancyCategoryMix.Marker);
        Assert.Equal(1, logs);

        await SeedVacancyCategoryMix.EnsureAsync(db, NullLogger.Instance);
        Assert.Equal(1, await db.PlatformLogs.CountAsync(l =>
            l.Category == "Seed" && l.Message == SeedVacancyCategoryMix.Marker));
    }

    private static void SeedMinimumWageRates(JobsyDbContext db)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.MinimumWageRates.AddRange(
            new Core.Entities.MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 15, HourlyRate = 4.22m, Label = "15", EffectiveFrom = today },
            new Core.Entities.MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 16, HourlyRate = 4.85m, Label = "16", EffectiveFrom = today },
            new Core.Entities.MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 17, HourlyRate = 5.55m, Label = "17", EffectiveFrom = today },
            new Core.Entities.MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 18, HourlyRate = 7.03m, Label = "18", EffectiveFrom = today },
            new Core.Entities.MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 19, HourlyRate = 8.44m, Label = "19", EffectiveFrom = today },
            new Core.Entities.MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 20, HourlyRate = 11.25m, Label = "20", EffectiveFrom = today },
            new Core.Entities.MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 21, HourlyRate = 14.06m, Label = "21+", EffectiveFrom = today });
        db.SaveChanges();
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
