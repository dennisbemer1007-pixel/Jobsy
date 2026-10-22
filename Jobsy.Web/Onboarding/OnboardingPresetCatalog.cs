namespace Jobsy.Web.Onboarding;

internal static class OnboardingPresetCatalog
{
    internal sealed record Preset(string Code, string TitleKey, string LeadKey);

    internal static readonly Preset[] Items =
    [
        new("school", "Onboarding.Preset.School", "Onboarding.Preset.School.Lead"),
        new("weekend", "Onboarding.Preset.Weekend", "Onboarding.Preset.Weekend.Lead"),
        new("evening", "Onboarding.Preset.Evening", "Onboarding.Preset.Evening.Lead"),
        new("office", "Onboarding.Preset.Office", "Onboarding.Preset.Office.Lead"),
        new("holiday", "Onboarding.Preset.Holiday", "Onboarding.Preset.Holiday.Lead"),
        new("parttime", "Onboarding.Preset.Parttime", "Onboarding.Preset.Parttime.Lead"),
        new("fulltime", "Onboarding.Preset.Fulltime", "Onboarding.Preset.Fulltime.Lead"),
        new("custom", "Onboarding.Preset.Custom", "Onboarding.Preset.Custom.Lead"),
    ];
}
