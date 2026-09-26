using Jobsy.Core.Entities;
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

    public CandidateValuesService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICandidateInsightsQueue queue)
    {
        _db = db;
        _commercial = commercial;
        _queue = queue;
    }

    public async Task<CandidateValuesStateDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var price = (await _commercial.GetAsync(cancellationToken)).DeepAnalysisPriceEuro;
        return ToDto(row, price);
    }

    public async Task<CandidateValuesStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var error = SchwartzValuesCatalog.ValidateAnswers(answers, complete);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        var row = await _db.CandidateValuesProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
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
            _db.CandidateValuesProfiles.Add(row);
        }

        row.AnswersJson = SchwartzValuesCatalog.SerializeAnswers(answers);
        row.UpdatedAtUtc = now;

        var preview = SchwartzValuesCatalog.Score(answers);
        if (complete)
        {
            if (preview is not { IsComplete: true })
            {
                throw new InvalidOperationException(
                    "Beantwoord alle 25 stellingen om de waardenscan af te ronden.");
            }

            ApplyScores(row, preview);
            row.Status = CandidateCompetencyStatuses.Completed;
            row.MatchTagsJson = SchwartzValuesCatalog.SerializeTags(
                SchwartzValuesCatalog.DeriveMatchTags(preview));
            row.CompletedAtUtc = now;
        }
        else
        {
            ClearScores(row);
            row.Status = CandidateCompetencyStatuses.Draft;
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
                price);
        }

        return new CandidateValuesStateDto(
            row.Status,
            SchwartzValuesCatalog.ParseAnswers(row.AnswersJson),
            FromRow(row),
            SchwartzValuesCatalog.ParseTags(row.MatchTagsJson),
            row.CompletedAtUtc,
            price);
    }
}
