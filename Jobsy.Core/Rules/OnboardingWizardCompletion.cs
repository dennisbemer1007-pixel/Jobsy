using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>Sticky onboarding completion: finish is inferred from persisted signals and never downgraded on step revisits.</summary>
public static class OnboardingWizardCompletion
{
    public static bool IsWizardFinished(
        bool finishReached,
        DateTime? completedAtUtc,
        IReadOnlyList<OnboardingStepAnalytics.StepEvent> steps)
    {
        if (finishReached || completedAtUtc is not null)
        {
            return true;
        }

        if (steps.Any(s => s.Step == OnboardingWizardCatalog.V3StepCount && s.CompletedAtUtc is not null))
        {
            return true;
        }

        return AllStepsDone(steps);
    }

    public static bool AllStepsDone(IReadOnlyList<OnboardingStepAnalytics.StepEvent> steps)
    {
        if (steps.Count == 0)
        {
            return false;
        }

        var done = new HashSet<int>();
        foreach (var s in steps)
        {
            if (s.CompletedAtUtc is not null || s.SkippedAtUtc is not null)
            {
                done.Add(s.Step);
            }
        }

        return Enumerable.Range(1, OnboardingWizardCatalog.V3StepCount).All(done.Contains);
    }

    public static void ApplyStickyFinish(CandidateOnboarding row)
    {
        var steps = OnboardingStepAnalytics.Parse(row.StepsJson);
        if (IsWizardFinished(row.FinishReached, row.CompletedAtUtc, steps))
        {
            row.FinishReached = true;
        }
    }
}
