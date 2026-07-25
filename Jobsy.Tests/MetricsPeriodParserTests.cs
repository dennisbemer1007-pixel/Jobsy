using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Tests;

public class MetricsPeriodParserTests
{
    [Theory]
    [InlineData(null, MetricsPeriod.Day)]
    [InlineData("", MetricsPeriod.Day)]
    [InlineData("day", MetricsPeriod.Day)]
    [InlineData("WEEK", MetricsPeriod.Week)]
    [InlineData("month", MetricsPeriod.Month)]
    [InlineData("quarter", MetricsPeriod.Quarter)]
    [InlineData("year", MetricsPeriod.Year)]
    [InlineData(" unknown ", MetricsPeriod.Day)]
    public void Parse_maps_expected_period(string? input, MetricsPeriod expected)
        => Assert.Equal(expected, MetricsPeriodParser.Parse(input));

    [Fact]
    public void ResolveRange_day_starts_at_utc_midnight()
    {
        var now = new DateTime(2026, 7, 24, 15, 30, 0, DateTimeKind.Utc);
        var (from, to) = MetricsPeriodParser.ResolveRange(MetricsPeriod.Day, now);
        Assert.Equal(now.Date, from);
        Assert.Equal(now, to);
    }

    [Fact]
    public void ResolveRange_week_is_seven_days_back()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var (from, _) = MetricsPeriodParser.ResolveRange(MetricsPeriod.Week, now);
        Assert.Equal(now.Date.AddDays(-7), from);
    }
}
