using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

public static class JobsyDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("JobsyDbSeeder");

        var knownMigrations = db.Database.GetMigrations().ToList();
        if (knownMigrations.Count > 0)
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            logger.LogWarning(
                "No EF migrations found. Using EnsureCreated. Run: dotnet ef migrations add InitialCreate -p Jobsy.Infrastructure -s Jobsy.Api");
            await db.Database.EnsureCreatedAsync();
        }

        if (await db.Companies.AnyAsync())
        {
            await MediaBackfillSeeder.BackfillMediaAsync(db, logger);
            await DemoUsersSeeder.SeedUsersAsync(db, logger);
            await PlatformSettingsSeeder.SeedPlatformSettingsAsync(db, logger);
            await Sprint0DemoSeeder.SeedSprint0DemoAsync(db, logger);
            await Sprint8MetricsSeeder.SeedRichMetricsAsync(db, logger);
            await WestlandVacanciesSeeder.SeedWestlandBanenkaartAsync(db, logger);
            return;
        }

        await DemoCompaniesSeeder.SeedCompaniesAsync(db, logger);
        await DemoUsersSeeder.SeedUsersAsync(db, logger);
        await ApplicationsAndWagesSeeder.SeedApplicationsAndWagesAsync(db, logger);
        await PlatformSettingsSeeder.SeedPlatformSettingsAsync(db, logger);
        await Sprint0DemoSeeder.SeedSprint0DemoAsync(db, logger);
        await Sprint8MetricsSeeder.SeedRichMetricsAsync(db, logger);
        await WestlandVacanciesSeeder.SeedWestlandBanenkaartAsync(db, logger);
        logger.LogInformation("Seed completed: employers + intermediary, vacancies, tokens, role users, sprint-0/8 demo.");
    }
}
