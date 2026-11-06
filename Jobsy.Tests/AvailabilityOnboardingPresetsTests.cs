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
        Assert.Contains("Za:Ochtend", availability);
    }

    [Fact]
    public void Selecting_weekend_from_custom_manual_keeps_matrix_when_overridden()
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
        Assert.True(overridden);
        Assert.False(fineTune);
        Assert.Contains("Ma:Nacht", availability);
    }

    [Fact]
    public void Multiple_named_presets_can_be_selected_together()
    {
        var presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var availability = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var overridden = false;
        var fineTune = false;
        decimal? min = 12;
        decimal? max = 40;

        AvailabilityOnboardingPresets.Toggle("weekend", presets, availability, ref overridden, ref fineTune, ref min, ref max);
        AvailabilityOnboardingPresets.Toggle("evening", presets, availability, ref overridden, ref fineTune, ref min, ref max);

        Assert.Contains("weekend", presets);
        Assert.Contains("evening", presets);
        Assert.True(availability.Count > 0);
    }

    [Fact]
    public void Manual_edit_promotes_to_custom_and_keeps_fine_tune_open()
    {
        var presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "office" };
        var overridden = false;
        var fineTune = false;

        AvailabilityOnboardingPresets.MarkManualEdit(presets, ref overridden, ref fineTune);

        Assert.Single(presets);
        Assert.Contains(AvailabilityPresetRules.Custom, presets);
        Assert.True(overridden);
        Assert.True(fineTune);
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
