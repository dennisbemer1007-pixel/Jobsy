using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

public static class JobsyDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await MigrateAsync(services);
        await SeedDataAsync(services);
    }

    public static async Task MigrateAsync(IServiceProvider services)
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

        // Always ensure vacancy categories + backfill CategoryId after migrate (also when Seed:Enabled=false).
        try
        {
            await VacancyCategorySeeder.SeedAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Vacancy category ensure/backfill after migrate failed; continuing.");
        }
    }

    public static async Task PurgeDemoDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("JobsyDbSeeder");
        await DemoDataPurge.PurgeAsync(db, logger);
    }

    public static async Task SeedDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("JobsyDbSeeder");

        if (await db.Companies.AnyAsync())
        {
            // Users first so salesmanager demo account exists even if later seeders fail.
            await DemoUsersSeeder.SeedUsersAsync(db, logger);

            // Hierarchy / salary-table demo fixes before wage sync.
            try
            {
                await Sprint0DemoSeeder.SeedSprint0DemoAsync(db, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Sprint0 demo seed failed; continuing.");
            }

            try
            {
                await MediaBackfillSeeder.BackfillMediaAsync(db, logger);
                await PlatformSettingsSeeder.SeedPlatformSettingsAsync(db, logger);
                await MasterdataSeeder.SeedAsync(db, logger);
                await VacancyCategorySeeder.SeedAsync(db, logger);
                await Sprint8MetricsSeeder.SeedRichMetricsAsync(db, logger);
                await SalesManagerDemoSeeder.SeedAsync(db, logger);
                await WestlandVacanciesSeeder.SeedWestlandBanenkaartAsync(db, logger);
                await EnterpriseWestlandVacanciesSeeder.SeedAsync(db, logger);
                await HaaglandenVacanciesSeeder.SeedHaaglandenBanenkaartAsync(db, logger);
                await SeedVacancyCategoryMix.EnsureAsync(db, logger);
                // Re-run media backfill after banenkaart seeds so every vacancy has image/video/copy.
                await MediaBackfillSeeder.BackfillMediaAsync(db, logger);
                // EnsureForAll also fills empty tables and assigns missing vacancy salary tables.
                await WmlSalaryTableService.EnsureForAllCompaniesAsync(db);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Partial seed failure after demo users; continuing.");
            }

            return;
        }

        await DemoCompaniesSeeder.SeedCompaniesAsync(db, logger);
        await DemoUsersSeeder.SeedUsersAsync(db, logger);
        try
        {
            await ApplicationsAndWagesSeeder.SeedApplicationsAndWagesAsync(db, logger);
            await PlatformSettingsSeeder.SeedPlatformSettingsAsync(db, logger);
            await MasterdataSeeder.SeedAsync(db, logger);
            await VacancyCategorySeeder.SeedAsync(db, logger);
            await Sprint0DemoSeeder.SeedSprint0DemoAsync(db, logger);
            await Sprint8MetricsSeeder.SeedRichMetricsAsync(db, logger);
            await SalesManagerDemoSeeder.SeedAsync(db, logger);
            await WestlandVacanciesSeeder.SeedWestlandBanenkaartAsync(db, logger);
            await EnterpriseWestlandVacanciesSeeder.SeedAsync(db, logger);
            await HaaglandenVacanciesSeeder.SeedHaaglandenBanenkaartAsync(db, logger);
            await SeedVacancyCategoryMix.EnsureAsync(db, logger);
            await MediaBackfillSeeder.BackfillMediaAsync(db, logger);
            // EnsureForAll also fills empty tables and assigns missing vacancy salary tables.
            await WmlSalaryTableService.EnsureForAllCompaniesAsync(db);
            logger.LogInformation("Seed completed: employers + intermediary, vacancies, tokens, role users, sprint-0/8 demo.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Partial seed failure after companies/users; continuing.");
        }
    }
}
