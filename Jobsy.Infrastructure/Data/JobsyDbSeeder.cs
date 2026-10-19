using Jobsy.Infrastructure.Data.Migrations;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

public static class JobsyDbSeeder
{
    /// <summary>
    /// Ensures <see cref="PlatformFeatureSettings.PassportPdfDnaBlocksEnabled"/> exists before the API
    /// serves feature-gated public reads (banenkaart pins, partner catalog). Idempotent on Postgres.
    /// </summary>
    public static async Task EnsurePassportPdfDnaBlocksColumnAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("JobsyDbSeeder");
        try
        {
            await db.Database.ExecuteSqlRawAsync(PlatformFeatureSchemaIdempotentSql.EnsurePassportPdfDnaBlocksEnabled);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Passport PDF DNA blocks column ensure failed; migrate may repair.");
        }
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        await MigrateAsync(services);
        await SeedDataAsync(services);
    }

    public static async Task EnsureFeatureFlagAdminOverrideColumnsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("JobsyDbSeeder");
        try
        {
            await db.Database.ExecuteSqlRawAsync(PlatformFeatureSchemaIdempotentSql.EnsureFeatureFlagAdminOverrides);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Feature flag admin override columns ensure failed; migrate may repair.");
        }
    }

    public static async Task MigrateAsync(IServiceProvider services)
    {
        await EnsurePassportPdfDnaBlocksColumnAsync(services);
        await EnsureFeatureFlagAdminOverrideColumnsAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("JobsyDbSeeder");

        var knownMigrations = db.Database.GetMigrations().ToList();
        if (knownMigrations.Count > 0)
        {
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync(Migrations.PupilPassportV2SchemaIdempotentSql.EnsureColumns);
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

        try
        {
            await BeheerTodoSeeder.SeedAsync(db);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Beheer todo seed after migrate failed; continuing.");
        }

        try
        {
            await scope.ServiceProvider.GetRequiredService<Jobsy.Core.Interfaces.ITrainingUpskillService>()
                .EnsureDefaultsAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Training catalog seed after migrate failed; continuing.");
        }

        try
        {
            await CompetencyTagBackfillSeeder.BackfillAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Competency tag backfill after migrate failed; continuing.");
        }

        try
        {
            await MockVacancyCopyBackfill.BackfillAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Mock vacancy copy backfill after migrate failed; continuing.");
        }

        try
        {
            await AtsScrapeSourceSeeder.SeedAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ATS scrape source seed after migrate failed; continuing.");
        }

        try
        {
            await CompanyVerificationBackfillLog.EnsureLoggedAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Company verification backfill PlatformLog failed; continuing.");
        }

        try
        {
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var allowBedrijvenkaartDemo = environment.IsDevelopment()
                                          || configuration.GetValue("Seed:Enabled", false);
            if (allowBedrijvenkaartDemo && await db.Companies.AnyAsync())
            {
                await DemoUsersSeeder.SeedUsersAsync(db, logger);
                await DemoCompanyMapInitiativeSeeder.EnsureBakkerijBrandingAsync(db, logger);
                await DemoCompanyMapInitiativeSeeder.SeedAsync(db, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bedrijvenkaart demo ensure after migrate failed; continuing.");
        }
    }

    public static async Task SeedDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("JobsyDbSeeder");
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var perfCount = configuration.GetValue("Seed:BanenkaartPerfCount", 0);

        if (await db.Companies.AnyAsync())
        {
            // Users first so salesmanager demo account exists even if later seeders fail.
            await DemoUsersSeeder.SeedUsersAsync(db, logger);

            try
            {
                await DemoCompanyMapInitiativeSeeder.EnsureBakkerijBrandingAsync(db, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bakkerij De Korenaar demo branding ensure failed; continuing.");
            }

            // Hierarchy / salary-table demo fixes before wage sync.
            try
            {
                await ReferenceVerificationDemoSeeder.EnsureAsync(db, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Reference verification demo seed failed; continuing.");
            }

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
                await AtsScrapeSourceSeeder.SeedAsync(db, logger);
                await ApplicationsAndWagesSeeder.EnsureDemoCandidateApplicationCardsAsync(db, logger);
                await BanenkaartPerfVacanciesSeeder.SeedAsync(db, logger, perfCount);
                await DemoCompanyMapInitiativeSeeder.EnsureBakkerijBrandingAsync(db, logger);
                await DemoCompanyMapInitiativeSeeder.SeedAsync(db, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Partial seed failure after demo users; continuing.");
            }

            return;
        }

        await DemoCompaniesSeeder.SeedCompaniesAsync(db, logger);
        await DemoUsersSeeder.SeedUsersAsync(db, logger);
        await DemoCompanyMapInitiativeSeeder.EnsureBakkerijBrandingAsync(db, logger);
        await DemoCompanyMapInitiativeSeeder.SeedAsync(db, logger);
        try
        {
            await ReferenceVerificationDemoSeeder.EnsureAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reference verification demo seed failed; continuing.");
        }

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
            await AtsScrapeSourceSeeder.SeedAsync(db, logger);
            await ApplicationsAndWagesSeeder.EnsureDemoCandidateApplicationCardsAsync(db, logger);
            await BanenkaartPerfVacanciesSeeder.SeedAsync(db, logger, perfCount);
            logger.LogInformation("Seed completed: employers + intermediary, vacancies, tokens, role users, sprint-0/8 demo.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Partial seed failure after companies/users; continuing.");
        }
    }
}
