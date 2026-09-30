using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
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
    private readonly AssessmentSaveGuard _saveGuard;

    public CandidateCompetencyService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICandidateMatchSnapshotService matchSnapshots,
        ICandidateInsightsQueue queue,
        AssessmentSaveGuard saveGuard)
    {
        _db = db;
        _commercial = commercial;
        _matchSnapshots = matchSnapshots;
        _queue = queue;
        _saveGuard = saveGuard;
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
        var error = CompetencyTestCatalog.ValidateAnswers(answers, complete);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        var row = await _db.CandidateCompetencies
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
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
            _db.CandidateCompetencies.Add(row);
        }

        var wasCompleted = CandidateCompetencyStatuses.IsCompleted(row.Status);
        var answersJson = CompetencyTestCatalog.SerializeAnswers(answers);
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

            var previewComplete = CompetencyTestCatalog.Score(answers);
            if (previewComplete is not { IsComplete: true })
            {
                throw new InvalidOperationException("Beantwoord alle 25 vragen om de test af te ronden.");
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
            if (noOp)
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
                CompetencyTestCatalog.DeriveMatchTags(previewComplete));
            row.CompletedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            await _matchSnapshots.MarkInputsStaleAsync(userId, cancellationToken);
            _queue.TryEnqueue(userId);
            var priceDone = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Competence);
            return ToDto(row, priceDone);
        }

        row.AnswersJson = answersJson;
        row.UpdatedAtUtc = now;

        var preview = CompetencyTestCatalog.Score(answers);
        if (complete)
        {
            if (preview is not { IsComplete: true })
            {
                throw new InvalidOperationException("Beantwoord alle 25 vragen om de Quick-Scan af te ronden.");
            }

            row.Status = CandidateCompetencyStatuses.Completed;
            row.SamenwerkenPercent = preview.Samenwerken;
            row.ResultaatgerichtheidPercent = preview.Resultaatgerichtheid;
            row.StressbestendigheidPercent = preview.Stressbestendigheid;
            row.InnovatiePercent = preview.Innovatie;
            row.ExtraversiePercent = preview.Extraversie;
            row.RiasecTagsJson = "[]";
            row.MatchTagsJson = CompetencyTestCatalog.SerializeTags(
                CompetencyTestCatalog.DeriveMatchTags(preview));
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

    private static CandidateCompetencyStateDto ToDto(CandidateCompetency? row, decimal deepAnalysisPriceEuro)
    {
        var answers = CompetencyTestCatalog.ParseAnswersJson(row?.AnswersJson);
        var preview = CompetencyTestCatalog.Score(answers);
        var completed = CompetencyTestCatalog.CompletedScoresOrNull(
            row?.Status,
            row?.SamenwerkenPercent,
            row?.ResultaatgerichtheidPercent,
            row?.StressbestendigheidPercent,
            row?.InnovatiePercent,
            row?.ExtraversiePercent);
        return new CandidateCompetencyStateDto(
            row?.Status ?? CandidateCompetencyStatuses.Draft,
            answers,
            answers.Count,
            CompetencyTestCatalog.QuestionCount,
            completed,
            preview,
            row?.CompletedAtUtc,
            row?.UpdatedAtUtc,
            CompetencyTestCatalog.Questions
                .Select(q => new CompetencyQuestionDto(q.Id, q.Category, q.Reverse, q.TextKey, q.IsRiasec))
                .ToList(),
            CompetencyTestCatalog.ParseTagsJson(row?.MatchTagsJson),
            DeepAnalysisService.FormatUpsellCopy(deepAnalysisPriceEuro, AssessmentKind.Competence));
    }
}
