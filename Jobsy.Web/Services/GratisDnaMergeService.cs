using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

public sealed class GratisDnaMergeService(
    GratisDnaStorage storage,
    JobsyApiClient api,
    ILogger<GratisDnaMergeService> logger)
{
    private Task? _mergeTask;

    public Task TryMergeAsync()
    {
        _mergeTask ??= RunMergeAsync();
        return _mergeTask;
    }

    private async Task RunMergeAsync()
    {
        try
        {
            var stored = await storage.LoadAsync();
            var profile = await api.GetMyProfileAsync();
            if (profile is null)
            {
                return;
            }

            var competencyTask = api.GetMyCompetenciesAsync();
            var careerTask = api.GetMyCareerInterestsAsync();
            var cultureTask = api.GetMyCultureAsync();
            var valuesTask = api.GetMyValuesAsync();
            await Task.WhenAll(competencyTask, careerTask, cultureTask, valuesTask);

            var competency = await competencyTask;
            var career = await careerTask;
            var culture = await cultureTask;
            var values = await valuesTask;

            var serverStates = new Dictionary<GratisDnaTestKind, GratisDnaServerTestState>
            {
                [GratisDnaTestKind.Competency] = ToServerState(competency?.Status, competency?.Answers),
                [GratisDnaTestKind.Career] = ToServerState(career?.Status, career?.Answers),
                [GratisDnaTestKind.Culture] = ToServerState(culture?.Status, culture?.Answers),
                [GratisDnaTestKind.Values] = ToServerState(values?.Status, values?.Answers)
            };

            var plan = GratisDnaMerge.Plan(
                stored,
                GratisDnaMergeProfileRules.CanUseCandidateFeatures(profile),
                GratisDnaMergeProfileRules.HasCurrentTestAiConsent(profile),
                PrivacyConstants.CandidateProfilingConsentVersion,
                serverStates);

            if (plan.IsNoOp)
            {
                return;
            }

            if (plan.ShouldAutoAcceptTestConsent)
            {
                var consentProfile = await api.AcceptTestAiConsentAsync();
                if (consentProfile?.TestAiConsentAt is null)
                {
                    logger.LogWarning("Gratis DNA merge: auto test consent was not recorded");
                    return;
                }
            }

            foreach (var action in plan.Tests)
            {
                if (action.SkipBecauseCompleted)
                {
                    continue;
                }

                if (action.AnswersToSave is null)
                {
                    continue;
                }

                try
                {
                    switch (action.Test)
                    {
                        case GratisDnaTestKind.Competency:
                            await api.SaveMyCompetenciesAsync(action.AnswersToSave, complete: false);
                            break;
                        case GratisDnaTestKind.Career:
                            await api.SaveMyCareerInterestsAsync(action.AnswersToSave, complete: false);
                            break;
                        case GratisDnaTestKind.Culture:
                            await api.SaveMyCultureAsync(action.AnswersToSave, complete: false);
                            break;
                        case GratisDnaTestKind.Values:
                            await api.SaveMyValuesAsync(action.AnswersToSave, complete: false);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Gratis DNA merge: save failed for test {TestKind}", action.Test);
                    return;
                }
            }

            if (!plan.ClearStorageAfterSuccess)
            {
                return;
            }

            await storage.ClearAsync();
            GratisDnaMergeNotifier.NotifyMerged();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gratis DNA merge failed");
        }
    }

    private static GratisDnaServerTestState ToServerState(string? status, Dictionary<string, int>? answers)
        => new(status, GratisDnaServerAnswerParser.Parse(answers));
}
