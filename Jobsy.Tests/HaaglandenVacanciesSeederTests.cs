using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class HaaglandenVacanciesSeederTests
{
    [Fact]
    public async Task Seeds_225_distinct_active_vacancies_across_three_cities()
    {
        await using var db = CreateDb();
        var logger = NullLogger.Instance;
        SeedMinimumWageRates(db);

        await HaaglandenVacanciesSeeder.SeedHaaglandenBanenkaartAsync(db, logger);

        var vacancies = await db.Vacancies
            .Where(v => v.Status == VacancyStatus.Active)
            .ToListAsync();

        Assert.Equal(225, vacancies.Count);
        Assert.Equal(100, vacancies.Count(v => v.Id.ToString().StartsWith("a2000000", StringComparison.Ordinal)));
        Assert.Equal(75, vacancies.Count(v => v.Id.ToString().StartsWith("a3000000", StringComparison.Ordinal)));
        Assert.Equal(50, vacancies.Count(v => v.Id.ToString().StartsWith("a4000000", StringComparison.Ordinal)));

        Assert.Equal(vacancies.Count, vacancies.Select(v => v.Id).Distinct().Count());
        Assert.Equal(vacancies.Count, vacancies.Select(v => v.ImageUrl).Distinct().Count());
        Assert.Equal(vacancies.Count, vacancies.Select(v => v.Title).Distinct().Count());
        Assert.Equal(vacancies.Count, vacancies.Select(v => v.Description).Distinct().Count());

        Assert.All(vacancies, v =>
        {
            Assert.False(string.IsNullOrWhiteSpace(v.ImageUrl));
            Assert.Contains("picsum.photos", v.ImageUrl, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(v.VideoUrl));
            Assert.True(v.Description.Length >= MockVacancyMedia.MinRichDescriptionLength);
            Assert.NotNull(v.SalaryTableId);
        });

        Assert.True(vacancies.Count(v => !string.IsNullOrWhiteSpace(v.RequiredDrivingLicense)) >= 50);
        Assert.Contains(vacancies, v => v.Description.Length > 280);
        Assert.True(await db.PlatformLogs.AnyAsync(l =>
            l.Category == "Seed" &&
            l.Message == "Haaglanden banenkaart seed DH100-Delft75-Zoetermeer50"));

        // Idempotent: second run adds nothing.
        await HaaglandenVacanciesSeeder.SeedHaaglandenBanenkaartAsync(db, logger);
        Assert.Equal(225, await db.Vacancies.CountAsync());
    }

    [Fact]
    public async Task AssignMissing_backfills_seeded_vacancies_without_salary_table()
    {
        await using var db = CreateDb();
        SeedMinimumWageRates(db);
        var companyId = Guid.Parse("c2000000-0000-4000-8000-000000000001");
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test",
            KvkNumber = "22000001",
            Address = "Test",
            Type = CompanyType.Employer,
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.07, 4.3)
        });
        var vacancyId = Guid.Parse("a2000000-0000-4000-8000-000000000099");
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "Zonder tabel",
            Description = new string('x', MockVacancyMedia.MinRichDescriptionLength),
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1),
            Status = VacancyStatus.Active,
            CompanyId = companyId,
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.07, 4.3),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            SalaryTableId = null
        });
        await db.SaveChangesAsync();

        await WmlSalaryTableService.EnsureForAllCompaniesAsync(db);

        var vacancy = await db.Vacancies.SingleAsync(v => v.Id == vacancyId);
        Assert.NotNull(vacancy.SalaryTableId);
        Assert.True(await db.CompanySalaryRates.AnyAsync(r => r.SalaryTableId == vacancy.SalaryTableId));
    }

    private static void SeedMinimumWageRates(JobsyDbContext db)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.MinimumWageRates.AddRange(
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 15, HourlyRate = 4.22m, Label = "15", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 16, HourlyRate = 4.85m, Label = "16", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 17, HourlyRate = 5.55m, Label = "17", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 18, HourlyRate = 7.03m, Label = "18", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 19, HourlyRate = 8.44m, Label = "19", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 20, HourlyRate = 11.25m, Label = "20", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 21, HourlyRate = 14.06m, Label = "21+", EffectiveFrom = today });
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

public class MediaBackfillSeederTests
{
    [Fact]
    public async Task Backfills_missing_image_video_and_short_description()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Co",
            KvkNumber = "12345678",
            Address = "Teststraat 1",
            Type = CompanyType.Employer,
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.0, 4.3)
        });
        var vacancyId = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "Test vacature",
            Description = "Korte tekst.",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1),
            Status = VacancyStatus.Active,
            CompanyId = companyId,
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.01, 4.31),
            WorkTypes = WorkType.Winkel,
            ImageUrl = "https://images.unsplash.com/photo-1500000000001?auto=format&fit=crop&w=600&q=80",
            VideoUrl = null
        });
        await db.SaveChangesAsync();

        await MediaBackfillSeeder.BackfillMediaAsync(db, NullLogger.Instance);

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        Assert.Contains("picsum.photos", vacancy.ImageUrl, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(vacancy.VideoUrl));
        Assert.True(vacancy.Description.Length >= MockVacancyMedia.MinRichDescriptionLength);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
