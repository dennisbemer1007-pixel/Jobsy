using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Resolves assessment scores for DNA/Kompas/matching: completed tests always win;
/// otherwise draft answers (e.g. from the onboarding wizard) yield provisional scores.
/// </summary>
public static class ProvisionalAssessmentScores
{
    public const int NeutralFillPercent = 55;

    public sealed record ResolvedCompetency(CompetencyScores? Scores, bool IsProvisional);
    public sealed record ResolvedRiasec(RiasecScores? Scores, bool IsProvisional);
    public sealed record ResolvedCulture(CulturePersonalityScores? Scores, bool IsProvisional);
    public sealed record ResolvedValues(SchwartzValuesScores? Scores, bool IsProvisional);

    public static ResolvedCompetency ResolveCompetency(
        string? status,
        string? answersJson,
        int? samenwerken,
        int? resultaat,
        int? stress,
        int? innovatie,
        int? extraversie)
    {
        var completed = CompetencyTestCatalog.CompletedScoresOrNull(
            status, samenwerken, resultaat, stress, innovatie, extraversie);
        if (completed is { IsComplete: true })
        {
            return new ResolvedCompetency(completed, false);
        }

        var answers = CompetencyTestCatalog.ParseAnswersJson(answersJson);
        if (answers.Count == 0)
        {
            return new ResolvedCompetency(null, false);
        }

        var preview = CompetencyTestCatalog.Score(answers);
        if (preview is null)
        {
            return new ResolvedCompetency(null, false);
        }

        // Wizard mini fills one item per dimension → treat as usable provisional.
        return new ResolvedCompetency(preview, true);
    }

    public static ResolvedRiasec ResolveCareer(
        string? status,
        string? answersJson,
        int? realistic,
        int? investigative,
        int? artistic,
        int? social,
        int? enterprising,
        int? conventional)
    {
        var completed = CareerTestCatalog.CompletedScoresOrNull(
            status, realistic, investigative, artistic, social, enterprising, conventional);
        if (completed is { IsComplete: true })
        {
            return new ResolvedRiasec(completed, false);
        }

        var answers = CareerTestCatalog.ParseAnswersJson(answersJson);
        if (answers.Count == 0)
        {
            return new ResolvedRiasec(null, false);
        }

        var preview = CareerTestCatalog.Score(answers);
        return preview is null
            ? new ResolvedRiasec(null, false)
            : new ResolvedRiasec(preview, true);
    }

    public static ResolvedCulture ResolveCulture(string? status, string? answersJson, CulturePersonalityScores? storedCompleted)
    {
        if (storedCompleted is { IsComplete: true }
            && CandidateCompetencyStatuses.IsCompleted(status))
        {
            return new ResolvedCulture(storedCompleted, false);
        }

        var answers = CulturePersonalityCatalog.ParseAnswers(answersJson);
        if (answers.Count == 0)
        {
            return new ResolvedCulture(null, false);
        }

        var preview = CulturePersonalityCatalog.Score(answers);
        if (preview is null)
        {
            return new ResolvedCulture(null, false);
        }

        // Pad missing dims with neutral so matching/DNA can use the partial mini-test.
        var padded = PadCulture(preview);
        return new ResolvedCulture(padded, true);
    }

    public static ResolvedValues ResolveValues(
        string? status,
        string? answersJson,
        int? autonomy,
        int? connection,
        int? achievement,
        int? stability,
        int? impact)
    {
        if (CandidateCompetencyStatuses.IsCompleted(status)
            && autonomy is not null
            && connection is not null
            && achievement is not null
            && stability is not null
            && impact is not null)
        {
            var completed = new SchwartzValuesScores(autonomy, connection, achievement, stability, impact);
            if (completed is { IsComplete: true })
            {
                return new ResolvedValues(completed, false);
            }
        }

        var answers = SchwartzValuesCatalog.ParseAnswers(answersJson);
        if (answers.Count == 0)
        {
            return new ResolvedValues(null, false);
        }

        var preview = SchwartzValuesCatalog.Score(answers);
        return preview is null
            ? new ResolvedValues(null, false)
            : new ResolvedValues(preview, true);
    }

    /// <summary>True when draft answers exist but the test is not fully completed.</summary>
    public static bool HasProvisionalDraft(string? status, int answeredCount)
        => !CandidateCompetencyStatuses.IsCompleted(status) && answeredCount > 0;

    private static CulturePersonalityScores PadCulture(CulturePersonalityScores s)
        => new(
            Autonomy: s.Autonomy ?? NeutralFillPercent,
            Informal: s.Informal ?? NeutralFillPercent,
            Collaboration: s.Collaboration ?? NeutralFillPercent,
            Flexibility: s.Flexibility ?? NeutralFillPercent,
            Innovation: s.Innovation ?? NeutralFillPercent,
            PeopleFirst: s.PeopleFirst ?? NeutralFillPercent,
            Openness: s.Openness ?? NeutralFillPercent,
            Conscientiousness: s.Conscientiousness ?? NeutralFillPercent,
            Extraversion: s.Extraversion ?? NeutralFillPercent,
            Agreeableness: s.Agreeableness ?? NeutralFillPercent,
            EmotionalStability: s.EmotionalStability ?? NeutralFillPercent);
}
