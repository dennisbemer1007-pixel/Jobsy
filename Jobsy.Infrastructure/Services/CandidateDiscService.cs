using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateDiscService : ICandidateDiscService
{
    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;

    public CandidateDiscService(JobsyDbContext db, IFlexCommercialService commercial)
    {
        _db = db;
        _commercial = commercial;
    }

    public async Task<CandidateDiscStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDiscProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
        return ToDto(row, price);
    }

    public async Task<CandidateDiscStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var error = DiscTestCatalog.ValidateAnswers(answers, complete);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        var row = await _db.CandidateDiscProfiles
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
            row = new CandidateDiscProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Draft,
                AnswersJson = "{}",
                CreatedAtUtc = now
            };
            _db.CandidateDiscProfiles.Add(row);
        }

        row.AnswersJson = DiscTestCatalog.SerializeAnswers(answers);
        row.UpdatedAtUtc = now;

        var preview = DiscTestCatalog.Score(answers);
        if (complete)
        {
            if (preview is not { IsComplete: true })
            {
                throw new InvalidOperationException("Beantwoord alle 25 vragen om de Quick-Scan af te ronden.");
            }

            row.Status = CandidateCompetencyStatuses.Completed;
            row.DominantPercent = preview.Dominant;
            row.InvloedPercent = preview.Invloed;
            row.StabielPercent = preview.Stabiel;
            row.NauwkeurigPercent = preview.Nauwkeurig;
            row.MatchTagsJson = DiscTestCatalog.SerializeTags(DiscTestCatalog.DeriveMatchTags(preview));
            row.CompletedAtUtc = now;
        }
        else
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.DominantPercent = null;
            row.InvloedPercent = null;
            row.StabielPercent = null;
            row.NauwkeurigPercent = null;
            row.MatchTagsJson = "[]";
            row.CompletedAtUtc = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
        return ToDto(row, price);
    }

    public async Task<DiscScores?> GetCompletedScoresAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDiscProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        return DiscTestCatalog.CompletedScoresOrNull(
            row.Status,
            row.DominantPercent,
            row.InvloedPercent,
            row.StabielPercent,
            row.NauwkeurigPercent);
    }

    private static CandidateDiscStateDto ToDto(CandidateDiscProfile? row, decimal deepAnalysisPriceEuro)
    {
        var answers = DiscTestCatalog.ParseAnswersJson(row?.AnswersJson);
        var preview = DiscTestCatalog.Score(answers);
        var completed = DiscTestCatalog.CompletedScoresOrNull(
            row?.Status,
            row?.DominantPercent,
            row?.InvloedPercent,
            row?.StabielPercent,
            row?.NauwkeurigPercent);
        return new CandidateDiscStateDto(
            row?.Status ?? CandidateCompetencyStatuses.Draft,
            answers,
            answers.Count,
            DiscTestCatalog.QuestionCount,
            completed,
            preview,
            row?.CompletedAtUtc,
            row?.UpdatedAtUtc,
            DiscTestCatalog.Questions
                .Select(q => new CompetencyQuestionDto(q.Id, q.Category, q.Reverse, q.TextKey))
                .ToList(),
            DiscTestCatalog.ParseTagsJson(row?.MatchTagsJson),
            DeepAnalysisService.FormatUpsellCopy(deepAnalysisPriceEuro, AssessmentKind.Disc));
    }
}
