using Jobsy.Core.Entities;
using Jobsy.Core.Localization;

namespace Jobsy.Core.Rules;

/// <summary>
/// Reiskaart zone index (0–4) from onboarding progress — mirrors kreeft screens 1–16.
/// </summary>
public static class KreeftJourneyZone
{
    public static int ScreenForOnboardingStep(int step) =>
        step switch
        {
            >= 1 and <= 8 => step + 3,
            9 => 13,
            10 => 14,
            _ => 16
        };

    public static int ZoneIndexForScreen(int screenNumber) =>
        screenNumber switch
        {
            1 or 2 or 3 => 0,
            4 or 5 or 6 => 1,
            7 or 8 or 9 => 2,
            10 or 11 or 12 or 13 or 14 => 3,
            15 or 16 => 4,
            _ => 4
        };

    public static int ZoneIndexForOnboardingStep(int step) =>
        ZoneIndexForScreen(ScreenForOnboardingStep(step));

    public static int ResolveZoneIndex(CandidateOnboarding? row)
    {
        if (row is null)
        {
            return 4;
        }

        var steps = OnboardingStepAnalytics.Parse(row.StepsJson);
        if (OnboardingWizardCompletion.IsWizardFinished(row.FinishReached, row.CompletedAtUtc, steps))
        {
            return 4;
        }

        var step = Math.Clamp(row.CurrentStep, 1, 10);
        return ZoneIndexForOnboardingStep(step);
    }

    public static string ZoneLabel(int zoneIndex, string language)
    {
        var lang = JobsyLanguages.Normalize(language);
        return lang switch
        {
            "en" => zoneIndex switch
            {
                0 => "Beach",
                1 => "Coast",
                2 => "Rocks",
                3 => "Depth",
                _ => "Light"
            },
            _ => zoneIndex switch
            {
                0 => "Strand",
                1 => "Kust",
                2 => "Rotsen",
                3 => "Diepte",
                _ => "Licht"
            }
        };
    }
}
