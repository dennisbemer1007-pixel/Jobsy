using System.Security.Claims;
using Bunit;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AvailabilityStepFineTuneBunitTests : BunitContext
{
    private static readonly AvailabilityStep.Preset[] AllPresets =
    [
        new("school", "Onboarding.Preset.School", "Onboarding.Preset.School.Lead"),
        new("weekend", "Onboarding.Preset.Weekend", "Onboarding.Preset.Weekend.Lead"),
        new("evening", "Onboarding.Preset.Evening", "Onboarding.Preset.Evening.Lead"),
        new("office", "Onboarding.Preset.Office", "Onboarding.Preset.Office.Lead"),
        new("holiday", "Onboarding.Preset.Holiday", "Onboarding.Preset.Holiday.Lead"),
        new("parttime", "Onboarding.Preset.Parttime", "Onboarding.Preset.Parttime.Lead"),
        new("fulltime", "Onboarding.Preset.Fulltime", "Onboarding.Preset.Fulltime.Lead"),
        new("custom", "Onboarding.Preset.Custom", "Onboarding.Preset.Custom.Lead")
    ];

    public AvailabilityStepFineTuneBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Theory]
    [InlineData("school")]
    [InlineData("weekend")]
    [InlineData("evening")]
    [InlineData("office")]
    [InlineData("holiday")]
    [InlineData("parttime")]
    [InlineData("fulltime")]
    public void Fine_tune_opens_matrix_prefilled_for_preset(string presetCode)
    {
        var draft = NewDraft();
        ApplyPreset(draft, presetCode);
        var expected = AvailabilityPresetRules.Compute(draft.Presets).DayParts;

        var cut = RenderStep(draft);
        Assert.DoesNotContain("role=\"grid\"", cut.Markup, StringComparison.Ordinal);

        cut.Find(".ob-wizard__summary-action").Click();

        cut.Find("[role=grid]");
        var pressed = cut.FindAll("[role=gridcell][aria-pressed=true]");
        Assert.Equal(expected.Count, pressed.Count);
        foreach (var key in expected)
        {
            Assert.Contains(key, draft.Availability, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Office_preset_prefills_weekday_morning_and_afternoon()
    {
        var draft = NewDraft();
        ApplyPreset(draft, AvailabilityPresetRules.Office);

        var cut = RenderStep(draft);
        cut.Find(".ob-wizard__summary-action").Click();

        Assert.Contains("Ma:Ochtend", draft.Availability);
        Assert.Contains("Vr:Middag", draft.Availability);
        Assert.DoesNotContain("Za:Ochtend", draft.Availability);
    }

    [Fact]
    public void Editing_matrix_switches_to_custom_preset()
    {
        var draft = NewDraft();
        ApplyPreset(draft, AvailabilityPresetRules.Office);
        var cut = RenderStep(draft);
        cut.Find(".ob-wizard__summary-action").Click();

        cut.FindAll("[role=gridcell][aria-pressed=false]")[0].Click();

        Assert.Contains(AvailabilityPresetRules.Custom, draft.Presets);
        Assert.DoesNotContain(AvailabilityPresetRules.Office, draft.Presets);
        Assert.True(draft.PresetsOverridden);
    }

    private static OnboardingProfileDraft NewDraft()
    {
        return new OnboardingProfileDraft
        {
            Immediate = true,
            MinHours = 8,
            MaxHours = 40
        };
    }

    private static void ApplyPreset(OnboardingProfileDraft draft, string code)
    {
        var overridden = draft.PresetsOverridden;
        var fineTune = draft.FineTune;
        decimal? min = draft.MinHours;
        decimal? max = draft.MaxHours;
        AvailabilityOnboardingPresets.Toggle(code, draft.Presets, draft.Availability, ref overridden, ref fineTune, ref min, ref max);
        draft.PresetsOverridden = overridden;
        draft.FineTune = fineTune;
        draft.MinHours = min;
        draft.MaxHours = max;
    }

    private IRenderedComponent<AvailabilityStep> RenderStep(OnboardingProfileDraft draft)
    {
        return Render<AvailabilityStep>(p => p
            .Add(c => c.Draft, draft)
            .Add(c => c.Presets, AllPresets)
            .Add(c => c.TogglePreset, (string code) => ApplyPreset(draft, code))
            .Add(c => c.ToggleFineTune, () => { })
            .Add(c => c.SetMinHours, (int v) => draft.MinHours = v)
            .Add(c => c.SetMaxHours, (int v) => draft.MaxHours = v)
            .Add(c => c.ManualAvailability, () =>
            {
                var overridden = draft.PresetsOverridden;
                var fineTune = draft.FineTune;
                AvailabilityOnboardingPresets.MarkManualEdit(draft.Presets, ref overridden, ref fineTune);
                draft.PresetsOverridden = overridden;
                draft.FineTune = fineTune;
            })
            .Add(c => c.ToggleAvailability, (string key) =>
            {
                if (!draft.Availability.Add(key))
                {
                    draft.Availability.Remove(key);
                }

                var overridden = draft.PresetsOverridden;
                var fineTune = draft.FineTune;
                AvailabilityOnboardingPresets.MarkManualEdit(draft.Presets, ref overridden, ref fineTune);
                draft.PresetsOverridden = overridden;
                draft.FineTune = fineTune;
            })
            .Add(c => c.ResetPresets, () =>
            {
                var overridden = draft.PresetsOverridden;
                var fineTune = draft.FineTune;
                decimal? min = draft.MinHours;
                decimal? max = draft.MaxHours;
                AvailabilityOnboardingPresets.ResetToPresets(draft.Presets, draft.Availability, ref overridden, ref fineTune, ref min, ref max);
                draft.PresetsOverridden = overridden;
                draft.FineTune = fineTune;
                draft.MinHours = min;
                draft.MaxHours = max;
            }));
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
