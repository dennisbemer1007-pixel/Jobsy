using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class AvailabilityOnboardingPresetsTests
{
    [Fact]
    public void Custom_preset_opens_manual_mode_and_clears_other_presets()
    {
        var presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "weekend", "parttime" };
        var availability = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Za:Ochtend" };
        var overridden = false;
        var fineTune = false;
        decimal? min = 8;
        decimal? max = 16;

        AvailabilityOnboardingPresets.Toggle(
            AvailabilityPresetRules.Custom,
            presets,
            availability,
            ref overridden,
            ref fineTune,
            ref min,
            ref max);

        Assert.Single(presets);
        Assert.Contains(AvailabilityPresetRules.Custom, presets);
        Assert.True(overridden);
        Assert.True(fineTune);
        Assert.Empty(availability);
    }

    [Fact]
    public void Selecting_weekend_removes_custom_and_reapplies_slots()
    {
        var presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { AvailabilityPresetRules.Custom };
        var availability = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ma:Nacht" };
        var overridden = true;
        var fineTune = true;
        decimal? min = 8;
        decimal? max = 24;

        AvailabilityOnboardingPresets.Toggle("weekend", presets, availability, ref overridden, ref fineTune, ref min, ref max);

        Assert.DoesNotContain(AvailabilityPresetRules.Custom, presets);
        Assert.Contains("weekend", presets);
        Assert.False(overridden);
        Assert.False(fineTune);
        Assert.Contains("Za:Ochtend", availability);
        Assert.DoesNotContain("Ma:Nacht", availability);
    }

    [Fact]
    public void Custom_is_a_known_preset_with_no_default_day_parts()
    {
        Assert.True(AvailabilityPresetRules.IsKnown(AvailabilityPresetRules.Custom));
        var result = AvailabilityPresetRules.Compute(new HashSet<string> { AvailabilityPresetRules.Custom });
        Assert.Empty(result.DayParts);
        Assert.Null(result.MinHours);
        Assert.Null(result.MaxHours);
    }
}
