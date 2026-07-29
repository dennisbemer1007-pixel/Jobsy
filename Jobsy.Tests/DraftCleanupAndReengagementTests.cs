using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class DraftCleanupAndReengagementTests
{
    [Fact]
    public async Task Metrics_include_integration_and_unpublished_kpis()
    {
        await using var db = CreateDb();
        var orgId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = orgId,
            Name = "Org",
            KvkNumber = "12345678",
            Address = "A",
            Location = new GeoPoint(52, 4),
            CsvBatchImportEnabled = true,
            ReengagementEmailSentAtUtc = DateTime.UtcNow.AddDays(-10)
        });
        db.ApiKeys.Add(new ApiKey
        {
            Id = Guid.NewGuid(),
            CompanyId = orgId,
            ApiKeyHash = new string('a', 64),
            Name = "Key",
            KeyPrefix = "lob_test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = "Concept",
            Description = "Nog niet live",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            Status = VacancyStatus.Draft,
            CompanyId = orgId,
            CreatedVia = VacancySource.Csv,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
            Location = new GeoPoint(52, 4)
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = "Was live",
            Description = "Archief — mag nooit auto-opschoning",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-40)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            Status = VacancyStatus.Archived,
            CompanyId = orgId,
            CreatedVia = VacancySource.Api,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-40),
            PublishedAtUtc = DateTime.UtcNow.AddDays(-35),
            Location = new GeoPoint(52, 4)
        });
        await db.SaveChangesAsync();

        var metrics = new MetricsQueryService(db);
        var summary = await metrics.GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "week");
        Assert.Equal(1, summary.Single(m => m.Key == "companies_with_api").Value);
        Assert.Equal(1, summary.Single(m => m.Key == "companies_with_csv").Value);
        Assert.Equal(1, summary.Single(m => m.Key == "unpublished_vacancies").Value);
        Assert.Equal(1, summary.Single(m => m.Key == "reengagement_emails_sent").Value);

        var apiDrill = await metrics.GetDrilldownAsync("companies_with_api", true, null, "week");
        Assert.Single(apiDrill);
        Assert.Contains("API", apiDrill[0].Subtitle);
        Assert.Equal(1, apiDrill[0].Amount); // one archived API vacancy

        var csvDrill = await metrics.GetDrilldownAsync("companies_with_csv", true, null, "week");
        Assert.Single(csvDrill);
        Assert.Contains("CSV", csvDrill[0].Subtitle);
        Assert.Equal(1, csvDrill[0].Amount);

        var unpublished = await metrics.GetDrilldownAsync("unpublished_vacancies", true, null, "week");
        Assert.Single(unpublished);
        Assert.Equal("Concept", unpublished[0].Title);
    }

    [Fact]
    public void Draft_cleanup_rules_are_30_plus_14()
    {
        Assert.Equal(30, DraftVacancyCleanupRules.WarningAfterDays);
        Assert.Equal(14, DraftVacancyCleanupRules.DeleteAfterWarningDays);
        Assert.Equal(44, DraftVacancyCleanupRules.DeleteAfterDays);
    }

    [Fact]
    public async Task Never_published_draft_is_cleanup_candidate_archived_is_not()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Co",
            KvkNumber = "1",
            Address = "A",
            Location = new GeoPoint(52, 4)
        });

        var oldDraft = new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = "Oud concept",
            Description = "x",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Status = VacancyStatus.Draft,
            CompanyId = companyId,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-50),
            PublishedAtUtc = null,
            Location = new GeoPoint(52, 4)
        };
        var oldPublished = new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = "Oud gepubliceerd",
            Description = "x",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-100)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)),
            Status = VacancyStatus.Archived,
            CompanyId = companyId,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-100),
            PublishedAtUtc = DateTime.UtcNow.AddDays(-90),
            Location = new GeoPoint(52, 4)
        };
        db.Vacancies.AddRange(oldDraft, oldPublished);
        await db.SaveChangesAsync();

        var deleteBefore = DateTime.UtcNow.AddDays(-DraftVacancyCleanupRules.DeleteAfterDays);
        var deletable = await db.Vacancies
            .Where(v => v.Status == VacancyStatus.Draft
                        && v.PublishedAtUtc == null
                        && v.CreatedAtUtc <= deleteBefore)
            .Select(v => v.Title)
            .ToListAsync();

        Assert.Contains("Oud concept", deletable);
        Assert.DoesNotContain("Oud gepubliceerd", deletable);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("AdminKpis-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }
}
