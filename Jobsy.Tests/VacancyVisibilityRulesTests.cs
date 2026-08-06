using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Tests;

public class VacancyVisibilityRulesTests
{
    [Fact]
    public void Public_visibility_matches_shared_rules()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var live = CreateVacancy("Live", VacancyStatus.Active, today);
        var draft = CreateVacancy("Draft", VacancyStatus.Draft, today);

        Assert.True(VacancyVisibilityRules.IsPubliclyVisible(live, today));
        Assert.False(VacancyVisibilityRules.IsPubliclyVisible(draft, today));
        Assert.True(VacancyVisibilityRules.CanAcceptApplications(live, today, 0));
    }

    [Fact]
    public void Token_spend_amounts_should_come_from_configured_costs()
    {
        var costs = new Dictionary<TokenSpendReason, decimal>
        {
            [TokenSpendReason.Publish] = 1m,
            [TokenSpendReason.Highlight] = VacancyProductRules.DefaultHighlightCostTokens,
            [TokenSpendReason.PushBom] = 3m,
            [TokenSpendReason.Extend] = 1m
        };

        Assert.Equal(1m, costs[TokenSpendReason.Publish]);
        Assert.Equal(2m, costs[TokenSpendReason.Highlight]);
        Assert.True(costs.Values.All(v => v > 0));
    }

    private static Vacancy CreateVacancy(string title, VacancyStatus status, DateOnly today) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = title,
        HourlyWage = 14m,
        StartDate = today,
        EndDate = today.AddMonths(1),
        Status = status,
        CompanyId = Guid.NewGuid(),
        Location = new GeoPoint(52.0, 4.3),
        RequiredTransport = TransportMode.Bike,
        MaxApplications = 5
    };
}
