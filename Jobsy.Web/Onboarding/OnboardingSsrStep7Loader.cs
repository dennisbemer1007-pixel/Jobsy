using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingSsrStep7Loader
{
    internal sealed record Step7Data(
        IReadOnlyList<OnboardingClientStepSeed.MiniQuestionSeed> Questions,
        IReadOnlyDictionary<int, int> Answers,
        int QuickScanVersion);

    public static async Task<Step7Data> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var testsV2 = await featureFlags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var competency = await api.GetMyCompetenciesAsync(cancellationToken);
        var version = ResolveQuickScanVersion(competency?.QuickScanVersion, testsV2);
        var questionIds = OnboardingMiniBlockCompletion.MiniQuestionIds(7, version);
        var catalogById = AssessmentQuickScanCatalog.CompetenceQuestions(version)
            .ToDictionary(item => item.Id, item => item.TextKey);

        var serverAnswers = new Dictionary<int, int>();
        foreach (var answer in competency?.Answers ?? [])
        {
            if (int.TryParse(answer.Key, out var id)
                && questionIds.Contains(id)
                && answer.Value is >= 1 and <= 5)
            {
                serverAnswers[id] = answer.Value;
            }
        }

        var questions = questionIds
            .Where(catalogById.ContainsKey)
            .Select(id => new OnboardingClientStepSeed.MiniQuestionSeed(id, culture[catalogById[id]]))
            .ToList();

        return new Step7Data(questions, serverAnswers, version);
    }

    private static int ResolveQuickScanVersion(int? version, bool testsV2)
        => version is > 0 ? version.Value : testsV2 ? AssessmentTestVersions.V2 : AssessmentTestVersions.V1;
}
