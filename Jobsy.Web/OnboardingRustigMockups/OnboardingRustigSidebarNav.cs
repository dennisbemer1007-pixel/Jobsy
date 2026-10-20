namespace Jobsy.Web.OnboardingRustigMockups;

public static class OnboardingRustigSidebarNav
{
    public static int ActiveSection(int wizardStep, OnboardingRustigMockupKind kind) =>
        kind switch
        {
            OnboardingRustigMockupKind.Start => 1,
            OnboardingRustigMockupKind.Kompas => 5,
            _ => wizardStep switch
            {
                <= 3 => 1,
                <= 5 => 2,
                6 => 3,
                <= 10 => 4,
                _ => 5,
            },
        };
}
