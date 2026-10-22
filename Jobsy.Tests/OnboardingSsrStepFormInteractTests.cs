using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Onboarding;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public sealed class OnboardingSsrStepFormInteractTests
{
    [Fact]
    public void Preset_post_toggle_fills_availability_for_validation()
    {
        var draft = new OnboardingProfileDraft();
        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["obPreset"] = "weekend",
        });

        Assert.True(OnboardingSsrStepFormInteract.TryApply(2, form, draft, null!, out _));
        Assert.Contains("weekend", draft.Presets);
        Assert.True(draft.Availability.Count > 0);

        var issues = OnboardingWizardValidation.ValidateAvailability(
            draft.MinHours,
            draft.MaxHours,
            draft.Availability.Count,
            draft.Immediate,
            draft.AvailableFrom,
            requirePresetSelection: draft.Presets.Count == 0 && draft.Availability.Count == 0);
        Assert.Empty(issues);
    }
}
