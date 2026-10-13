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
