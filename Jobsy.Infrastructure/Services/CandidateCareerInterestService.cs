using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateCareerInterestService : ICandidateCareerInterestService
{
    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;
    private readonly ICandidateCompetencyService _competencies;

    public CandidateCareerInterestService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICandidateCompetencyService competencies)
    {
        _db = db;
        _commercial = commercial;
        _competencies = competencies;
    }

    public async Task<CandidateCareerInterestStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
        var matches = await _competencies.GetTopMatchesAsync(userId, cancellationToken);
        return ToDto(row, price, matches);
    }

    public async Task<CandidateCareerInterestStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var error = CareerTestCatalog.ValidateAnswers(answers, complete);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        var row = await _db.CandidateCareerInterests
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (answers.Count == 0)
        {
            if (row is not null)
            {
                throw new InvalidOperationException(
                    "Lege antwoorden overschrijven je bestaande test niet. Stuur de huidige antwoorden mee.");
            }

            return ToDto(null, FlexCommercialSettings.DefaultDeepAnalysisPriceEuro, []);
        }

        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateCareerInterest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Draft,
                AnswersJson = "{}",
                CreatedAtUtc = now
            };
            _db.CandidateCareerInterests.Add(row);
        }

        row.AnswersJson = CareerTestCatalog.SerializeAnswers(answers);
        row.UpdatedAtUtc = now;

        var preview = CareerTestCatalog.Score(answers);
        if (complete)
        {
            if (preview is not { IsComplete: true })
            {
                throw new InvalidOperationException("Beantwoord alle 25 vragen om de beroepentest af te ronden.");
            }

            var tags = CareerTestCatalog.DeriveRiasecTags(preview);
            row.Status = CandidateCompetencyStatuses.Completed;
            row.RealisticPercent = preview.Realistic;
            row.InvestigativePercent = preview.Investigative;
            row.ArtisticPercent = preview.Artistic;
            row.SocialPercent = preview.Social;
            row.EnterprisingPercent = preview.Enterprising;
            row.ConventionalPercent = preview.Conventional;
            row.HollandCode = CareerTestCatalog.HollandCode(preview);
            row.RiasecTagsJson = CareerTestCatalog.SerializeTags(tags);
            row.MatchTagsJson = CareerTestCatalog.SerializeTags(tags);
            row.CompletedAtUtc = now;
        }
        else
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.RealisticPercent = null;
            row.InvestigativePercent = null;
            row.ArtisticPercent = null;
            row.SocialPercent = null;
            row.EnterprisingPercent = null;
            row.ConventionalPercent = null;
            row.HollandCode = "";
            row.RiasecTagsJson = "[]";
            row.MatchTagsJson = "[]";
            row.CompletedAtUtc = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
        var matches = await _competencies.GetTopMatchesAsync(userId, cancellationToken);
        return ToDto(row, price, matches);
    }

    public async Task<RiasecScores?> GetCompletedScoresAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        return CareerTestCatalog.CompletedScoresOrNull(
            row.Status,
            row.RealisticPercent,
            row.InvestigativePercent,
            row.ArtisticPercent,
            row.SocialPercent,
            row.EnterprisingPercent,
            row.ConventionalPercent);
    }

    public async Task<IReadOnlyList<string>> GetCompletedTagsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null || !CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return [];
        }

        return CareerTestCatalog.ParseTagsJson(row.RiasecTagsJson);
    }

    private static CandidateCareerInterestStateDto ToDto(
        CandidateCareerInterest? row,
        decimal deepAnalysisPriceEuro,
        IReadOnlyList<CandidateMatchedVacancyDto> matches)
    {
        var answers = CareerTestCatalog.ParseAnswersJson(row?.AnswersJson);
        var preview = CareerTestCatalog.Score(answers);
        var completed = CareerTestCatalog.CompletedScoresOrNull(
            row?.Status,
            row?.RealisticPercent,
            row?.InvestigativePercent,
            row?.ArtisticPercent,
            row?.SocialPercent,
            row?.EnterprisingPercent,
            row?.ConventionalPercent);
        return new CandidateCareerInterestStateDto(
            row?.Status ?? CandidateCompetencyStatuses.Draft,
            answers,
            answers.Count,
            CareerTestCatalog.QuestionCount,
            completed,
            preview,
            row?.HollandCode ?? "",
            row?.CompletedAtUtc,
            row?.UpdatedAtUtc,
            CareerTestCatalog.Questions
                .Select(q => new CareerQuestionDto(q.Id, q.Category, q.Reverse, q.TextKey))
                .ToList(),
            CareerTestCatalog.ParseTagsJson(row?.RiasecTagsJson),
            CareerTestCatalog.ParseTagsJson(row?.MatchTagsJson),
            DeepAnalysisService.FormatUpsellCopy(deepAnalysisPriceEuro, AssessmentKind.Career),
            matches);
    }
}
