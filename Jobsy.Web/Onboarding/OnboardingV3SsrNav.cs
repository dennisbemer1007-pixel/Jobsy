using Jobsy.Web.Navigation;

namespace Jobsy.Web.Onboarding;

/// <summary>Plain GET targets for onboarding v3 SSR footer back (no POST / BFF).</summary>
internal static class OnboardingV3SsrNav
{
    public static string? BackHref(int step, bool step7ShowsConsentOnly, int miniQuestionQ1Based)
    {
        if (step <= 1)
        {
            return null;
        }

        if (step is >= 7 and <= 10)
        {
            if (step == 7 && step7ShowsConsentOnly)
            {
                return OnboardingRoutes.StepUrl(6);
            }

            if (miniQuestionQ1Based > 1)
            {
                return OnboardingSsrMiniNavigation.StepQuestionUrl(step, miniQuestionQ1Based - 1);
            }
        }

        return OnboardingRoutes.StepUrl(step - 1);
    }
}