namespace Jobsy.Core.Rules;

/// <summary>How the discovery overview labels steps that have no per-step timestamp.</summary>
public static class DiscoveryOverviewRules
{
    /// <summary>
    /// A finished v2 journey never collected steps 3–6, so those stay "new".
    /// A finished v3 journey is done even when a step event was not stored.
    /// </summary>
    public static bool StepLooksNew(bool journeyComplete, int wizardVersion, int step, bool stepFilled)
        => journeyComplete
           && step is >= 3 and <= 6
           && wizardVersion < OnboardingWizardCatalog.WizardVersionV3
           && !stepFilled;
}
