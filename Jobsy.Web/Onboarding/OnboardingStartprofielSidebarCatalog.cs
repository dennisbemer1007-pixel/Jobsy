using Jobsy.Core.Rules;

namespace Jobsy.Web.Onboarding;

public static class OnboardingStartprofielSidebarCatalog
{
    public sealed record PhaseNavItem(
        int Index,
        string LabelKey,
        string HintKey,
        int FirstStep,
        int LastStep);

    public static readonly PhaseNavItem[] Phases =
    [
        new(1, "Onboarding.Phase.OverJou", "Onboarding.Side.OverJou", 1, 3),
        new(2, "Onboarding.Welcome.Background", "Onboarding.Side.Achtergrond", 4, 5),
        new(3, "Onboarding.Welcome.Dream", "Onboarding.Side.Droom", 6, 6),
        new(4, "Onboarding.Phase.WieBenJij", "Onboarding.Side.WieBenJij", 7, 10),
        new(5, "Onboarding.Side.Kompas", "Onboarding.Side.KompasLead", 11, 11),
    ];

    public static PhaseNavItem? PhaseForStep(int step)
    {
        foreach (var phase in Phases)
        {
            if (step >= phase.FirstStep && step <= phase.LastStep)
            {
                return phase;
            }
        }

        return step <= 10 ? Phases[0] : null;
    }
}
