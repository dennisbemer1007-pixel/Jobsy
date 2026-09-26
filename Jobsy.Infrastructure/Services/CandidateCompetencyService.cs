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

    public CandidateCompetencyService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICandidateMatchSnapshotService matchSnapshots,
        ICandidateInsightsQueue queue)
    {
        _db = db;
        _commercial = commercial;
        _matchSnapshots = matchSnapshots;
        _queue = queue;
    }

    public async Task<CandidateCompetencyStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
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

        row.AnswersJson = CompetencyTestCatalog.SerializeAnswers(answers);
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
        else
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
        if (complete)
        {
            _queue.TryEnqueue(userId);
        }

        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
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
