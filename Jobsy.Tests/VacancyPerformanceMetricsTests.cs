using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public sealed class VacancyPerformanceMetricsTests
{
    [Fact]
    public async Task GetVacancyPerformance_ranks_top_and_flop_by_clicks()
    {
        await using var db = CreateDb();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            Type = CompanyType.Employer,
            KvkNumber = "12345678",
            Address = "Test",
            Location = new GeoPoint(52, 4)
        };
        db.Companies.Add(company);

        var hot = MakeVacancy(company.Id, "Hot job");
        var mid = MakeVacancy(company.Id, "Mid job");
        var cold = MakeVacancy(company.Id, "Cold job");
        db.Vacancies.AddRange(hot, mid, cold);

        var now = DateTime.UtcNow;
        db.VacancyClicks.AddRange(
            Enumerable.Range(0, 5).Select(_ => new VacancyClick { Id = Guid.NewGuid(), VacancyId = hot.Id, CreatedAt = now.AddHours(-1) }));
        db.VacancyClicks.AddRange(
            Enumerable.Range(0, 2).Select(_ => new VacancyClick { Id = Guid.NewGuid(), VacancyId = mid.Id, CreatedAt = now.AddHours(-2) }));
        db.VacancyClicks.Add(new VacancyClick { Id = Guid.NewGuid(), VacancyId = cold.Id, CreatedAt = now.AddHours(-3) });
        db.VacancySearchImpressions.AddRange(
            new VacancySearchImpression { Id = Guid.NewGuid(), VacancyId = hot.Id, CreatedAt = now.AddHours(-1) },
            new VacancySearchImpression { Id = Guid.NewGuid(), VacancyId = cold.Id, CreatedAt = now.AddHours(-1) });

        await db.SaveChangesAsync();

        var service = new MetricsQueryService(db);
        var board = await service.GetVacancyPerformanceAsync([company.Id], "week", take: 1);

        Assert.Equal("week", board.Period);
        Assert.Single(board.Top);
        Assert.Equal(hot.Id, board.Top[0].VacancyId);
        Assert.Equal(5, board.Top[0].Clicks);
        Assert.Single(board.Flop);
        Assert.Equal(cold.Id, board.Flop[0].VacancyId);
        Assert.Equal(1, board.Flop[0].Clicks);
    }

    [Fact]
    public async Task GetSummary_attaches_sparkline_for_clicks()
    {
        await using var db = CreateDb();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            Type = CompanyType.Employer,
            KvkNumber = "12345678",
            Address = "Test",
            Location = new GeoPoint(52, 4)
        };
        db.Companies.Add(company);
        var vacancy = MakeVacancy(company.Id, "Spark");
        db.Vacancies.Add(vacancy);
        db.VacancyClicks.Add(new VacancyClick
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancy.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var service = new MetricsQueryService(db);
        var metrics = await service.GetSummaryAsync(includePlatformOnly: false, [company.Id], "week");
        var clicks = Assert.Single(metrics, m => m.Key == "clicks");
        Assert.NotNull(clicks.Sparkline);
        Assert.Equal(7, clicks.Sparkline!.Count);
        Assert.Equal(1, clicks.Sparkline.Sum());
    }

    private static Vacancy MakeVacancy(Guid companyId, string title) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "d",
        CompanyId = companyId,
        Status = VacancyStatus.Active,
        HourlyWage = 14,
        Location = new GeoPoint(52, 4),
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
    };

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
