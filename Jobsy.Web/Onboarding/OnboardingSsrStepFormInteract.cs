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

    public static bool HasInteract(int step, IFormCollection form)
    {
        if (step == 2)
        {
            return Has(form, "obPreset")
                   || Has(form, "obMatrix")
                   || Has(form, "obStart")
                   || Has(form, "obAvailableFromSave");
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

        if (Has(form, "obStart"))
        {
            var start = form["obStart"].ToString();
            if (string.Equals(start, "immediate", StringComparison.OrdinalIgnoreCase))
            {
                draft.Immediate = true;
                changed = true;
            }
            else if (string.Equals(start, "from", StringComparison.OrdinalIgnoreCase))
            {
                draft.Immediate = false;
                changed = true;
            }
        }

        if (Has(form, "obAvailableFromSave") || (Has(form, "obAvailableFrom") && !draft.Immediate))
        {
            var raw = form["obAvailableFrom"].ToString();
            if (JobsyDateInputRules.TryParseFlexibleDate(raw, out var fromDate))
            {
                draft.Immediate = false;
                draft.AvailableFrom = fromDate;
                changed = true;
            }
            else if (!string.IsNullOrWhiteSpace(raw))
            {
                errorMessage = culture["Onboarding.NotSaved"];
                return true;
            }
        }

        return changed;
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
