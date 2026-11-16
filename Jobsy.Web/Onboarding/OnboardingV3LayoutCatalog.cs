using Jobsy.Core.Rules;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingV3LayoutCatalog
{
    public static string? TipKeyForStep(int step) => step switch
    {
        >= 1 and <= 6 => $"Onboarding.IllustratedSidebar.Step{step}",
        7 => "Onboarding.V3.Tip.Step7",
        8 => "Onboarding.V3.Tip.Step8",
        9 => "Onboarding.V3.Tip.Step9",
        10 => "Onboarding.V3.Tip.Step10",
        _ => null,
    };

    public enum ProgressChipKind
    {
        Later,
        Todo,
        Optional,
        Fraction,
        Done,
    }

    public static (ProgressChipKind Kind, int A, int B) ProgressChip(
        OnboardingStartprofielSidebarCatalog.PhaseNavItem phase,
        int currentStep)
    {
        if (currentStep > phase.LastStep)
        {
            return (ProgressChipKind.Done, 0, 0);
        }

        if (currentStep < phase.FirstStep)
        {
            return (ProgressChipKind.Later, 0, 0);
        }

        if (phase.Index is 2)
        {
            return (ProgressChipKind.Optional, 0, 0);
        }

        if (phase.Index is 3)
        {
            return (ProgressChipKind.Fraction, 1, 1);
        }

        if (phase.Index is 4)
        {
            var testIndex = currentStep is >= 7 and <= 10 ? currentStep - 6 : 1;
            return (ProgressChipKind.Fraction, testIndex, 4);
        }

        if (phase.Index is 5)
        {
            return currentStep >= 11
                ? (ProgressChipKind.Done, 0, 0)
                : (ProgressChipKind.Todo, 0, 0);
        }

        var stepsInPhase = phase.LastStep - phase.FirstStep + 1;
        var index = currentStep - phase.FirstStep + 1;
        return (ProgressChipKind.Fraction, index, stepsInPhase);
    }

    public static string PhaseTagKey(int step)
    {
        if (step is >= 7 and <= 10)
        {
            return "Onboarding.Phase.WieBenJij";
        }

        if (step >= 11)
        {
            return "Onboarding.Side.Kompas";
        }

        return OnboardingWizardCatalog.PhaseForStep(step).NameKey;
    }
}
