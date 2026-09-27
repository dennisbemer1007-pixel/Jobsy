using Jobsy.Core.Entities;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class GratisDnaMergePlanTests
{
    private static GratisDnaStoragePayload SampleStored() => new()
    {
        V = 1,
        CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
        ExpiresAtUtc = DateTime.UtcNow.AddDays(6),
        AgeBand = GratisDnaStoragePayload.AgeBand16Plus,
        Consent = new GratisDnaStoredConsent
        {
            Version = PrivacyConstants.CandidateProfilingConsentVersion,
            AtUtc = DateTime.UtcNow.AddDays(-1)
        },
        Answers = new GratisDnaStoredAnswers
        {
            Competency = new Dictionary<int, int> { [1] = 4, [6] = 5 },
            Career = new Dictionary<int, int> { [14] = 3 },
            Culture = new Dictionary<int, int> { [1] = 5 },
            Values = new Dictionary<int, int> { [21] = 4 }
        }
    };

    [Fact]
    public void Plan_fills_only_unanswered_ids_and_skips_completed()
    {
        var stored = SampleStored();
        var server = new Dictionary<GratisDnaTestKind, GratisDnaServerTestState>
        {
            [GratisDnaTestKind.Competency] = new(
                CandidateCompetencyStatuses.Draft,
                new Dictionary<int, int> { [1] = 2 }),
            [GratisDnaTestKind.Career] = new(CandidateCompetencyStatuses.Completed, new Dictionary<int, int> { [1] = 5 }),
            [GratisDnaTestKind.Culture] = new(CandidateCompetencyStatuses.Draft, new Dictionary<int, int>()),
            [GratisDnaTestKind.Values] = new(CandidateCompetencyStatuses.Draft, new Dictionary<int, int>())
        };

        var plan = GratisDnaMerge.Plan(
            stored,
            canUseCandidateFeatures: true,
            userHasTestAiConsent: true,
            PrivacyConstants.CandidateProfilingConsentVersion,
            server);

        Assert.False(plan.IsNoOp);
        Assert.True(plan.ClearStorageAfterSuccess);

        var competency = plan.Tests.Single(t => t.Test == GratisDnaTestKind.Competency);
        Assert.False(competency.SkipBecauseCompleted);
        Assert.NotNull(competency.AnswersToSave);
        Assert.Equal(2, competency.AnswersToSave![1]);
        Assert.Equal(5, competency.AnswersToSave[6]);

        var career = plan.Tests.Single(t => t.Test == GratisDnaTestKind.Career);
        Assert.True(career.SkipBecauseCompleted);
        Assert.Null(career.AnswersToSave);
    }

    [Fact]
    public void Plan_no_op_when_candidate_features_blocked_or_consent_mismatch()
    {
        var stored = SampleStored();
        var server = EmptyServer();

        var blocked = GratisDnaMerge.Plan(stored, false, true, PrivacyConstants.CandidateProfilingConsentVersion, server);
        Assert.True(blocked.IsNoOp);
        Assert.False(blocked.ClearStorageAfterSuccess);

        stored = new GratisDnaStoragePayload
        {
            V = SampleStored().V,
            CreatedAtUtc = SampleStored().CreatedAtUtc,
            ExpiresAtUtc = SampleStored().ExpiresAtUtc,
            AgeBand = SampleStored().AgeBand,
            Answers = SampleStored().Answers,
            Consent = new GratisDnaStoredConsent { Version = "2020-01-01", AtUtc = DateTime.UtcNow }
        };
        var oldConsent = GratisDnaMerge.Plan(stored, true, false, PrivacyConstants.CandidateProfilingConsentVersion, server);
        Assert.True(oldConsent.IsNoOp);
        Assert.False(oldConsent.ShouldAutoAcceptTestConsent);
    }

    [Fact]
    public void Plan_suggests_auto_consent_when_stored_version_matches_and_user_lacks_consent()
    {
        var plan = GratisDnaMerge.Plan(
            SampleStored(),
            canUseCandidateFeatures: true,
            userHasTestAiConsent: false,
            PrivacyConstants.CandidateProfilingConsentVersion,
            EmptyServer());

        Assert.False(plan.IsNoOp);
        Assert.True(plan.ShouldAutoAcceptTestConsent);
        Assert.True(plan.ClearStorageAfterSuccess);
    }

    private static Dictionary<GratisDnaTestKind, GratisDnaServerTestState> EmptyServer()
        => new()
        {
            [GratisDnaTestKind.Competency] = new(null, new Dictionary<int, int>()),
            [GratisDnaTestKind.Career] = new(null, new Dictionary<int, int>()),
            [GratisDnaTestKind.Culture] = new(null, new Dictionary<int, int>()),
            [GratisDnaTestKind.Values] = new(null, new Dictionary<int, int>())
        };
}
