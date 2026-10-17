namespace Jobsy.Core.Rules;

/// <summary>
/// Shared preset toggle / apply logic for onboarding availability (wizard + discovery journey).
/// </summary>
public static class AvailabilityOnboardingPresets
{
    public static void Toggle(
        string code,
        HashSet<string> presets,
        HashSet<string> availability,
        ref bool presetsOverridden,
        ref bool fineTune,
        ref decimal? minHours,
        ref decimal? maxHours)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var normalized = code.Trim();
        if (string.Equals(normalized, AvailabilityPresetRules.Custom, StringComparison.OrdinalIgnoreCase))
        {
            ToggleCustom(presets, availability, ref presetsOverridden, ref fineTune);
            return;
        }

        if (presets.Remove(AvailabilityPresetRules.Custom))
        {
            fineTune = false;
            presetsOverridden = false;
        }

        if (!presets.Add(normalized))
        {
            presets.Remove(normalized);
        }

        if (!presetsOverridden)
        {
            ApplyComputed(presets, availability, ref minHours, ref maxHours);
        }
    }

    public static void ApplyComputed(
        HashSet<string> presets,
        HashSet<string> availability,
        ref decimal? minHours,
        ref decimal? maxHours)
    {
        if (presets.Contains(AvailabilityPresetRules.Custom))
        {
            return;
        }

        var result = AvailabilityPresetRules.Compute(presets, minHours, maxHours);
        minHours = result.MinHours;
        maxHours = result.MaxHours;
        availability.Clear();
        foreach (var dayPart in result.DayParts)
        {
            availability.Add(dayPart);
        }
    }

    /// <summary>
    /// After the candidate edits the matrix or hours, drop computed presets and keep custom + manual mode.
    /// </summary>
    public static void MarkManualEdit(HashSet<string> presets, ref bool presetsOverridden, ref bool fineTune)
    {
        presetsOverridden = true;
        if (presets.Contains(AvailabilityPresetRules.Custom))
        {
            fineTune = true;
            return;
        }

        if (presets.Count == 0)
        {
            presets.Clear();
            presets.Add(AvailabilityPresetRules.Custom);
            fineTune = true;
            return;
        }

        presets.Clear();
        presets.Add(AvailabilityPresetRules.Custom);
        fineTune = true;
    }

    public static void ResetToPresets(
        HashSet<string> presets,
        HashSet<string> availability,
        ref bool presetsOverridden,
        ref bool fineTune,
        ref decimal? minHours,
        ref decimal? maxHours)
    {
        presetsOverridden = false;
        if (presets.Contains(AvailabilityPresetRules.Custom))
        {
            fineTune = true;
            return;
        }

        fineTune = false;
        ApplyComputed(presets, availability, ref minHours, ref maxHours);
    }

    private static void ToggleCustom(
        HashSet<string> presets,
        HashSet<string> availability,
        ref bool presetsOverridden,
        ref bool fineTune)
    {
        if (presets.Remove(AvailabilityPresetRules.Custom))
        {
            if (!presetsOverridden)
            {
                fineTune = false;
            }

            return;
        }

        presets.Clear();
        presets.Add(AvailabilityPresetRules.Custom);
        presetsOverridden = true;
        fineTune = true;
        availability.Clear();
    }
}
