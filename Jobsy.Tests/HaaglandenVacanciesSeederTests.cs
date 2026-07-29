using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
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

        Assert.True(vacancies.Count(v => !string.IsNullOrWhiteSpace(v.RequiredDrivingLicense)) >= 50);
        Assert.Contains(vacancies, v => v.Description.Length > 280);
        Assert.True(await db.PlatformLogs.AnyAsync(l =>
            l.Category == "Seed" &&
            l.Message == "Haaglanden banenkaart seed DH100-Delft75-Zoetermeer50"));

        // Idempotent: second run adds nothing.
        await HaaglandenVacanciesSeeder.SeedHaaglandenBanenkaartAsync(db, logger);
        Assert.Equal(225, await db.Vacancies.CountAsync());
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
