using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Onboarding;

public static class OnboardingRustigSidebarNav
{
    public sealed record NavItem(string Number, string Title, string? Sub, bool Current);

    public static int ActiveSection(int wizardStep) =>
        wizardStep switch
        {
            <= 3 => 1,
            <= 5 => 2,
            6 => 3,
            <= 10 => 4,
            _ => 5,
        };

    /// <summary>First wizard step for a sidebar section (1–5).</summary>
    public static int LinkWizardStep(int sectionNumber) =>
        sectionNumber switch
        {
            1 => 1,
            2 => 4,
            3 => 6,
            4 => 7,
            5 => 10,
            _ => 1,
        };

    public static string LinkHref(int sectionNumber) =>
        OnboardingRoutes.StepUrl(LinkWizardStep(sectionNumber));

    public static IReadOnlyList<NavItem> Items(CultureState culture, int wizardStep, int? activeSectionOverride = null)
    {
        var active = activeSectionOverride ?? ActiveSection(wizardStep);
        return
        [
            new("1", culture["Onboarding.Phase.OverJou"], culture["Onboarding.StartJourney.Sidebar1"], active == 1),
            new("2", culture["Onboarding.Phase.Achtergrond"], culture["Onboarding.StartJourney.Sidebar2"], active == 2),
            new("3", culture["Onboarding.Rustig.Phase.Droom"], culture["Onboarding.StartJourney.Sidebar3"], active == 3),
            new("4", culture["Onboarding.Rustig.Phase.WieBenJij"], culture["Onboarding.StartJourney.Sidebar4"], active == 4),
            new("5", culture["Onboarding.Rustig.Phase.Kompas"], culture["Onboarding.StartJourney.Sidebar5"], active == 5),
        ];
    }
}
