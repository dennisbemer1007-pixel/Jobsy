using Jobsy.Core.Features;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Flag-aware entry path for candidate first-login / resume onboarding.
/// Always the step-by-step wizard; discovery follows after the wizard when paspoort is ON.
/// </summary>
public static class OnboardingRoutes
{
    public const string ClassicStartPath = "/candidate/start";
    public const string DiscoveryPath = "/candidate/ontdekkingsreis";

    public const string WizardStartPath = ClassicStartPath + "/wizard";

    public const string KompasReadyPath = ClassicStartPath + "/kompas";

    public const string KompasHubPath = "/candidate/kompas";

    /// <summary>Steps 1–10: static SSR on classic start. Step 11+: classic entry.</summary>
    public static string StepUrl(int step) =>
        step switch
        {
            >= 1 and <= 10 => $"{ClassicStartPath}?step={step}",
            _ => ClassicStartPath,
        };

    public static string StartPath(FeatureFlagSnapshot flags)
    {
        _ = flags;
        return ClassicStartPath;
    }

    public static string StartPath(bool candidatePassportEnabled)
    {
        _ = candidatePassportEnabled;
        return ClassicStartPath;
    }
}
