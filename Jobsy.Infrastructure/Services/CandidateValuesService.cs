using Jobsy.Core.Enums;
using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateValuesService : ICandidateValuesService
{
    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;
    private readonly ICandidateInsightsQueue _queue;
    private readonly IAssessmentResearchService _research;
    private readonly AssessmentSaveGuard _saveGuard;
    private readonly ICandidateMatchSnapshotService _matchSnapshots;
    private readonly IFeatureFlags _flags;

    public CandidateValuesService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICandidateInsightsQueue queue,
        IAssessmentResearchService research,
        ICandidateMatchSnapshotService matchSnapshots,
        AssessmentSaveGuard saveGuard,
        IFeatureFlags flags)
    {
        _db = db;
        _commercial = commercial;
        _queue = queue;
        _research = research;
        _matchSnapshots = matchSnapshots;
        _saveGuard = saveGuard;
        _flags = flags;
    }

    public async Task<CandidateValuesStateDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var price = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Values);
        return ToDto(row, price);
    }

    public async Task<CandidateValuesStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateValuesProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (answers.Count == 0 && row is not null && CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            var kept = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Values);
            return ToDto(row, kept);
        }

        var testsV2 = await _flags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var version = QuickScanVersionPolicy.ResolveVersionForNew(row?.QuickScanVersion, testsV2);

        var error = QuickScanAssessmentBridge.ValidateAnswers(
            AssessmentKind.Values, answers, complete, version);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        if (answers.Count == 0)
        {
            if (row is not null)
            {
                throw new InvalidOperationException(
                    "Lege antwoorden overschrijven je bestaande waardenscan niet. Stuur de huidige antwoorden mee.");
            }

            return ToDto(null, FlexCommercialSettings.DefaultDeepAnalysisPriceEuro);
        }

        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateValuesProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Draft,
                AnswersJson = "{}",
                CreatedAtUtc = now
            };
            row.QuickScanVersion = version;
            _db.CandidateValuesProfiles.Add(row);
        }

        version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        row.QuickScanHighestAnswered = Math.Max(row.QuickScanHighestAnswered, answers.Count);

        var storedAnswers = QuickScanAssessmentBridge.ParseAnswers(
            AssessmentKind.Values, row.AnswersJson, version);
        // A "completed" row with a short answer set (seed or a partial save) is not a real
        // result yet. Treat the first full save as the original completion so scores update
        // and it does not spend an adjustment.
        var wasCompleted = CandidateCompetencyStatuses.IsCompleted(row.Status)
            && QuickScanAssessmentBridge.IsComplete(AssessmentKind.Values, storedAnswers, version);
        var answersJson = QuickScanAssessmentBridge.SerializeAnswers(
            AssessmentKind.Values, answers, version);
        var baselineAnswersJson = row.AnswersJson;
        if (wasCompleted && !complete)
        {
            await _saveGuard.SaveDraftAsync(userId, AssessmentKind.Values, AssessmentVariant.Quick, answersJson, baselineAnswersJson, cancellationToken);
        }
        else if (wasCompleted && complete)
        {
            var noOp = await _saveGuard.CommitCompleteAsync(
                userId, AssessmentKind.Values, AssessmentVariant.Quick, answersJson, null, baselineAnswersJson, baselineAnswersJson, cancellationToken);
            if (noOp && ScoresMatch(row, QuickScanAssessmentBridge.ScoreValues(answers, version)))
            {
                var priceNoOp = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Values);
                return ToDto(row, priceNoOp);
            }
        }
        row.AnswersJson = answersJson;
        row.UpdatedAtUtc = now;

        var preview = QuickScanAssessmentBridge.ScoreValues(answers, version);
        if (complete)
        {
            if (preview is not { IsComplete: true })
            {
                throw new InvalidOperationException(
                    $"Beantwoord alle {QuickScanAssessmentBridge.FullCount(AssessmentKind.Values, version)} stellingen om de waardenscan af te ronden.");
            }

            ApplyScores(row, preview);
            row.Status = CandidateCompetencyStatuses.Completed;
            row.MatchTagsJson = SchwartzValuesCatalog.SerializeTags(
                QuickScanAssessmentBridge.DeriveValuesTags(preview, version));
            row.CompletedAtUtc = now;
        }
        else if (!CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            ClearScores(row);
            row.Status = CandidateCompetencyStatuses.Draft;
            row.MatchTagsJson = "[]";
            row.CompletedAtUtc = null;
        }
        // Completed + autosave: keep status/scores; AnswersJson above is the pending edit
        // until the candidate finishes again with complete:true (rescore).

        await _db.SaveChangesAsync(cancellationToken);
        await _matchSnapshots.MarkInputsStaleAsync(userId, cancellationToken);
        if (complete)
        {
            await ResearchQuickScanSync.AfterCompleteAsync(
                _research,
                userId,
                AssessmentKind.Values,
                answers,
                itemDurationMs: null,
                totalDurationMs: null,
                cancellationToken);
            _queue.TryEnqueue(userId);
        }

        var price = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Values);
        return ToDto(row, price);
    }

    public async Task<SchwartzValuesScores?> GetCompletedScoresAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.UserId == userId && c.Status == CandidateCompetencyStatuses.Completed,
                cancellationToken);
        return row is null ? null : FromRow(row);
    }

    private static bool ScoresMatch(CandidateValuesProfile row, SchwartzValuesScores? preview)
        => preview is { IsComplete: true }
           && row.AutonomyPercent == preview.Autonomy
           && row.ConnectionPercent == preview.Connection
           && row.AchievementPercent == preview.Achievement
           && row.StabilityPercent == preview.Stability
           && row.ImpactPercent == preview.Impact;

    private static void ApplyScores(CandidateValuesProfile row, SchwartzValuesScores s)
    {
        row.AutonomyPercent = s.Autonomy;
        row.ConnectionPercent = s.Connection;
        row.AchievementPercent = s.Achievement;
        row.StabilityPercent = s.Stability;
        row.ImpactPercent = s.Impact;
    }

    private static void ClearScores(CandidateValuesProfile row)
    {
        row.AutonomyPercent = null;
        row.ConnectionPercent = null;
        row.AchievementPercent = null;
        row.StabilityPercent = null;
        row.ImpactPercent = null;
    }

    private static SchwartzValuesScores? FromRow(CandidateValuesProfile row)
    {
        var scores = new SchwartzValuesScores(
            row.AutonomyPercent,
            row.ConnectionPercent,
            row.AchievementPercent,
            row.StabilityPercent,
            row.ImpactPercent);
        return scores.IsComplete ? scores : null;
    }

    private static CandidateValuesStateDto ToDto(CandidateValuesProfile? row, decimal price)
    {
        if (row is null)
        {
            return new CandidateValuesStateDto(
                CandidateCompetencyStatuses.Draft,
                new Dictionary<int, int>(),
                null,
                [],
                null,
                price,
                PreviewScores: null,
                QuickScanVersion: 0);
        }

        var version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        var fullCount = QuickScanAssessmentBridge.FullCount(AssessmentKind.Values, version);
        var answers = QuickScanAssessmentBridge.ParseAnswers(
            AssessmentKind.Values, row.AnswersJson, version);
        var preview = QuickScanAssessmentBridge.ScoreValues(answers, version);
        var depthCount = Math.Max(answers.Count, row.QuickScanHighestAnswered);
        return new CandidateValuesStateDto(
            row.Status,
            answers,
            FromRow(row),
            SchwartzValuesCatalog.ParseTags(row.MatchTagsJson),
            row.CompletedAtUtc,
            price,
            preview,
            version,
            fullCount,
            depthCount < fullCount);
    }
}
