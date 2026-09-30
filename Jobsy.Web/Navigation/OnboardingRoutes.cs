using Jobsy.Core.Features;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Flag-aware entry path for candidate first-login / resume onboarding.
/// Passport ON → De ontdekkingsreis; OFF → classic wizard.
/// </summary>
public static class OnboardingRoutes
{
    public const string ClassicStartPath = "/candidate/start";
    public const string DiscoveryPath = "/candidate/ontdekkingsreis";

    public static string StartPath(FeatureFlagSnapshot flags)
        => flags.CandidatePassportEnabled ? DiscoveryPath : ClassicStartPath;

    public static string StartPath(bool candidatePassportEnabled)
        => candidatePassportEnabled ? DiscoveryPath : ClassicStartPath;
}
