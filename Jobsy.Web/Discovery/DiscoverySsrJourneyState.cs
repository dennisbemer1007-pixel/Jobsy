using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;

namespace Jobsy.Web.Discovery;

internal static class DiscoverySsrJourneyState
{
    public static bool IsJourneyComplete(OnboardingState? state, OnboardingProfileDraft draft)
    {
        if (state?.IsComplete == true || state?.FinishReached == true || state?.CompletedAtUtc is not null)
        {
            return true;
        }

        if (state?.Steps.Any(s => s.Step == 10 && s.CompletedAtUtc is not null) == true)
        {
            return true;
        }

        return draft.HasConsent && (state?.CurrentStep ?? 0) >= 10;
    }

    public static string ResolveScreen(string? stapRaw, OnboardingState? state)
    {
        if (!string.IsNullOrWhiteSpace(stapRaw))
        {
            return stapRaw.Trim().ToLowerInvariant();
        }

        var step = Math.Clamp(state?.CurrentStep ?? 1, 1, 10);
        var showStart = step == 1
            && (state?.Steps.Count == 0
                || state!.Steps.All(item => item.Step == 1 && item.CompletedAtUtc is null));
        return showStart ? "start" : "overview";
    }

    public static HashSet<int> CompletedStepNumbers(OnboardingState? state)
    {
        var set = new HashSet<int>();
        if (state?.Steps is null)
        {
            return set;
        }

        foreach (var ev in state.Steps)
        {
            if (ev.CompletedAtUtc is not null || ev.SkippedAtUtc is not null)
            {
                set.Add(ev.Step);
            }
        }

        return set;
    }

    public static int RemainingStepsInPhase(
        string phaseId,
        OnboardingProfileDraft draft,
        OnboardingState? state,
        CandidateDiscoveryOverviewPanelDto? panel)
    {
        if (phaseId == "beach")
        {
            return draft.HasConsent ? 0 : 1;
        }

        if (phaseId == "light")
        {
            return 0;
        }

        if (phaseId == "diep" && panel is not null)
        {
            var mini = OnboardingWizardCatalog.MiniLevelCount;
            var remaining = 0;
            if (panel.CompetencyAnswered < mini)
            {
                remaining++;
            }

            if (panel.CareerAnswered < mini)
            {
                remaining++;
            }

            if (panel.CultureAnswered < mini)
            {
                remaining++;
            }

            if (panel.ValuesAnswered < mini)
            {
                remaining++;
            }

            return remaining;
        }

        var completed = CompletedStepNumbers(state);
        var steps = phaseId switch
        {
            "kust" => new[] { 1, 2 },
            "rots" => new[] { 3, 4, 5, 6 },
            "diep" => new[] { 7, 8, 9, 10 },
            _ => Array.Empty<int>()
        };
        return steps.Count(s => !completed.Contains(s));
    }

    public static string ResolveOpenPhaseId(
        OnboardingProfileDraft draft,
        OnboardingState? state,
        CandidateDiscoveryOverviewPanelDto? panel)
    {
        if (!draft.HasConsent)
        {
            return "beach";
        }

        if (RemainingStepsInPhase("kust", draft, state, panel) > 0)
        {
            return "kust";
        }

        if (RemainingStepsInPhase("rots", draft, state, panel) > 0)
        {
            return "rots";
        }

        if (RemainingStepsInPhase("diep", draft, state, panel) > 0)
        {
            return "diep";
        }

        return "light";
    }

    public static int AnsweredCount(
        OnboardingWizardCatalog.OnboardingTestKind kind,
        CandidateDiscoveryOverviewPanelDto? panel)
        => panel is null
            ? 0
            : kind switch
            {
                OnboardingWizardCatalog.OnboardingTestKind.Competency => panel.CompetencyAnswered,
                OnboardingWizardCatalog.OnboardingTestKind.Career => panel.CareerAnswered,
                OnboardingWizardCatalog.OnboardingTestKind.Culture => panel.CultureAnswered,
                _ => panel.ValuesAnswered
            };

    public static bool IsTestMiniDone(
        int step,
        CandidateDiscoveryOverviewPanelDto? panel)
    {
        var kind = OnboardingWizardCatalog.TestKindForStep(step);
        if (kind is null || panel is null)
        {
            return false;
        }

        return AnsweredCount(kind.Value, panel) >= OnboardingWizardCatalog.MiniLevelCount;
    }
}
