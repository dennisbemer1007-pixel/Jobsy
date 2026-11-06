using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Onboarding;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public sealed class OnboardingSsrStepFormInteractTests
{
    private static CultureState NlCulture()
    {
        var culture = new CultureState(null!, null!, null!);
        culture.InitializeFromLanguage("nl");
        return culture;
    }
    [Fact]
    public void Step2_save_button_uses_mockup_inline_styles()
    {
        var html = OnboardingClientStepShells.Build(2, NlCulture(), new OnboardingProfileDraft());
        Assert.Contains("white-space:nowrap", html, StringComparison.Ordinal);
        Assert.Contains("min-width:max-content", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"onboarding-start-save\"", html, StringComparison.Ordinal);
        Assert.Contains("flex-direction:column", html, StringComparison.Ordinal);
    }

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

    [Fact]
    public void Custom_preset_post_keeps_existing_availability()
    {
        var draft = new OnboardingProfileDraft
        {
            PresetsOverridden = true,
            FineTune = true,
        };
        draft.Presets.Add(AvailabilityPresetRules.Custom);
        draft.Availability.Add("Do:Middag");
        draft.Availability.Add("Vr:Middag");

        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["obPreset"] = AvailabilityPresetRules.Custom,
        });

        Assert.True(OnboardingSsrStepFormInteract.TryApply(2, form, draft, null!, out _));
        Assert.DoesNotContain(AvailabilityPresetRules.Custom, draft.Presets);
        Assert.Equal(2, draft.Availability.Count);
    }

    [Fact]
    public void Named_preset_with_overridden_matrix_does_not_clear_cells()
    {
        var draft = new OnboardingProfileDraft
        {
            PresetsOverridden = true,
            FineTune = true,
        };
        draft.Presets.Add(AvailabilityPresetRules.Custom);
        draft.Availability.Add("Za:Ochtend");

        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["obPreset"] = "weekend",
        });

        Assert.True(OnboardingSsrStepFormInteract.TryApply(2, form, draft, null!, out _));
        Assert.Contains("weekend", draft.Presets);
        Assert.Contains("Za:Ochtend", draft.Availability);
        Assert.True(draft.PresetsOverridden);
    }

    [Fact]
    public void Immediate_start_does_not_clear_stored_from_date()
    {
        var from = new DateOnly(2027, 4, 20);
        var draft = new OnboardingProfileDraft
        {
            Immediate = false,
            AvailableFrom = from,
        };

        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["obAction"] = "saveStart",
            ["obStartMode"] = "now",
        });

        Assert.True(OnboardingSsrStepFormInteract.TryApply(2, form, draft, null!, out _));
        Assert.True(draft.Immediate);
        Assert.Equal(from, draft.AvailableFrom);
    }

    [Fact]
    public void SaveStart_from_mode_persists_iso_date()
    {
        var draft = new OnboardingProfileDraft { Immediate = true };
        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["obAction"] = "saveStart",
            ["obStartMode"] = "from",
            ["obAvailableFrom"] = "2027-04-20",
        });
        Assert.True(OnboardingSsrStepFormInteract.TryApply(2, form, draft, NlCulture(), out var err));
        Assert.Null(err);
        Assert.False(draft.Immediate);
        Assert.Equal(new DateOnly(2027, 4, 20), draft.AvailableFrom);
    }

    [Fact]
    public void SaveStart_from_mode_rejects_invalid_date()
    {
        var draft = new OnboardingProfileDraft();
        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["obAction"] = "saveStart",
            ["obStartMode"] = "from",
            ["obAvailableFrom"] = "not-a-date",
        });
        Assert.True(OnboardingSsrStepFormInteract.TryApply(2, form, draft, NlCulture(), out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Scroll_anchor_for_matrix_and_start()
    {
        var matrixForm = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["obMatrix"] = "Do:Middag",
        });
        Assert.Equal("#ob-matrix", OnboardingSsrStepFormInteract.ScrollAnchorFor(2, matrixForm));

        var startForm = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["obAction"] = "saveStart",
            ["obStartMode"] = "now",
        });
        Assert.Equal("#ob-start", OnboardingSsrStepFormInteract.ScrollAnchorFor(2, startForm));
    }
}
