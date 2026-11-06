using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateCompetencyService : ICandidateCompetencyService
{
    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;
    private readonly ICandidateMatchSnapshotService _matchSnapshots;
    private readonly ICandidateInsightsQueue _queue;
    private readonly IAssessmentResearchService _research;
    private readonly AssessmentSaveGuard _saveGuard;
    private readonly IFeatureFlags _flags;

    public CandidateCompetencyService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICandidateMatchSnapshotService matchSnapshots,
        ICandidateInsightsQueue queue,
        IAssessmentResearchService research,
        AssessmentSaveGuard saveGuard,
        IFeatureFlags flags)
    {
        _db = db;
        _commercial = commercial;
        _matchSnapshots = matchSnapshots;
        _queue = queue;
        _research = research;
        _saveGuard = saveGuard;
        _flags = flags;
    }

    public async Task<CandidateCompetencyStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var price = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Competence);
        return ToDto(row, price);
    }

    public async Task<CandidateCompetencyStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCompetencies
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (answers.Count == 0 && row is not null && CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            // Journey "Naar het licht" can re-post with no answers loaded. Keep the result.
            var kept = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Competence);
            return ToDto(row, kept);
        }

        var testsV2 = await _flags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var version = QuickScanVersionPolicy.ResolveVersionForNew(row?.QuickScanVersion, testsV2);

        var error = QuickScanAssessmentBridge.ValidateAnswers(
            AssessmentKind.Competence, answers, complete, version);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        if (answers.Count == 0)
        {
            if (row is not null)
            {
                throw new InvalidOperationException(
                    "Lege antwoorden overschrijven je bestaande test niet. Stuur de huidige antwoorden mee.");
            }

            return ToDto(null, FlexCommercialSettings.DefaultDeepAnalysisPriceEuro);
        }

        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Draft,
                AnswersJson = "{}",
                CreatedAtUtc = now
            };
            row.QuickScanVersion = version;
            _db.CandidateCompetencies.Add(row);
        }

        version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        row.QuickScanHighestAnswered = Math.Max(row.QuickScanHighestAnswered, answers.Count);

        var storedAnswers = QuickScanAssessmentBridge.ParseAnswers(
            AssessmentKind.Competence, row.AnswersJson, version);
        var wasCompleted = CandidateCompetencyStatuses.IsCompleted(row.Status)
            && QuickScanAssessmentBridge.IsComplete(AssessmentKind.Competence, storedAnswers, version);
        var answersJson = QuickScanAssessmentBridge.SerializeAnswers(
            AssessmentKind.Competence, answers, version);
        var previousSnapshot = wasCompleted
            ? System.Text.Json.JsonSerializer.Serialize(new
            {
                answers = row.AnswersJson,
                scores = new
                {
                    row.SamenwerkenPercent,
                    row.ResultaatgerichtheidPercent,
                    row.StressbestendigheidPercent,
                    row.InnovatiePercent,
                    row.ExtraversiePercent
                }
            })
            : null;

        if (wasCompleted && !complete)
        {
            await _saveGuard.SaveDraftAsync(
                userId,
                AssessmentKind.Competence,
                AssessmentVariant.Quick,
                answersJson,
                previousSnapshot,
                cancellationToken);
            row.AnswersJson = answersJson;
            row.UpdatedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            var draftPrice = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Competence);
            return ToDto(row, draftPrice);
        }

        if (wasCompleted && complete)
        {
            var baselineAnswers = row.AnswersJson;
            var open = await _db.CandidateAssessmentAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.UserId == userId
                         && a.Kind == AssessmentKind.Competence
                         && a.Variant == AssessmentVariant.Quick
                         && a.Status == CandidateAssessmentAttempt.AttemptStatus.Open,
                    cancellationToken);
            if (open?.PreviousSnapshotJson is { Length: > 0 } snap)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(snap);
                    if (doc.RootElement.TryGetProperty("answers", out var ans))
                    {
                        baselineAnswers = ans.GetString() ?? baselineAnswers;
                    }
                }
                catch
                {
                    // keep row.AnswersJson
                }
            }

            var previewComplete = AssessmentQuickScanCatalog.ScoreCompetence(answers, version);
            if (previewComplete is not { IsComplete: true })
            {
                throw new InvalidOperationException(
                    $"Beantwoord alle {QuickScanAssessmentBridge.FullCount(AssessmentKind.Competence, version)} vragen om de test af te ronden.");
            }

            var scoresJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                previewComplete.Samenwerken,
                previewComplete.Resultaatgerichtheid,
                previewComplete.Stressbestendigheid,
                previewComplete.Innovatie,
                previewComplete.Extraversie
            });

            var noOp = await _saveGuard.CommitCompleteAsync(
                userId,
                AssessmentKind.Competence,
                AssessmentVariant.Quick,
                answersJson,
                scoresJson,
                previousSnapshot,
                baselineAnswers,
                cancellationToken);
            if (noOp && ScoresMatch(row, previewComplete))
            {
                var priceNoOp = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Competence);
                return ToDto(row, priceNoOp);
            }

            row.AnswersJson = answersJson;
            row.UpdatedAtUtc = now;
            row.Status = CandidateCompetencyStatuses.Completed;
            row.SamenwerkenPercent = previewComplete.Samenwerken;
            row.ResultaatgerichtheidPercent = previewComplete.Resultaatgerichtheid;
            row.StressbestendigheidPercent = previewComplete.Stressbestendigheid;
            row.InnovatiePercent = previewComplete.Innovatie;
            row.ExtraversiePercent = previewComplete.Extraversie;
            row.RiasecTagsJson = "[]";
            row.MatchTagsJson = CompetencyTestCatalog.SerializeTags(
                QuickScanAssessmentBridge.DeriveCompetencyTags(previewComplete, version));
            row.CompletedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            await _matchSnapshots.MarkInputsStaleAsync(userId, cancellationToken);
            await ResearchQuickScanSync.AfterCompleteAsync(
                _research,
                userId,
                AssessmentKind.Competence,
                answers,
                itemDurationMs: null,
                totalDurationMs: null,
                cancellationToken);
            _queue.TryEnqueue(userId);
            var priceDone = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Competence);
            return ToDto(row, priceDone);
        }

        row.AnswersJson = answersJson;
        row.UpdatedAtUtc = now;

        var preview = AssessmentQuickScanCatalog.ScoreCompetence(answers, version);
        if (complete)
        {
            if (preview is not { IsComplete: true })
            {
                throw new InvalidOperationException(
                    $"Beantwoord alle {QuickScanAssessmentBridge.FullCount(AssessmentKind.Competence, version)} vragen om de Quick-Scan af te ronden.");
            }

            row.Status = CandidateCompetencyStatuses.Completed;
            row.SamenwerkenPercent = preview.Samenwerken;
            row.ResultaatgerichtheidPercent = preview.Resultaatgerichtheid;
            row.StressbestendigheidPercent = preview.Stressbestendigheid;
            row.InnovatiePercent = preview.Innovatie;
            row.ExtraversiePercent = preview.Extraversie;
            row.RiasecTagsJson = "[]";
            row.MatchTagsJson = CompetencyTestCatalog.SerializeTags(
                QuickScanAssessmentBridge.DeriveCompetencyTags(preview, version));
            row.CompletedAtUtc = now;
        }
        else if (!CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.SamenwerkenPercent = null;
            row.ResultaatgerichtheidPercent = null;
            row.StressbestendigheidPercent = null;
            row.InnovatiePercent = null;
            row.ExtraversiePercent = null;
            row.RiasecTagsJson = "[]";
            row.MatchTagsJson = "[]";
            row.CompletedAtUtc = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _matchSnapshots.MarkInputsStaleAsync(userId, cancellationToken);
        if (complete)
        {
            await ResearchQuickScanSync.AfterCompleteAsync(
                _research,
                userId,
                AssessmentKind.Competence,
                answers,
                itemDurationMs: null,
                totalDurationMs: null,
                cancellationToken);
            _queue.TryEnqueue(userId);
        }

        var price = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Competence);
        return ToDto(row, price);
    }

    public async Task<CompetencyScores?> GetCompletedScoresAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        return CompetencyTestCatalog.CompletedScoresOrNull(
            row.Status,
            row.SamenwerkenPercent,
            row.ResultaatgerichtheidPercent,
            row.StressbestendigheidPercent,
            row.InnovatiePercent,
            row.ExtraversiePercent);
    }

    public async Task<IReadOnlyList<CandidateMatchedVacancyDto>> GetTopMatchesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (matches, _) = await _matchSnapshots.GetAsync(userId, cancellationToken);
        return matches;
    }

    private static bool ScoresMatch(CandidateCompetency row, CompetencyScores? preview)
        => preview is { IsComplete: true }
           && row.SamenwerkenPercent == preview.Samenwerken
           && row.ResultaatgerichtheidPercent == preview.Resultaatgerichtheid
           && row.StressbestendigheidPercent == preview.Stressbestendigheid
           && row.InnovatiePercent == preview.Innovatie
           && row.ExtraversiePercent == preview.Extraversie;

    private static CandidateCompetencyStateDto ToDto(CandidateCompetency? row, decimal deepAnalysisPriceEuro)
    {
        var version = row is null ? 0 : AssessmentTestVersions.Normalize(row.QuickScanVersion);
        var fullCount = QuickScanAssessmentBridge.FullCount(AssessmentKind.Competence, version);
        var answers = QuickScanAssessmentBridge.ParseAnswers(
            AssessmentKind.Competence, row?.AnswersJson, version);
        var preview = AssessmentQuickScanCatalog.ScoreCompetence(answers, version);
        var completed = CompetencyTestCatalog.CompletedScoresOrNull(
            row?.Status,
            row?.SamenwerkenPercent,
            row?.ResultaatgerichtheidPercent,
            row?.StressbestendigheidPercent,
            row?.InnovatiePercent,
            row?.ExtraversiePercent);
        var depthCount = Math.Max(answers.Count, row?.QuickScanHighestAnswered ?? 0);
        return new CandidateCompetencyStateDto(
            row?.Status ?? CandidateCompetencyStatuses.Draft,
            answers,
            answers.Count,
            fullCount,
            completed,
            preview,
            row?.CompletedAtUtc,
            row?.UpdatedAtUtc,
            QuickScanAssessmentBridge.CompetenceQuestions(version)
                .Select(q => new CompetencyQuestionDto(q.Id, q.Category, q.Reverse, q.TextKey, q.IsRiasec))
                .ToList(),
            CompetencyTestCatalog.ParseTagsJson(row?.MatchTagsJson),
            DeepAnalysisService.FormatUpsellCopy(deepAnalysisPriceEuro, AssessmentKind.Competence),
            version,
            fullCount,
            depthCount < fullCount);
    }
}
