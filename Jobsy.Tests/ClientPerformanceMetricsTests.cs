using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public sealed class ClientPerformanceMetricsTests
{
    [Fact]
    public async Task Client_performance_aggregates_per_company()
    {
        await using var db = CreateDb();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var vacancyA = Guid.NewGuid();
        var vacancyB = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        db.Companies.AddRange(
            new Company
            {
                Id = companyA,
                Name = "Alpha Bakkerij",
                KvkNumber = "11111111",
                Address = "A",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Employer
            },
            new Company
            {
                Id = companyB,
                Name = "Beta Magazijn",
                KvkNumber = "22222222",
                Address = "B",
                Location = new GeoPoint(52.1, 4.1),
                Type = CompanyType.Employer
            });

        db.Vacancies.AddRange(
            new Vacancy
            {
                Id = vacancyA,
                Title = "Bakker",
                Description = "x",
                HourlyWage = 14,
                StartDate = today.AddDays(-10),
                EndDate = today.AddDays(3),
                Status = VacancyStatus.Active,
                CompanyId = companyA,
                Location = new GeoPoint(52, 4),
                RequiredTransport = TransportMode.Bike,
                IsHighlighted = true,
                HighlightedUntil = now.AddDays(2)
            },
            new Vacancy
            {
                Id = vacancyB,
                Title = "Picker",
                Description = "x",
                HourlyWage = 13,
                StartDate = today.AddDays(-5),
                EndDate = today.AddDays(40),
                Status = VacancyStatus.Active,
                CompanyId = companyB,
                Location = new GeoPoint(52.1, 4.1),
                RequiredTransport = TransportMode.Car
            });

        db.Applications.AddRange(
            new Application
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyA,
                CandidateName = "Open A",
                CandidateEmail = "a-open@example.com",
                CandidateCity = "Delft",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 12,
                Status = ApplicationStatus.Pending,
                CreatedAt = now.AddHours(-3),
                EmailVerifiedAt = now.AddHours(-3)
            },
            new Application
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyA,
                CandidateName = "Ok A",
                CandidateEmail = "a-ok@example.com",
                CandidateCity = "Rijswijk",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 18,
                Status = ApplicationStatus.Accepted,
                CreatedAt = now.AddHours(-2),
                EmailVerifiedAt = now.AddHours(-2),
                RespondedAt = now.AddHours(-1)
            },
            new Application
            {
                Id = Guid.NewGuid(),
                VacancyId = vacancyB,
                CandidateName = "Ok B",
                CandidateEmail = "b-ok@example.com",
                CandidateCity = "Den Haag",
                PreferredTransport = "Auto",
                EstimatedTravelMinutes = 25,
                Status = ApplicationStatus.Accepted,
                CreatedAt = now.AddHours(-1),
                EmailVerifiedAt = now.AddHours(-1),
                RespondedAt = now.AddMinutes(-30)
            });

        db.VacancyClicks.AddRange(
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyA, AnonymousKey = "a1", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyA, AnonymousKey = "a2", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyA, AnonymousKey = "a3", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyA, AnonymousKey = "a4", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyB, AnonymousKey = "b1", CreatedAt = now },
            new VacancyClick { Id = Guid.NewGuid(), VacancyId = vacancyB, AnonymousKey = "b2", CreatedAt = now });

        db.TokenTransactions.AddRange(
            new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyA,
                Kind = TokenTransactionKind.Purchase,
                Reason = TokenSpendReason.None,
                Amount = 2m,
                CreatedAt = now.AddDays(-2)
            },
            new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyB,
                Kind = TokenTransactionKind.Purchase,
                Reason = TokenSpendReason.None,
                Amount = 12m,
                CreatedAt = now.AddDays(-2)
            });

        await db.SaveChangesAsync();

        var sut = new MetricsQueryService(db);
        var board = await sut.GetClientPerformanceAsync([companyA, companyB], period: "month");

        Assert.Equal(2, board.Clients.Count);

        var alpha = Assert.Single(board.Clients, c => c.CompanyId == companyA);
        Assert.Equal(1, alpha.ActiveVacancies);
        Assert.Equal(1, alpha.ApplicationsPending);
        Assert.Equal(4, alpha.Clicks);
        Assert.Equal(2, alpha.Applications);
        Assert.Equal(50m, alpha.ConversionRate);
        Assert.Equal(15m, alpha.AvgTravelMinutes);
        Assert.Equal("Fiets", alpha.TopTransportMode);
        Assert.Equal(100m, alpha.TopTransportShare);
        Assert.Equal(2m, alpha.TokenBalance);
        Assert.Equal(1, alpha.ActiveBoosts);
        Assert.Equal(1, alpha.ExpiringWithin5Days);

        var beta = Assert.Single(board.Clients, c => c.CompanyId == companyB);
        Assert.Equal(1, beta.ActiveVacancies);
        Assert.Equal(0, beta.ApplicationsPending);
        Assert.Equal(2, beta.Clicks);
        Assert.Equal(1, beta.Applications);
        Assert.Equal(50m, beta.ConversionRate);
        Assert.Equal(25m, beta.AvgTravelMinutes);
        Assert.Equal("Auto", beta.TopTransportMode);
        Assert.Equal(12m, beta.TokenBalance);
        Assert.Equal(0, beta.ActiveBoosts);
        Assert.Equal(0, beta.ExpiringWithin5Days);

        // Action-required companies sort first.
        Assert.Equal(companyA, board.Clients[0].CompanyId);
    }

    [Fact]
    public void Status_badge_priority_is_action_then_low_tokens_then_healthy()
    {
        Assert.Equal(
            ClientPerformanceBadge.ActionRequired,
            ClientPerformanceStatus.Resolve(applicationsPending: 1, expiringWithin5Days: 0, tokenBalance: 10m));
        Assert.Equal(
            ClientPerformanceBadge.ActionRequired,
            ClientPerformanceStatus.Resolve(applicationsPending: 0, expiringWithin5Days: 2, tokenBalance: 10m));
        Assert.Equal(
            ClientPerformanceBadge.LowTokens,
            ClientPerformanceStatus.Resolve(applicationsPending: 0, expiringWithin5Days: 0, tokenBalance: 2m));
        Assert.Equal(
            ClientPerformanceBadge.Healthy,
            ClientPerformanceStatus.Resolve(applicationsPending: 0, expiringWithin5Days: 0, tokenBalance: 3m));
        Assert.Equal("danger", ClientPerformanceStatus.CssModifier(ClientPerformanceBadge.ActionRequired));
        Assert.Equal("warn", ClientPerformanceStatus.CssModifier(ClientPerformanceBadge.LowTokens));
        Assert.Equal("ok", ClientPerformanceStatus.CssModifier(ClientPerformanceBadge.Healthy));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
