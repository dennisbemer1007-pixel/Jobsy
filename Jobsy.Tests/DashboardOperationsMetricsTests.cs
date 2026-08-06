using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public sealed class DashboardOperationsMetricsTests
{
    [Fact]
    public async Task Summary_includes_operations_kpis_for_employer_scope()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Bakkerij",
            KvkNumber = "87654321",
            Address = "A",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "Broodjessnijder",
            Description = "x",
            HourlyWage = 13,
            StartDate = DateOnly.FromDateTime(now.Date),
            EndDate = DateOnly.FromDateTime(now.Date.AddDays(30)),
            Status = VacancyStatus.Active,
            CompanyId = companyId,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike,
            IsHighlighted = true,
            HighlightedUntil = now.AddDays(5)
        });
        db.Applications.AddRange(
            new Application
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyId,
                CandidateName = "Open Candidate",
                CandidateEmail = "open@example.com",
                CandidateCity = "Delft",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 10,
                Status = ApplicationStatus.Pending,
                CreatedAt = now.AddHours(-2),
                EmailVerifiedAt = now.AddHours(-2)
            },
            new Application
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyId,
                CandidateName = "Accepted Candidate",
                CandidateEmail = "ok@example.com",
                CandidateCity = "Rijswijk",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 14,
                Status = ApplicationStatus.Accepted,
                CreatedAt = now.AddHours(-1),
                EmailVerifiedAt = now.AddHours(-1),
                RespondedAt = now.AddMinutes(-30)
            },
            new Application
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyId,
                CandidateName = "Car Candidate",
                CandidateEmail = "car@example.com",
                CandidateCity = "Den Haag",
                PreferredTransport = "Auto",
                EstimatedTravelMinutes = 18,
                Status = ApplicationStatus.Pending,
                CreatedAt = now.AddMinutes(-20),
                EmailVerifiedAt = now.AddMinutes(-20)
            });
        db.VacancyClicks.AddRange(
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a1", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a2", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a3", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a4", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a5", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a6", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a7", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a8", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a9", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyId, AnonymousKey = "a10", CreatedAt = now });
        db.TokenTransactions.AddRange(
            new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Kind = TokenTransactionKind.Purchase,
                Reason = TokenSpendReason.None,
                Amount = 10m,
                CreatedAt = now.AddDays(-1)
            },
            new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Kind = TokenTransactionKind.Spend,
                Reason = TokenSpendReason.Publish,
                Amount = -1m,
                CreatedAt = now.AddHours(-3)
            },
            new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Kind = TokenTransactionKind.Spend,
                Reason = TokenSpendReason.PushBom,
                Amount = -3m,
                CreatedAt = now.AddHours(-2)
            });
        await db.SaveChangesAsync();

        var sut = new MetricsQueryService(db);
        var summary = await sut.GetSummaryAsync(includePlatformOnly: false, companyIds: [companyId], period: "month");

        Assert.Equal(2, summary.First(m => m.Key == "applications_pending").Value);
        Assert.Equal(3, summary.First(m => m.Key == "applications").Value);
        Assert.Equal(30m, summary.First(m => m.Key == "conversion_rate").Value); // 3/10
        Assert.Equal(6m, summary.First(m => m.Key == "tokens_balance").Value); // 10 - 1 - 3
        Assert.Equal(1, summary.First(m => m.Key == "active_boosts").Value);
        Assert.Equal(1, summary.First(m => m.Key == "pushboms").Value);
        Assert.Equal(14m, summary.First(m => m.Key == "avg_travel_minutes").Value); // (10+14+18)/3
        Assert.Equal(67m, summary.First(m => m.Key == "top_transport_share").Value); // 2/3 fiets
        Assert.Contains("Fiets", summary.First(m => m.Key == "top_transport_share").Label, StringComparison.OrdinalIgnoreCase);

        var pendingDrill = await sut.GetDrilldownAsync(
            "applications_pending", includePlatformOnly: false, companyIds: [companyId], period: "month");
        Assert.Equal(2, pendingDrill.Count);
        Assert.All(pendingDrill, item => Assert.Contains("Pending", item.Subtitle ?? "", StringComparison.Ordinal));
        // AVG: never leak city/name before Accept in drilldowns.
        Assert.All(pendingDrill, item => Assert.Equal("Kandidaat", item.Title));
        Assert.DoesNotContain(pendingDrill, item =>
            string.Equals(item.Title, "Delft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Title, "Rijswijk", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Title, "Den Haag", StringComparison.OrdinalIgnoreCase));

        var travelDrill = await sut.GetDrilldownAsync(
            "avg_travel_minutes", includePlatformOnly: false, companyIds: [companyId], period: "month");
        Assert.NotEmpty(travelDrill);
        Assert.Contains(travelDrill, item => item.Title == "Kandidaat");

        var boostDrill = await sut.GetDrilldownAsync(
            "active_boosts", includePlatformOnly: false, companyIds: [companyId], period: "month");
        Assert.Single(boostDrill);
        Assert.Equal("Broodjessnijder", boostDrill[0].Title);
    }

    [Fact]
    public void Catalog_places_operations_kpis_and_warns_on_backlog_and_low_tokens()
    {
        Assert.Equal("growth", MetricDashboardCatalog.CategoryIdFor("applications_pending"));
        Assert.Equal("growth", MetricDashboardCatalog.CategoryIdFor("conversion_rate"));
        Assert.Equal("growth", MetricDashboardCatalog.CategoryIdFor("tokens_balance"));
        Assert.Equal("engagement", MetricDashboardCatalog.CategoryIdFor("active_boosts"));
        Assert.Equal("engagement", MetricDashboardCatalog.CategoryIdFor("avg_travel_minutes"));
        Assert.Equal("engagement", MetricDashboardCatalog.CategoryIdFor("top_transport_share"));

        Assert.True(MetricDashboardCatalog.IsPercent("conversion_rate"));
        Assert.True(MetricDashboardCatalog.IsMinutes("avg_travel_minutes"));
        Assert.True(MetricDashboardCatalog.IsWarning("applications_pending", 1));
        Assert.False(MetricDashboardCatalog.IsWarning("applications_pending", 0));
        Assert.True(MetricDashboardCatalog.IsWarning("tokens_balance", 2));
        Assert.False(MetricDashboardCatalog.IsWarning("tokens_balance", 3));

        var metrics = new[]
        {
            new Jobsy.Web.Models.MetricCount { Key = "likes", Label = "Likes", Value = 1 },
            new Jobsy.Web.Models.MetricCount { Key = "applications_pending", Label = "Open", Value = 4 },
            new Jobsy.Web.Models.MetricCount { Key = "tokens_balance", Label = "Saldo", Value = 1 },
            new Jobsy.Web.Models.MetricCount { Key = "conversion_rate", Label = "Conversie", Value = 20 },
            new Jobsy.Web.Models.MetricCount { Key = "active_vacancies", Label = "Actief", Value = 9 }
        };

        var hero = MetricDashboardCatalog.HeroMetrics(metrics, m => m.Key, m => m.Value, max: 4);
        Assert.Equal(
            new[] { "applications_pending", "tokens_balance", "conversion_rate", "active_vacancies" },
            hero.Select(m => m.Key));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
