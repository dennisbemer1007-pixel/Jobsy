using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class AvailabilityStartRulesTests
{
    [Fact]
    public void Immediate_when_no_date()
    {
        Assert.True(AvailabilityStartRules.IsImmediate(null));
        Assert.True(AvailabilityStartRules.StepIsValid(true, null));
    }

    [Fact]
    public void From_date_requires_today_or_future()
    {
        var today = AvailabilityStartRules.TodayUtc;
        Assert.True(AvailabilityStartRules.IsValidExplicitStartDate(today));
        Assert.True(AvailabilityStartRules.IsValidExplicitStartDate(today.AddDays(14)));
        Assert.False(AvailabilityStartRules.IsValidExplicitStartDate(today.AddDays(-1)));
        Assert.True(AvailabilityStartRules.StepIsValid(false, today.AddDays(3)));
        Assert.False(AvailabilityStartRules.StepIsValid(false, today.AddDays(-2)));
        Assert.False(AvailabilityStartRules.StepIsValid(false, null));
    }

    [Fact]
    public void Min_date_input_is_today_utc()
    {
        Assert.Equal(
            AvailabilityStartRules.TodayUtc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            AvailabilityStartRules.MinDateInputValue);
    }
}
