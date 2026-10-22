using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

/// <summary>Apply step 2–3 tile / matrix / start / travel POST clicks onto the profile draft.</summary>
internal static class OnboardingSsrStepFormInteract
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static bool TryApply(
        int step,
        IFormCollection form,
        OnboardingProfileDraft draft,
        CultureState culture,
        out string? errorMessage)
    {
        errorMessage = null;
        if (step == 2)
        {
            return TryApplyStep2(form, draft, culture, out errorMessage);
        }

        if (step == 3)
        {
            return TryApplyStep3(form, draft, out errorMessage);
        }

        return false;
    }

    public static string? ScrollAnchorFor(int step, IFormCollection form)
    {
        if (step != 2)
        {
            return null;
        }

        if (IsSaveStart(form))
        {
            return "#ob-start";
        }

        if (Has(form, "obMatrix"))
        {
            return "#ob-matrix";
        }

        if (Has(form, "obPreset"))
        {
            return "#ob-presets";
        }

        return null;
    }

    public static bool HasInteract(int step, IFormCollection form)
    {
        if (step == 2)
        {
            return Has(form, "obPreset")
                   || Has(form, "obMatrix")
                   || IsSaveStart(form);
        }

        if (step == 3)
        {
            return Has(form, "obTransport") || Has(form, "obTravel");
        }

        return false;
    }

    public static JsonElement BuildPersistBody(int step, OnboardingProfileDraft draft)
    {
        object payload = step switch
        {
            2 => new OnboardingClientStepBridge.Step2Payload
            {
                Presets = draft.Presets.ToList(),
                PresetsOverridden = draft.PresetsOverridden,
                FineTune = draft.FineTune,
                MinHours = draft.MinHours,
                MaxHours = draft.MaxHours,
                Availability = draft.Availability.ToList(),
                Immediate = draft.Immediate,
                AvailableFrom = draft.AvailableFrom?.ToString("yyyy-MM-dd"),
            },
            3 => new OnboardingClientStepBridge.Step3Payload
            {
                Transport = TransportLabels.SplitMany(draft.Transport).ToList(),
                MaxTravel = draft.MaxTravel > 0 ? draft.MaxTravel : 30,
                SelectedTravel = draft.CustomTravel ? null : draft.MaxTravel,
                CustomTravel = draft.CustomTravel,
            },
            4 => new OnboardingClientStepBridge.Step4Payload
            {
                NoWork = draft.NoWork,
                Jobs = draft.Jobs.Select(j => new OnboardingClientStepBridge.Step4Payload.JobPayload
                {
                    Employer = j.EmployerName ?? "",
                    StartMonth = j.StartMonth,
                    EndMonth = j.EndMonth,
                    IsCurrent = j.IsCurrent == true,
                }).ToList(),
            },
            _ => new { },
        };

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload, Json));
        return doc.RootElement.Clone();
    }

    private static bool TryApplyStep2(
        IFormCollection form,
        OnboardingProfileDraft draft,
        CultureState culture,
        out string? errorMessage)
    {
        errorMessage = null;
        var changed = false;

        if (Has(form, "obPreset"))
        {
            var code = form["obPreset"].ToString();
            if (!AvailabilityPresetRules.IsKnown(code))
            {
                errorMessage = culture["Onboarding.NotSaved"];
                return true;
            }

            var min = draft.MinHours;
            var max = draft.MaxHours;
            var overridden = draft.PresetsOverridden;
            var fineTune = draft.FineTune;
            AvailabilityOnboardingPresets.Toggle(
                code,
                draft.Presets,
                draft.Availability,
                ref overridden,
                ref fineTune,
                ref min,
                ref max);
            draft.MinHours = min;
            draft.MaxHours = max;
            draft.PresetsOverridden = overridden;
            draft.FineTune = fineTune;
            changed = true;
        }

        if (Has(form, "obMatrix"))
        {
            var key = form["obMatrix"].ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                errorMessage = culture["Onboarding.NotSaved"];
                return true;
            }

            if (!draft.Availability.Remove(key))
            {
                draft.Availability.Add(key);
            }

            var overridden = draft.PresetsOverridden;
            var fineTune = draft.FineTune;
            AvailabilityOnboardingPresets.MarkManualEdit(draft.Presets, ref overridden, ref fineTune);
            draft.PresetsOverridden = overridden;
            draft.FineTune = fineTune;
            changed = true;
        }

        if (IsSaveStart(form))
        {
            if (!TryApplyStartBlock(form, draft, culture, out errorMessage))
            {
                return false;
            }

            return true;
        }

        return changed;
    }

    internal static bool TryApplyStartBlock(
        IFormCollection form,
        OnboardingProfileDraft draft,
        CultureState culture,
        out string? errorMessage)
    {
        errorMessage = null;
        var mode = form["obStartMode"].ToString();
        if (string.Equals(mode, "now", StringComparison.OrdinalIgnoreCase))
        {
            draft.Immediate = true;
            return true;
        }

        if (string.Equals(mode, "from", StringComparison.OrdinalIgnoreCase))
        {
            var raw = form["obAvailableFrom"].ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                errorMessage = culture["Onboarding.AvailabilityStartDateInvalid"];
                return true;
            }

            if (!TryParseAvailableFrom(raw, out var fromDate))
            {
                errorMessage = culture["Onboarding.AvailabilityStartDateInvalid"];
                return true;
            }

            draft.Immediate = false;
            draft.AvailableFrom = fromDate;
            return true;
        }

        errorMessage = culture["Onboarding.NotSaved"];
        return true;
    }

    internal static void ApplyStartFromPayload(OnboardingProfileDraft draft, bool immediate, string? availableFromIso)
    {
        draft.Immediate = immediate;
        if (immediate)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(availableFromIso)
            && TryParseAvailableFrom(availableFromIso, out var fromDate))
        {
            draft.AvailableFrom = fromDate;
        }
    }

    private static bool IsSaveStart(IFormCollection form)
        => form.TryGetValue("obAction", out var action)
           && string.Equals(action.ToString(), "saveStart", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseAvailableFrom(string raw, out DateOnly date)
    {
        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        return JobsyDateInputRules.TryParseFlexibleDate(raw, out date);
    }

    private static bool TryApplyStep3(IFormCollection form, OnboardingProfileDraft draft, out string? errorMessage)
    {
        errorMessage = null;
        var changed = false;

        if (Has(form, "obTransport"))
        {
            var code = form["obTransport"].ToString();
            draft.Transport = TransportLabels.Toggle(draft.Transport, code);
            changed = true;
        }

        if (Has(form, "obTravel"))
        {
            var raw = form["obTravel"].ToString();
            if (int.TryParse(raw, out var minutes) && minutes is > 0 and <= 180)
            {
                draft.MaxTravel = minutes;
                draft.CustomTravel = false;
                changed = true;
            }
        }

        return changed;
    }

    private static bool Has(IFormCollection form, string key)
        => form.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v);
}
