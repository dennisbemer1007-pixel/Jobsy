using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class JobSearchActivityPeriodRulesTests
{
    [Fact]
    public void CurrentPeriod_Uses28DayWindowsFromAnchor()
    {
        var anchor = new DateOnly(2026, 10, 5);
        var (start, end) = JobSearchActivityPeriodRules.CurrentPeriod(anchor, new DateOnly(2026, 10, 20));
        Assert.Equal(anchor, start);
        Assert.Equal(new DateOnly(2026, 11, 1), end);
    }

    [Fact]
    public void CurrentPeriod_RollsToNextWindowAfterEnd()
    {
        var anchor = new DateOnly(2026, 10, 5);
        var (start, end) = JobSearchActivityPeriodRules.CurrentPeriod(anchor, new DateOnly(2026, 11, 3));
        Assert.Equal(new DateOnly(2026, 11, 2), start);
        Assert.Equal(new DateOnly(2026, 11, 29), end);
    }

    [Fact]
    public void ClampActivitiesPerPeriod_StaysInRange()
    {
        Assert.Equal(1, JobSearchActivityPeriodRules.ClampActivitiesPerPeriod(0));
        Assert.Equal(20, JobSearchActivityPeriodRules.ClampActivitiesPerPeriod(99));
        Assert.Equal(4, JobSearchActivityPeriodRules.ClampActivitiesPerPeriod(4));
    }
}
