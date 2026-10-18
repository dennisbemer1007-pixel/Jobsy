using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

internal static class TestReflectionScoreLoader
{
    public static async Task<TestReflectionScoreSnapshot?> LoadFullAsync(
        JobsyDbContext db,
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken)
    {
        return kind switch
        {
            AssessmentKind.Competence => await LoadCompetenceAsync(db, userId, cancellationToken),
            AssessmentKind.Career => await LoadCareerAsync(db, userId, cancellationToken),
            AssessmentKind.Culture => await LoadCultureAsync(db, userId, cancellationToken),
            AssessmentKind.Values => await LoadValuesAsync(db, userId, cancellationToken),
            _ => null
        };
    }

    private static async Task<TestReflectionScoreSnapshot?> LoadCompetenceAsync(
        JobsyDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var row = await db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        var full = TestDepthRules.FullCount(AssessmentKind.Competence, version);
        var answers = QuickScanAssessmentBridge.ParseAnswers(AssessmentKind.Competence, row.AnswersJson, version);
        var depth = Math.Max(answers.Count, row.QuickScanHighestAnswered);
        if (TestDepthRules.Reached(AssessmentKind.Competence, depth, version) != TestDepthLevel.Full)
        {
            return null;
        }

        var scores = CompetencyTestCatalog.CompletedScoresOrNull(
            row.Status,
            row.SamenwerkenPercent,
            row.ResultaatgerichtheidPercent,
            row.StressbestendigheidPercent,
            row.InnovatiePercent,
            row.ExtraversiePercent);
        if (scores is null)
        {
            return null;
        }

        var traits = BuildTraits(
        [
            (CompetencyTestCatalog.Samenwerken, scores.Samenwerken),
            (CompetencyTestCatalog.Resultaatgerichtheid, scores.Resultaatgerichtheid),
            (CompetencyTestCatalog.Stressbestendigheid, scores.Stressbestendigheid),
            (CompetencyTestCatalog.Innovatie, scores.Innovatie),
            (CompetencyTestCatalog.Extraversie, scores.Extraversie)
        ]);

        var dream = await LoadDreamTitleAsync(db, userId, cancellationToken);
        return new TestReflectionScoreSnapshot(AssessmentKind.Competence, version, depth, traits, dream);
    }

    private static async Task<TestReflectionScoreSnapshot?> LoadCareerAsync(
        JobsyDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var row = await db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        var answers = QuickScanAssessmentBridge.ParseAnswers(AssessmentKind.Career, row.AnswersJson, version);
        var depth = Math.Max(answers.Count, row.QuickScanHighestAnswered);
        if (TestDepthRules.Reached(AssessmentKind.Career, depth, version) != TestDepthLevel.Full)
        {
            return null;
        }

        var scores = CareerTestCatalog.CompletedScoresOrNull(
            row.Status,
            row.RealisticPercent,
            row.InvestigativePercent,
            row.ArtisticPercent,
            row.SocialPercent,
            row.EnterprisingPercent,
            row.ConventionalPercent);
        if (scores is null)
        {
            return null;
        }

        var traits = BuildTraits(
        [
            (CareerTestCatalog.Realistic, scores.Realistic),
            (CareerTestCatalog.Investigative, scores.Investigative),
            (CareerTestCatalog.Artistic, scores.Artistic),
            (CareerTestCatalog.Social, scores.Social),
            (CareerTestCatalog.Enterprising, scores.Enterprising),
            (CareerTestCatalog.Conventional, scores.Conventional)
        ]);

        var dream = await LoadDreamTitleAsync(db, userId, cancellationToken);
        return new TestReflectionScoreSnapshot(AssessmentKind.Career, version, depth, traits, dream);
    }

    private static async Task<TestReflectionScoreSnapshot?> LoadCultureAsync(
        JobsyDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var row = await db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        var answers = QuickScanAssessmentBridge.ParseAnswers(AssessmentKind.Culture, row.AnswersJson, version);
        var depth = Math.Max(answers.Count, row.QuickScanHighestAnswered);
        if (TestDepthRules.Reached(AssessmentKind.Culture, depth, version) != TestDepthLevel.Full)
        {
            return null;
        }

        if (!CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        var scores = new CulturePersonalityScores(
            row.AutonomyPercent,
            row.InformalPercent,
            row.CollaborationPercent,
            row.FlexibilityPercent,
            row.InnovationPercent,
            row.PeopleFirstPercent,
            row.OpennessPercent,
            row.ConscientiousnessPercent,
            row.ExtraversionPercent,
            row.AgreeablenessPercent,
            row.EmotionalStabilityPercent);
        if (!scores.IsComplete)
        {
            return null;
        }

        var traits = BuildTraits(
        [
            (CulturePersonalityCatalog.Autonomy, scores.Autonomy),
            (CulturePersonalityCatalog.Informal, scores.Informal),
            (CulturePersonalityCatalog.Collaboration, scores.Collaboration),
            (CulturePersonalityCatalog.Flexibility, scores.Flexibility),
            (CulturePersonalityCatalog.Innovation, scores.Innovation),
            (CulturePersonalityCatalog.PeopleFirst, scores.PeopleFirst)
        ]);

        return new TestReflectionScoreSnapshot(AssessmentKind.Culture, version, depth, traits, null);
    }

    private static async Task<TestReflectionScoreSnapshot?> LoadValuesAsync(
        JobsyDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var row = await db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        var answers = QuickScanAssessmentBridge.ParseAnswers(AssessmentKind.Values, row.AnswersJson, version);
        var depth = Math.Max(answers.Count, row.QuickScanHighestAnswered);
        if (TestDepthRules.Reached(AssessmentKind.Values, depth, version) != TestDepthLevel.Full)
        {
            return null;
        }

        if (!CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        var scores = new SchwartzValuesScores(
            row.AutonomyPercent,
            row.ConnectionPercent,
            row.AchievementPercent,
            row.StabilityPercent,
            row.ImpactPercent);
        if (!scores.IsComplete)
        {
            return null;
        }

        var traits = BuildTraits(
        [
            (SchwartzValuesCatalog.Autonomy, scores.Autonomy),
            (SchwartzValuesCatalog.Connection, scores.Connection),
            (SchwartzValuesCatalog.Achievement, scores.Achievement),
            (SchwartzValuesCatalog.Stability, scores.Stability),
            (SchwartzValuesCatalog.Impact, scores.Impact)
        ]);

        return new TestReflectionScoreSnapshot(AssessmentKind.Values, version, depth, traits, null);
    }

    private static async Task<string?> LoadDreamTitleAsync(
        JobsyDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var plan = await db.CandidateCareerPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return string.IsNullOrWhiteSpace(plan?.DreamTitle) ? null : plan!.DreamTitle.Trim();
    }

    private static IReadOnlyList<TestReflectionTrait> BuildTraits(IEnumerable<(string Code, int? Score)> items)
        => items
            .Where(x => x.Score is not null)
            .Select(x => new TestReflectionTrait(x.Code, x.Score!.Value, DimensionLabels.For(x.Code)))
            .ToList();
}
