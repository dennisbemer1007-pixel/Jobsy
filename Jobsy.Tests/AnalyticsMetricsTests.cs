using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class AnalyticsMetricsTests
{
    [Fact]
    public async Task Summary_includes_impressions_for_managers_and_site_visits_for_admin()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Cafe",
            KvkNumber = "12345678",
            Address = "A",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "Barista",
            Description = "x",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30)),
            Status = VacancyStatus.Active,
            CompanyId = companyId,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.VacancySearchImpressions.Add(new VacancySearchImpression
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            AnonymousKey = "anon-11111111-1111-1111-1111-111111111111",
            CreatedAt = DateTime.UtcNow
        });
        db.VacancyClicks.Add(new VacancyClick
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            AnonymousKey = "anon-11111111-1111-1111-1111-111111111111",
            CreatedAt = DateTime.UtcNow
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateName = "Test Candidate",
            CandidateEmail = "c@example.com",
            CandidateCity = "Delft",
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        db.SiteVisits.AddRange(
            new SiteVisit
            {
                Id = Guid.NewGuid(),
                AnonymousKey = "anon-11111111-1111-1111-1111-111111111111",
                Path = "/",
                CreatedAt = DateTime.UtcNow
            },
            new SiteVisit
            {
                Id = Guid.NewGuid(),
                AnonymousKey = "anon-11111111-1111-1111-1111-111111111111",
                Path = "/vacancies",
                CreatedAt = DateTime.UtcNow
            },
            new SiteVisit
            {
                Id = Guid.NewGuid(),
                AnonymousKey = "anon-22222222-2222-2222-2222-222222222222",
                Path = "/",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var sut = new MetricsQueryService(db);
        var employer = await sut.GetSummaryAsync(includePlatformOnly: false, companyIds: [companyId], period: "month");
        var admin = await sut.GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "month");

        Assert.Equal(1, employer.First(m => m.Key == "impressions").Value);
        Assert.Equal(1, employer.First(m => m.Key == "clicks").Value);
        Assert.Equal(1, employer.First(m => m.Key == "applications").Value);
        Assert.DoesNotContain(employer, m => m.Key == "site_visits");

        Assert.Equal(1, admin.First(m => m.Key == "impressions").Value);
        Assert.Equal(1, admin.First(m => m.Key == "clicks").Value);
        Assert.Equal(1, admin.First(m => m.Key == "applications").Value);
        Assert.Equal(3, admin.First(m => m.Key == "site_visits").Value);
        Assert.Equal(2, admin.First(m => m.Key == "site_visits_unique").Value);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
