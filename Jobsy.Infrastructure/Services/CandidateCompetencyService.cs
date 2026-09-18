using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateCompetencyService : ICandidateCompetencyService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JobsyDbContext _db;
    private readonly IVacancyDiscoveryIndex _discovery;
    private readonly IRoutingService _routing;

    public CandidateCompetencyService(
        JobsyDbContext db,
        IVacancyDiscoveryIndex discovery,
        IRoutingService routing)
    {
        _db = db;
        _discovery = discovery;
        _routing = routing;
    }

    public async Task<CandidateCompetencyStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        return ToDto(row);
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

            return ToDto(null);
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
                throw new InvalidOperationException("Beantwoord alle 20 vragen om de test af te ronden.");
            }

            row.Status = CandidateCompetencyStatuses.Completed;
            row.SamenwerkenPercent = preview.Samenwerken;
            row.ResultaatgerichtheidPercent = preview.Resultaatgerichtheid;
            row.StressbestendigheidPercent = preview.Stressbestendigheid;
            row.InnovatiePercent = preview.Innovatie;
            row.CompletedAtUtc = now;
        }
        else
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.SamenwerkenPercent = null;
            row.ResultaatgerichtheidPercent = null;
            row.StressbestendigheidPercent = null;
            row.InnovatiePercent = null;
            row.CompletedAtUtc = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(row);
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
            row.InnovatiePercent);
    }

    public async Task<IReadOnlyList<CandidateMatchedVacancyDto>> GetTopMatchesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (user is null)
        {
            return [];
        }

        var prefs = DeserializePrefs(user.PreferencesJson);
        var scores = await GetCompletedScoresAsync(userId, cancellationToken);
        var vacancies = await _discovery.GetActiveAsync(cancellationToken);
        var ageYears = AgeRules.AgeYearsFromDateOfBirth(user.DateOfBirth) ?? prefs.AgeYears;
        var transport = TransportLabels.Parse(prefs.PreferredTransport);

        var inputs = new List<ProfileVacancyMatchInput>(vacancies.Count);
        foreach (var vacancy in vacancies)
        {
            int? travelMinutes = null;
            if (user.HomeLocation is not null)
            {
                var route = await _routing.GetRouteAsync(
                    user.HomeLocation.Latitude,
                    user.HomeLocation.Longitude,
                    vacancy.Latitude,
                    vacancy.Longitude,
                    transport,
                    cancellationToken);
                travelMinutes = (int)Math.Round(route.DurationSeconds / 60.0, MidpointRounding.AwayFromZero);
            }

            var core = MatchingProfileMapper.BuildInput(vacancy, prefs, travelMinutes, ageYears);
            inputs.Add(new ProfileVacancyMatchInput
            {
                VacancyId = vacancy.Id,
                Core = core,
                VacancyTitle = vacancy.Title,
                VacancyDescription = vacancy.Description,
                WorkTypes = vacancy.WorkTypeLabelList,
                CandidateRoles = prefs.Roles,
                CandidateLicenses = prefs.DrivingLicenses,
                CandidateEducations = prefs.Educations,
                CandidateEmployerCount = prefs.Employers?.Count ?? 0,
                RequiredDrivingLicense = vacancy.RequiredDrivingLicense,
                RequiredEducation = vacancy.RequiredEducation,
                MinimumEmployers = vacancy.MinimumEmployers,
                CandidateCompetencies = scores,
                VacancyCompetencies = VacancyCompetencyProfile.Infer(vacancy)
            });
        }

        var ranked = ProfileVacancyMatchCalculator.Rank(inputs);
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
                match.Why.Select(w => w.Text).ToList(),
                match.Gaps.Select(g => g.Text).ToList()));
        }

        return result;
    }

    private static CandidateCompetencyStateDto ToDto(CandidateCompetency? row)
    {
        var answers = CompetencyTestCatalog.ParseAnswersJson(row?.AnswersJson);
        var preview = CompetencyTestCatalog.Score(answers);
        var completed = CompetencyTestCatalog.CompletedScoresOrNull(
            row?.Status,
            row?.SamenwerkenPercent,
            row?.ResultaatgerichtheidPercent,
            row?.StressbestendigheidPercent,
            row?.InnovatiePercent);
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
                .Select(q => new CompetencyQuestionDto(q.Id, q.Category, q.Reverse, q.TextKey))
                .ToList());
    }

    private static CandidatePreferencesDto DeserializePrefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CandidatePreferencesDto([], null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, JsonOptions)
                   ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            return new CandidatePreferencesDto([], null, null);
        }
    }
}
