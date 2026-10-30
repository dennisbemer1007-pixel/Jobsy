using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingClientProgressSaver
{
    public static async Task<OnboardingState> SaveStickyAsync(
        JobsyApiClient api,
        int currentStep,
        bool? stepCompleted = null,
        bool? stepSkipped = null,
        string? source = null,
        bool? finishReached = null,
        CancellationToken ct = default)
    {
        var preserveFinish = finishReached is null && await ShouldPreserveFinishAsync(api, ct);
        var finishFlag = finishReached == true || preserveFinish ? true : finishReached;
        return await api.SaveMyOnboardingProgressAsync(
            currentStep,
            stepCompleted,
            stepSkipped,
            source,
            finishFlag,
            ct);
    }

    internal static bool IsFinished(OnboardingState? state)
    {
        if (state is null)
        {
            return false;
        }

        if (state.IsComplete || state.FinishReached || state.CompletedAtUtc is not null)
        {
            return true;
        }

        var steps = state.Steps
            .Select(s => new OnboardingStepAnalytics.StepEvent(
                s.Step,
                s.StartedAtUtc,
                s.CompletedAtUtc,
                s.SkippedAtUtc))
            .ToList();
        return OnboardingWizardCompletion.IsWizardFinished(
            state.FinishReached,
            state.CompletedAtUtc,
            steps);
    }

    private static async Task<bool> ShouldPreserveFinishAsync(JobsyApiClient api, CancellationToken ct)
    {
        try
        {
            return IsFinished(await api.GetMyOnboardingAsync(ct));
        }
        catch
        {
            return false;
        }
    }
}
