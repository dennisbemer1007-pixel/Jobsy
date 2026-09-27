using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class AvailabilityPresetRulesTests
{
    [Fact]
    public void School_weekend_and_direct_are_combined_deterministically()
    {
        var a = AvailabilityPresetRules.Compute(new HashSet<string> { "school", "weekend", "direct" });
        var b = AvailabilityPresetRules.Compute(new HashSet<string> { "direct", "weekend", "school" });

        Assert.Equal(6, a.MinHours);
        Assert.Equal(16, a.MaxHours);
        Assert.True(a.Immediate);
        Assert.Equal(a.DayParts.OrderBy(x => x), b.DayParts.OrderBy(x => x));
        Assert.Contains("Za:Ochtend", a.DayParts);
        Assert.Contains("Zo:Middag", a.DayParts);
    }

    [Theory]
    [InlineData("school", 6, 16, 7)]
    [InlineData("weekend", 8, 16, 4)]
    [InlineData("evening", 8, 20, 5)]
    [InlineData("office", 32, 40, 10)]
    [InlineData("holiday", 16, 40, 10)]
    public void Day_specific_presets_have_documented_values(string preset, decimal min, decimal max, int dayParts)
    {
        var result = AvailabilityPresetRules.Compute(new HashSet<string> { preset });
        Assert.Equal(min, result.MinHours);
        Assert.Equal(max, result.MaxHours);
        Assert.Equal(dayParts, result.DayParts.Count);
    }

    [Fact]
    public void Parttime_does_not_add_default_days_when_specific_days_exist()
    {
        var result = AvailabilityPresetRules.Compute(new HashSet<string> { "weekend", "parttime" });
        Assert.Equal(4, result.DayParts.Count);
        Assert.DoesNotContain("Ma:Ochtend", result.DayParts);
    }
}
