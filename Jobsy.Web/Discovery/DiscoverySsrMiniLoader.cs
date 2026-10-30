using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;

namespace Jobsy.Web.Discovery;

internal static class DiscoverySsrMiniLoader
{
    internal sealed record BandData(
        IReadOnlyList<OnboardingClientStepSeed.MiniQuestionSeed> Questions,
        IReadOnlyDictionary<int, int> Answers,
        int QuickScanVersion,
        bool BandComplete);

    public static async Task<BandData> LoadBandAsync(
        int step,
        int targetLevel,
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken = default)
    {
        var mini = await OnboardingSsrMiniStepLoader.LoadAsync(step, api, featureFlags, culture, cancellationToken);
        var assessmentKind = step switch
        {
            7 => AssessmentKind.Competence,
            8 => AssessmentKind.Career,
            9 => AssessmentKind.Culture,
            _ => AssessmentKind.Values
        };

        var ids = TestDepthRules.QuestionIdsUpTo(assessmentKind, targetLevel, mini.QuickScanVersion).ToArray();
        var idSet = ids.ToHashSet();
        var questions = mini.Questions.Where(q => idSet.Contains(q.Id)).ToList();
        var answers = mini.Answers.Where(kv => idSet.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        var complete = questions.Count > 0 && questions.All(q =>
            answers.TryGetValue(q.Id, out var s) && s is >= 1 and <= 5);
        return new BandData(questions, answers, mini.QuickScanVersion, complete);
    }
}
