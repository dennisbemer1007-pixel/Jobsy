namespace Jobsy.Web.Onboarding;

public static class OnboardingRustigSidebarNav
{
    public static int ActiveSection(int wizardStep) =>
        wizardStep switch
        {
            <= 3 => 1,
            <= 5 => 2,
            6 => 3,
            <= 10 => 4,
            _ => 5,
        };
}
