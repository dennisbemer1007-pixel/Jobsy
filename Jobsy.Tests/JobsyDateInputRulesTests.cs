using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class JobsyDateInputRulesTests
{
    [Fact]
    public void Birth_date_bounds_exclude_too_recent_and_ancient()
    {
        var today = JobsyDateInputRules.TodayUtc;
        var (min, max) = JobsyDateInputRules.Bounds(JobsyDateConstraint.CandidateBirthDate);
        Assert.True(DateOnly.Parse(min!) <= today.AddYears(-100));
        Assert.True(DateOnly.Parse(max!) <= today.AddYears(-13));
    }

    [Fact]
    public void Month_input_round_trips_yyyy_mm()
    {
        Assert.True(JobsyDateInputRules.TryParseMonthInput("2024-08", out var value));
        Assert.Equal("2024-08", value);
        Assert.Equal("2024-08", JobsyDateInputRules.ToMonthInputValue(value));
    }
}
