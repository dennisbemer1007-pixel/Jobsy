using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class EnterpriseWestlandVacanciesSeederTests
{
    [Fact]
    public async Task Seeds_50_active_westland_vacancies_for_enterprise_company()
    {
        await using var db = CreateDb();
        SeedMinimumWageRates(db);

        await EnterpriseWestlandVacanciesSeeder.SeedAsync(db, NullLogger.Instance);

        var vacancies = await db.Vacancies.ToListAsync();
        Assert.Equal(50, vacancies.Count);
        Assert.All(vacancies, v =>
        {
            Assert.Equal(VacancyStatus.Active, v.Status);
            Assert.Equal(EnterpriseWestlandVacanciesSeeder.WestlandFreshId, v.CompanyId);
            Assert.StartsWith("a5000000", v.Id.ToString(), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(v.ImageUrl));
            Assert.False(string.IsNullOrWhiteSpace(v.VideoUrl));
            Assert.True(v.Description.Length >= MockVacancyMedia.MinRichDescriptionLength);
            Assert.InRange(v.Location.Latitude, 51.94, 52.04);
            Assert.InRange(v.Location.Longitude, 4.14, 4.29);
        });

        Assert.Equal(50, vacancies.Select(v => v.Title).Distinct().Count());
        Assert.Equal(50, vacancies.Select(v => v.Description).Distinct().Count());
        Assert.Contains(vacancies, v => v.Title.Contains("Naaldwijk", StringComparison.Ordinal));
        Assert.Contains(vacancies, v => v.Title.Contains("Heenweg", StringComparison.Ordinal));
        Assert.Contains(vacancies, v => v.RequiredDrivingLicense == "B");
        Assert.Contains(vacancies, v => v.IsHighlighted);

        var company = await db.Companies.SingleAsync(c => c.Id == EnterpriseWestlandVacanciesSeeder.WestlandFreshId);
        Assert.Equal("Westland Fresh Logistics", company.Name);

        Assert.True(await db.PlatformLogs.AnyAsync(l =>
            l.Category == "Seed" && l.Message == EnterpriseWestlandVacanciesSeeder.SeedMarker));

        await EnterpriseWestlandVacanciesSeeder.SeedAsync(db, NullLogger.Instance);
        Assert.Equal(50, await db.Vacancies.CountAsync());
        Assert.Equal(1, await db.PlatformLogs.CountAsync(l =>
            l.Category == "Seed" && l.Message == EnterpriseWestlandVacanciesSeeder.SeedMarker));
    }

    [Fact]
    public async Task Links_existing_enterprise_user_to_westland_fresh()
    {
        await using var db = CreateDb();
        SeedMinimumWageRates(db);
        db.Users.Add(new User
        {
            Id = Guid.Parse("dddddddd-1111-1111-1111-111111111111"),
            Email = "enterprise@jobsy.local",
            FullName = "Bedrijfsmanager Jobsy Retail",
            Role = UserRole.EnterpriseManager,
            CompanyId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            IsActive = true
        });
        await db.SaveChangesAsync();

        await EnterpriseWestlandVacanciesSeeder.SeedAsync(db, NullLogger.Instance);

        Assert.True(await db.UserCompanies.AnyAsync(m =>
            m.UserId == Guid.Parse("dddddddd-1111-1111-1111-111111111111")
            && m.CompanyId == EnterpriseWestlandVacanciesSeeder.WestlandFreshId));
        Assert.Equal(50, await db.Vacancies.CountAsync(v =>
            v.CompanyId == EnterpriseWestlandVacanciesSeeder.WestlandFreshId));
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
