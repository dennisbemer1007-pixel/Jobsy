using Jobsy.Core.Rules;

namespace Jobsy.Tests.Werkgever;

public class WerkgeverDashboardRulesTests
{
    [Fact]
    public void Branch_status_achterstand_on_overdue_count()
    {
        var status = WerkgeverDashboardRules.ResolveBranchStatus(
            overduePendingCount: 6,
            avgFirstResponseDays: 1,
            hasActiveManager: true,
            tokenBalance: 100);
        Assert.Equal(WerkgeverDashboardRules.BranchHealthStatus.Achterstand, status);
    }

    [Fact]
    public void Branch_status_achterstand_on_slow_response()
    {
        var status = WerkgeverDashboardRules.ResolveBranchStatus(
            overduePendingCount: 0,
            avgFirstResponseDays: 3.1,
            hasActiveManager: true,
            tokenBalance: 100);
        Assert.Equal(WerkgeverDashboardRules.BranchHealthStatus.Achterstand, status);
    }

    [Fact]
    public void Branch_status_aandacht_no_manager_or_low_tokens_or_slow()
    {
        Assert.Equal(
            WerkgeverDashboardRules.BranchHealthStatus.Aandacht,
            WerkgeverDashboardRules.ResolveBranchStatus(0, 1, hasActiveManager: false, 100));
        Assert.Equal(
            WerkgeverDashboardRules.BranchHealthStatus.Aandacht,
            WerkgeverDashboardRules.ResolveBranchStatus(0, 1, true, 5));
        Assert.Equal(
            WerkgeverDashboardRules.BranchHealthStatus.Aandacht,
            WerkgeverDashboardRules.ResolveBranchStatus(0, 2.1, true, 100));
    }

    [Fact]
    public void Branch_status_opschema_otherwise()
    {
        Assert.Equal(
            WerkgeverDashboardRules.BranchHealthStatus.OpSchema,
            WerkgeverDashboardRules.ResolveBranchStatus(2, 1.5, true, 20));
    }

    [Fact]
    public void First_response_null_below_five_samples()
    {
        Assert.Null(WerkgeverDashboardRules.AverageFirstResponseHours([1, 2, 3, 4]));
        Assert.Equal(3d, WerkgeverDashboardRules.AverageFirstResponseHours([1, 2, 3, 4, 5]));
    }

    [Fact]
    public void Token_runway_null_without_spend()
    {
        Assert.Null(WerkgeverDashboardRules.TokenRunwayWeeks(100, 0));
        Assert.Equal(5d, WerkgeverDashboardRules.TokenRunwayWeeks(40, 64, lookbackWeeks: 8));
    }

    [Theory]
    [InlineData(null, "30d")]
    [InlineData("7d", "7d")]
    [InlineData("90d", "90d")]
    [InlineData("x", "30d")]
    public void Normalize_period(string? input, string expected)
        => Assert.Equal(expected, WerkgeverDashboardRules.NormalizePeriod(input));
}
