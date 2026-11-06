using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

public enum GratisDnaTestKind
{
    Competency,
    Career,
    Culture,
    Values
}

public sealed record GratisDnaServerTestState(
    string? Status,
    IReadOnlyDictionary<int, int> ServerAnswers);

public sealed record GratisDnaTestMergeAction(
    GratisDnaTestKind Test,
    bool SkipBecauseCompleted,
    IReadOnlyDictionary<int, int>? AnswersToSave);

public sealed record GratisDnaMergePlan(
    bool IsNoOp,
    bool ClearStorageAfterSuccess,
    bool ShouldAutoAcceptTestConsent,
    IReadOnlyList<GratisDnaTestMergeAction> Tests);

/// <summary>Pure merge decisions for gratis DNA → candidate profile (§6.3).</summary>
public static class GratisDnaMerge
{
    public static GratisDnaMergePlan Plan(
        GratisDnaStoragePayload? stored,
        bool canUseCandidateFeatures,
        bool userHasTestAiConsent,
        string currentProfilingConsentVersion,
        IReadOnlyDictionary<GratisDnaTestKind, GratisDnaServerTestState> serverStates)
    {
        if (stored is null)
        {
            return NoOp();
        }

        if (!canUseCandidateFeatures)
        {
            return NoOp();
        }

        var storedConsentMatches = string.Equals(
            stored.Consent.Version,
            currentProfilingConsentVersion,
            StringComparison.Ordinal);

        var shouldAutoConsent = storedConsentMatches && !userHasTestAiConsent;
        if (!userHasTestAiConsent && !shouldAutoConsent)
        {
            return NoOp();
        }

        var tests = new List<GratisDnaTestMergeAction>(4);
        foreach (var kind in Enum.GetValues<GratisDnaTestKind>())
        {
            if (!serverStates.TryGetValue(kind, out var server))
            {
                server = new GratisDnaServerTestState(null, new Dictionary<int, int>());
            }

            tests.Add(PlanTest(kind, stored, server));
        }

        return new GratisDnaMergePlan(
            IsNoOp: false,
            ClearStorageAfterSuccess: true,
            ShouldAutoAcceptTestConsent: shouldAutoConsent,
            Tests: tests);
    }

    private static GratisDnaTestMergeAction PlanTest(
        GratisDnaTestKind kind,
        GratisDnaStoragePayload stored,
        GratisDnaServerTestState server)
    {
        if (CandidateCompetencyStatuses.IsCompleted(server.Status))
        {
            return new GratisDnaTestMergeAction(kind, SkipBecauseCompleted: true, AnswersToSave: null);
        }

        var storedAnswers = StoredAnswersFor(stored, kind);
        var merged = new Dictionary<int, int>(server.ServerAnswers);
        var added = false;
        foreach (var (id, value) in storedAnswers)
        {
            if (merged.ContainsKey(id))
            {
                continue;
            }

            merged[id] = value;
            added = true;
        }

        return added
            ? new GratisDnaTestMergeAction(kind, SkipBecauseCompleted: false, AnswersToSave: merged)
            : new GratisDnaTestMergeAction(kind, SkipBecauseCompleted: false, AnswersToSave: null);
    }

    private static IReadOnlyDictionary<int, int> StoredAnswersFor(GratisDnaStoragePayload stored, GratisDnaTestKind kind)
        => kind switch
        {
            GratisDnaTestKind.Competency => stored.Answers.Competency,
            GratisDnaTestKind.Career => stored.Answers.Career,
            GratisDnaTestKind.Culture => stored.Answers.Culture,
            GratisDnaTestKind.Values => stored.Answers.Values,
            _ => new Dictionary<int, int>()
        };

    private static GratisDnaMergePlan NoOp()
        => new(IsNoOp: true, ClearStorageAfterSuccess: false, ShouldAutoAcceptTestConsent: false, Tests: []);
}
