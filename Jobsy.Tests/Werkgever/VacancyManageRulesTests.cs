using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Werkgever;

namespace Jobsy.Tests.Werkgever;

public class VacancyManageRulesTests
{
    [Theory]
    [InlineData("Active", "actief", true)]
    [InlineData("PendingApproval", "wacht", true)]
    [InlineData("Draft", "concept", true)]
    [InlineData("Archived", "gesloten", true)]
    [InlineData("Fulfilled", "gesloten", true)]
    [InlineData("Active", "concept", false)]
    public void MatchesTab_status(string status, string tab, bool expected)
    {
        var v = new VacancyListItem { Status = status, EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)) };
        Assert.Equal(expected, VacancyManageRules.MatchesTab(v, tab, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void Verloopt_tab_uses_EndDate_window()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soon = new VacancyListItem
        {
            Status = "Active",
            EndDate = today.AddDays(3)
        };
        var later = new VacancyListItem
        {
            Status = "Active",
            EndDate = today.AddDays(30)
        };
        Assert.True(VacancyManageRules.MatchesTab(soon, "verloopt", today));
        Assert.False(VacancyManageRules.MatchesTab(later, "verloopt", today));
    }

    [Fact]
    public void EmploymentType_from_hours_and_flex()
    {
        Assert.Equal("Flex", VacancyManageRules.EmploymentTypeLabel(new VacancyListItem { FlexibleTimes = true }));
        Assert.Equal("Fulltime", VacancyManageRules.EmploymentTypeLabel(new VacancyListItem
        {
            MinHoursPerWeek = 36,
            MaxHoursPerWeek = 40
        }));
        Assert.Equal("Bijbaan", VacancyManageRules.EmploymentTypeLabel(new VacancyListItem
        {
            MinHoursPerWeek = 4,
            MaxHoursPerWeek = 8
        }));
    }

    [Fact]
    public void EstimatePendingTokens_sums_requested_options()
    {
        var costs = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Publish"] = 1m,
            ["Highlight"] = 2m,
            ["Extend"] = 1m,
            ["PushBom"] = 3m
        };
        var v = new VacancyListItem
        {
            RequestedHighlight = true,
            RequestedExtend = true,
            CategoryPublishCostTokens = 1m
        };
        Assert.Equal(2m, VacancyManageRules.EstimatePendingTokens(v, costs));
    }

    [Fact]
    public void BulkSummary_format()
        => Assert.Equal("2 gelukt, 1 niet gelukt", VacancyManageRules.BulkSummary(2, 1));

    [Fact]
    public void FormatShortName_initials()
        => Assert.Equal("S. Bakker", VacancyManageRules.FormatShortName("Sanne Bakker"));
}

public class VacancyDraftCompletenessCountTests
{
    [Fact]
    public void CountMissingFields_empty_draft()
    {
        var v = new Jobsy.Core.Entities.Vacancy
        {
            Title = "",
            Description = "",
            ContentModerationPassed = true
        };
        Assert.True(VacancyDraftCompletenessRules.CountMissingFields(v) >= 4);
        Assert.True(VacancyDraftCompletenessRules.IsIncomplete(v));
    }
}
