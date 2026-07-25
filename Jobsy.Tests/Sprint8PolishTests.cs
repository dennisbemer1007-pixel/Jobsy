using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class Sprint8PolishTests
{
    [Fact]
    public async Task Sprint8_seed_fills_spend_pushbom_extension_and_error_metrics()
    {
        await using var db = CreateDb();
        await JobsyDbSeederHarness.SeedFreshAsync(db);

        var metrics = await new MetricsQueryService(db)
            .GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "month");

        Assert.True(metrics.First(m => m.Key == "tokens_spent").Value > 0);
        Assert.True(metrics.First(m => m.Key == "pushboms").Value > 0);
        Assert.True(metrics.First(m => m.Key == "extensions").Value > 0);
        Assert.True(metrics.First(m => m.Key == "errors").Value > 0);
        Assert.True(metrics.First(m => m.Key == "active_vacancies_intermediaries").Value >= 1);
        Assert.True(metrics.First(m => m.Key == "clicks").Value > 0);
        Assert.True(metrics.First(m => m.Key == "shares").Value > 0);
        Assert.True(await db.Vacancies.AnyAsync(v => v.Status == VacancyStatus.Draft));
        Assert.True(await db.Vacancies.AnyAsync(v => v.Status == VacancyStatus.PendingApproval));
        Assert.True(await db.TokenTransactions.AnyAsync(t => t.Kind == TokenTransactionKind.Allocation));
    }

    [Fact]
    public async Task Sprint8_seed_is_idempotent()
    {
        await using var db = CreateDb();
        await JobsyDbSeederHarness.SeedFreshAsync(db);
        var spendsBefore = await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend);
        var logsBefore = await db.PlatformLogs.CountAsync();

        await Sprint8MetricsSeeder.SeedRichMetricsAsync(db, NullLogger.Instance);

        Assert.Equal(spendsBefore, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
        Assert.Equal(logsBefore, await db.PlatformLogs.CountAsync());
    }

    [Fact]
    public async Task Month_period_includes_time_spread_engagement()
    {
        await using var db = CreateDb();
        await JobsyDbSeederHarness.SeedFreshAsync(db);

        var day = await new MetricsQueryService(db)
            .GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "day");
        var month = await new MetricsQueryService(db)
            .GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "month");

        Assert.True(month.First(m => m.Key == "clicks").Value
                    >= day.First(m => m.Key == "clicks").Value);
        Assert.True(month.First(m => m.Key == "tokens_spent").Value
                    >= day.First(m => m.Key == "tokens_spent").Value);
    }

    [Fact]
    public async Task Stock_metric_keys_have_non_empty_drilldown_when_summary_positive()
    {
        await using var db = CreateDb();
        await JobsyDbSeederHarness.SeedFreshAsync(db);

        var sut = new MetricsQueryService(db);
        var summary = await sut.GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "month");
        foreach (var key in new[]
                 {
                     "active_vacancies", "active_vacancies_employers", "active_vacancies_intermediaries",
                     "users_open_for_work", "users_active", "companies_employers", "companies_intermediaries"
                 })
        {
            var count = summary.First(m => m.Key == key).Value;
            Assert.True(count > 0, $"{key} summary should be > 0 after seed");
            var drill = await sut.GetDrilldownAsync(key, includePlatformOnly: true, companyIds: null, period: "month");
            Assert.True(drill.Count > 0, $"{key} drilldown should list items");
        }
    }

    [Fact]
    public async Task Applications_drilldown_redacts_name_for_employers_until_accepted()
    {
        await using var db = CreateDb();
        await JobsyDbSeederHarness.SeedFreshAsync(db);

        var sut = new MetricsQueryService(db);
        var employer = await sut.GetDrilldownAsync(
            "applications", includePlatformOnly: false, companyIds: null, period: "month");
        var pendingish = employer.Where(i => i.Subtitle?.Contains("Pending") == true).ToList();
        Assert.NotEmpty(pendingish);
        Assert.All(pendingish, i => Assert.DoesNotContain("@", i.Title));

        var admin = await sut.GetDrilldownAsync(
            "applications", includePlatformOnly: true, companyIds: null, period: "month");
        Assert.Contains(admin, i => i.Title.Contains(' ') || i.Title.Length > 3);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}

/// <summary>
/// Runs the same seed pipeline as production without Migrate (in-memory).
/// </summary>
internal static class JobsyDbSeederHarness
{
    public static async Task SeedFreshAsync(JobsyDbContext db)
    {
        var logger = NullLogger.Instance;
        await DemoCompaniesSeeder.SeedCompaniesAsync(db, logger);
        await DemoUsersSeeder.SeedUsersAsync(db, logger);
        await ApplicationsAndWagesSeeder.SeedApplicationsAndWagesAsync(db, logger);
        await PlatformSettingsSeeder.SeedPlatformSettingsAsync(db, logger);
        await Sprint0DemoSeeder.SeedSprint0DemoAsync(db, logger);
        await Sprint8MetricsSeeder.SeedRichMetricsAsync(db, logger);
    }
}
