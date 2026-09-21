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
    private readonly IVacancyDiscoveryIndex _discovery;
    private readonly IProfileVacancyMatchService _matches;
    private readonly IFlexCommercialService _commercial;

    public CandidateCompetencyService(
        JobsyDbContext db,
        IVacancyDiscoveryIndex discovery,
        IProfileVacancyMatchService matches,
        IFlexCommercialService commercial)
    {
        _db = db;
        _discovery = discovery;
        _matches = matches;
        _commercial = commercial;
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
        var context = await _matches.TryLoadForUserIdAsync(userId, cancellationToken);
        if (context is null)
        {
            return [];
        }

        var vacancies = await _discovery.GetActiveAsync(cancellationToken);
        var transport = TransportLabels.Parse(context.Prefs.PreferredTransport);
        var scored = _matches.Score(
            context,
            vacancies.Select(vacancy =>
            {
                int? travelMinutes = null;
                if (context.HomeLatitude is double lat && context.HomeLongitude is double lng)
                {
                    var estimate = TravelReach.Estimate(
                        lat,
                        lng,
                        vacancy.Latitude,
                        vacancy.Longitude,
                        transport);
                    travelMinutes = estimate.TravelMinutes;
                }

                return (vacancy, travelMinutes);
            }));

        var ranked = ProfileVacancyMatchCalculator.RankScored(scored.Values);
        var byId = vacancies.ToDictionary(v => v.Id);
        var result = new List<CandidateMatchedVacancyDto>(ranked.Count);
        foreach (var match in ranked)
        {
            if (!byId.TryGetValue(match.VacancyId, out var vacancy))
            {
                continue;
            }

            result.Add(new CandidateMatchedVacancyDto(
                vacancy.Id,
                vacancy.Title,
                vacancy.CompanyName,
                vacancy.ImageUrl,
                vacancy.CompanyLogoUrl,
                match.TotalPercent,
                match.ColorBand,
                PreferWhy(match),
                match.Gaps.Select(g => g.Text).ToList(),
                match.IsBroadMatch,
                match.MatchRationale));
        }

        return result;
    }

    private static IReadOnlyList<string> PreferWhy(ProfileVacancyMatch match)
    {
        var lines = match.Why.Select(w => w.Text).ToList();
        if (match.IsBroadMatch
            && !string.IsNullOrWhiteSpace(match.MatchRationale)
            && !lines.Contains(match.MatchRationale, StringComparer.Ordinal))
        {
            lines.Insert(0, match.MatchRationale);
        }

        return lines;
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
