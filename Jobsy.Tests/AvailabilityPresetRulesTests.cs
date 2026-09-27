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

    [Fact]
    public void Parttime_alone_adds_weekday_dayparts()
    {
        var result = AvailabilityPresetRules.Compute(new HashSet<string> { "parttime" });
        Assert.Equal(12, result.MinHours);
        Assert.Equal(32, result.MaxHours);
        Assert.Contains("Ma:Ochtend", result.DayParts);
        Assert.Equal(10, result.DayParts.Count);
    }

    [Fact]
    public void Combinations_are_order_independent_for_common_sets()
    {
        string[][] sets =
        [
            ["school", "evening"],
            ["office", "parttime", "direct"],
            ["weekend", "fulltime", "holiday"]
        ];

        foreach (var set in sets)
        {
            var baseline = AvailabilityPresetRules.Compute(set.ToHashSet(StringComparer.OrdinalIgnoreCase));
            foreach (var perm in Permute(set))
            {
                var result = AvailabilityPresetRules.Compute(perm.ToHashSet(StringComparer.OrdinalIgnoreCase));
                Assert.Equal(baseline.MinHours, result.MinHours);
                Assert.Equal(baseline.MaxHours, result.MaxHours);
                Assert.Equal(baseline.Immediate, result.Immediate);
                Assert.Equal(
                    baseline.DayParts.OrderBy(x => x, StringComparer.Ordinal),
                    result.DayParts.OrderBy(x => x, StringComparer.Ordinal));
            }
        }
    }

    private static IEnumerable<string[]> Permute(string[] items)
    {
        if (items.Length == 0)
        {
            yield return [];
            yield break;
        }

        for (var i = 0; i < items.Length; i++)
        {
            var head = items[i];
            var rest = items.Where((_, idx) => idx != i).ToArray();
            foreach (var tail in Permute(rest))
            {
                yield return [head, .. tail];
            }
        }
    }
}
