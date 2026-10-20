using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Services;

namespace Jobsy.Web.Components.Candidate.Onboarding;

internal static class OnboardingTestCompletion
{
    public static async Task<IReadOnlyList<int>> LoadCompletedWizardStepsAsync(
        JobsyApiClient api,
        bool testsV2Enabled,
        CancellationToken cancellationToken = default)
    {
        var competency = await api.GetMyCompetenciesAsync(cancellationToken);
        var career = await api.GetMyCareerInterestsAsync(cancellationToken);
        var culture = await api.GetMyCultureAsync(cancellationToken);
        var values = await api.GetMyValuesAsync(cancellationToken);

        var completed = new List<int>(4);
        var competenceVersion = ResolveQuickScanVersion(competency?.QuickScanVersion, testsV2Enabled);
        if (OnboardingMiniBlockCompletion.IsMiniBlockComplete(AssessmentKind.Competence, competenceVersion, competency?.Answers))
        {
            completed.Add(7);
        }

        var careerVersion = ResolveQuickScanVersion(career?.QuickScanVersion, testsV2Enabled);
        if (OnboardingMiniBlockCompletion.IsMiniBlockComplete(AssessmentKind.Career, careerVersion, career?.Answers))
        {
            completed.Add(8);
        }

        var cultureVersion = ResolveQuickScanVersion(culture?.QuickScanVersion, testsV2Enabled);
        if (OnboardingMiniBlockCompletion.IsMiniBlockComplete(AssessmentKind.Culture, cultureVersion, culture?.Answers))
        {
            completed.Add(9);
        }

        var valuesVersion = ResolveQuickScanVersion(values?.QuickScanVersion, testsV2Enabled);
        if (OnboardingMiniBlockCompletion.IsMiniBlockComplete(AssessmentKind.Values, valuesVersion, values?.Answers))
        {
            completed.Add(10);
        }

        return completed;
    }

    public static int FirstIncompleteStep(IReadOnlyList<int> completedSteps)
    {
        foreach (var step in new[] { 7, 8, 9, 10 })
        {
            if (!completedSteps.Contains(step))
            {
                return step;
            }
        }

        return 10;
    }

    public static int TestNumberForStep(int wizardStep) => wizardStep switch
    {
        7 => 1,
        8 => 2,
        9 => 3,
        10 => 4,
        _ => 1
    };

    private static int ResolveQuickScanVersion(int? stored, bool testsV2Enabled)
    {
        if (stored is int value && value is AssessmentTestVersions.V1 or AssessmentTestVersions.V2 or AssessmentTestVersions.V3)
        {
            return value;
        }

        return testsV2Enabled ? AssessmentTestVersions.V3 : AssessmentTestVersions.V1;
    }
}
