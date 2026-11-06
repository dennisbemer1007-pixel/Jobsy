using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingSsrMiniStepLoader
{
    internal sealed record MiniStepData(
        IReadOnlyList<OnboardingClientStepSeed.MiniQuestionSeed> Questions,
        IReadOnlyDictionary<int, int> Answers,
        int QuickScanVersion);

    public static async Task<MiniStepData> LoadAsync(
        int step,
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
        => step switch
        {
            7 => await LoadCompetenceAsync(api, featureFlags, culture, cancellationToken),
            8 => await LoadCareerAsync(api, featureFlags, culture, cancellationToken),
            9 => await LoadCultureAsync(api, featureFlags, culture, cancellationToken),
            10 => await LoadValuesAsync(api, featureFlags, culture, cancellationToken),
            _ => new MiniStepData([], new Dictionary<int, int>(), AssessmentTestVersions.V1),
        };

    private static async Task<MiniStepData> LoadCompetenceAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var testsV2 = await featureFlags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var competency = await api.GetMyCompetenciesAsync(cancellationToken);
        var version = OnboardingMiniBlockCompletion.ResolveQuickScanVersion(competency?.QuickScanVersion, testsV2);
        return Build(
            competency?.Answers,
            version,
            7,
            AssessmentQuickScanCatalog.CompetenceQuestions(version).Select(q => (q.Id, q.TextKey)),
            culture);
    }

    private static async Task<MiniStepData> LoadCareerAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var testsV2 = await featureFlags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var career = await api.GetMyCareerInterestsAsync(cancellationToken);
        var version = OnboardingMiniBlockCompletion.ResolveQuickScanVersion(career?.QuickScanVersion, testsV2);
        return Build(
            career?.Answers,
            version,
            8,
            AssessmentQuickScanCatalog.CareerQuestions(version).Select(q => (q.Id, q.TextKey)),
            culture);
    }

    private static async Task<MiniStepData> LoadCultureAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var testsV2 = await featureFlags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var cultureState = await api.GetMyCultureAsync(cancellationToken);
        var version = OnboardingMiniBlockCompletion.ResolveQuickScanVersion(cultureState?.QuickScanVersion, testsV2);
        return Build(
            cultureState?.Answers,
            version,
            9,
            AssessmentQuickScanCatalog.CultureQuestions(version).Select(q => (q.Id, q.TextKey)),
            culture);
    }

    private static async Task<MiniStepData> LoadValuesAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var testsV2 = await featureFlags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var values = await api.GetMyValuesAsync(cancellationToken);
        var version = OnboardingMiniBlockCompletion.ResolveQuickScanVersion(values?.QuickScanVersion, testsV2);
        return Build(
            values?.Answers,
            version,
            10,
            AssessmentQuickScanCatalog.ValuesQuestions(version).Select(q => (q.Id, q.TextKey)),
            culture);
    }

    private static MiniStepData Build(
        Dictionary<string, int>? answers,
        int version,
        int step,
        IEnumerable<(int Id, string TextKey)> catalog,
        CultureState culture)
    {
        var questionIds = OnboardingMiniBlockCompletion.MiniQuestionIds(step, version);
        var catalogById = catalog.ToDictionary(item => item.Id, item => item.TextKey);
        var serverAnswers = new Dictionary<int, int>();
        foreach (var answer in answers ?? [])
        {
            if (int.TryParse(answer.Key, out var id)
                && answer.Value is >= 1 and <= 5)
            {
                serverAnswers[id] = answer.Value;
            }
        }

        var visible = OnboardingMiniBlockCompletion.MergeVisible(step, version, serverAnswers, serverAnswers);
        var questions = questionIds
            .Where(catalogById.ContainsKey)
            .Select(id => new OnboardingClientStepSeed.MiniQuestionSeed(id, culture[catalogById[id]]))
            .ToList();

        return new MiniStepData(questions, visible, version);
    }
}
