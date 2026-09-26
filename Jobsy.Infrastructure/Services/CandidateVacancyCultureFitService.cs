using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateVacancyCultureFitService : ICandidateVacancyCultureFitService
{
    private readonly JobsyDbContext _db;
    private readonly ICultureFitAiService _ai;
    private readonly ICultureFitRefineQueue _queue;
    private readonly ILogger<CandidateVacancyCultureFitService> _logger;

    public CandidateVacancyCultureFitService(
        JobsyDbContext db,
        ICultureFitAiService ai,
        ICultureFitRefineQueue queue,
        ILogger<CandidateVacancyCultureFitService> logger)
    {
        _db = db;
        _ai = ai;
        _queue = queue;
        _logger = logger;
    }

    public async Task<(CultureFitResult? Result, string Status)> ResolveForGetAsync(
        Guid userId,
        Guid vacancyId,
        IReadOnlyList<string> culturePillars,
        CompetencyScores? competencies,
        CulturePersonalityScores? cultureScores,
        CultureFitResult? localResult,
        CancellationToken cancellationToken = default)
    {
        if (localResult is null)
        {
            return (null, InsightsStatuses.Ready);
        }

        var fingerprint = CultureFitFingerprint.For(culturePillars, competencies, cultureScores);
        var stored = await _db.CandidateVacancyCultureFits.AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.VacancyId == vacancyId, cancellationToken);

        if (stored is not null
            && string.Equals(stored.InputFingerprint, fingerprint, StringComparison.Ordinal)
            && CultureFitStoredJson.TryDeserialize(stored.ResultJson) is { } cached)
        {
            var retryFallback = CultureFitFingerprint.ShouldRetryFallback(
                stored.FromOpenAi,
                stored.ComputedAtUtc,
                DateTime.UtcNow);
            if (!retryFallback)
            {
                return (cached, InsightsStatuses.Ready);
            }

            _queue.TryEnqueue(userId, vacancyId);
            return (cached, InsightsStatuses.Updating);
        }

        // Missing or stale: return local immediately and refine in background.
        _queue.TryEnqueue(userId, vacancyId);
        return (localResult, InsightsStatuses.Updating);
    }

    public async Task RefineAsync(Guid userId, Guid vacancyId, CancellationToken cancellationToken = default)
    {
        var vacancy = await _db.Vacancies.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken);
        if (vacancy is null)
        {
            return;
        }

        var competencyRow = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var cultureRow = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        var competencies = CompetencyTestCatalog.CompletedScoresOrNull(
            competencyRow?.Status,
            competencyRow?.SamenwerkenPercent,
            competencyRow?.ResultaatgerichtheidPercent,
            competencyRow?.StressbestendigheidPercent,
            competencyRow?.InnovatiePercent,
            competencyRow?.ExtraversiePercent);
        if (competencies is not { IsComplete: true })
        {
            return;
        }

        CulturePersonalityScores? cultureScores = null;
        if (cultureRow is not null && CandidateCompetencyStatuses.IsCompleted(cultureRow.Status))
        {
            var c = new CulturePersonalityScores(
                cultureRow.AutonomyPercent,
                cultureRow.InformalPercent,
                cultureRow.CollaborationPercent,
                cultureRow.FlexibilityPercent,
                cultureRow.InnovationPercent,
                cultureRow.PeopleFirstPercent,
                cultureRow.OpennessPercent,
                cultureRow.ConscientiousnessPercent,
                cultureRow.ExtraversionPercent,
                cultureRow.AgreeablenessPercent,
                cultureRow.EmotionalStabilityPercent);
            if (c is { IsComplete: true })
            {
                cultureScores = c;
            }
        }

        var pillars = CulturePillarCatalog.Deserialize(vacancy.CulturePillarsJson);
        var local = CultureFitBuilder.Evaluate(pillars, competencies, cultureScores);
        if (local is null)
        {
            return;
        }

        var fingerprint = CultureFitFingerprint.For(pillars, competencies, cultureScores);
        var existing = await _db.CandidateVacancyCultureFits
            .FirstOrDefaultAsync(r => r.UserId == userId && r.VacancyId == vacancyId, cancellationToken);
        if (existing is not null
            && string.Equals(existing.InputFingerprint, fingerprint, StringComparison.Ordinal)
            && existing.FromOpenAi
            && CultureFitStoredJson.TryDeserialize(existing.ResultJson) is not null)
        {
            return;
        }

        CultureFitResult result = local;
        try
        {
            var labels = CulturePillarCatalog.Labels(pillars);
            var refined = await _ai.TryRefineAsync(local, competencies, labels, cultureScores, cancellationToken);
            if (refined is not null)
            {
                result = refined;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Culture-fit AI refine failed for {UserId}/{VacancyId}.", userId, vacancyId);
        }

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            existing = new CandidateVacancyCultureFit
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VacancyId = vacancyId
            };
            _db.CandidateVacancyCultureFits.Add(existing);
        }

        existing.ResultJson = CultureFitStoredJson.Serialize(result);
        existing.InputFingerprint = fingerprint;
        existing.FromOpenAi = result.FromOpenAi;
        existing.ComputedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task InvalidateForVacancyAsync(Guid vacancyId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.CandidateVacancyCultureFits
            .Where(r => r.VacancyId == vacancyId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        _db.CandidateVacancyCultureFits.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CultureFitResult?> GetStoredAsync(
        Guid userId,
        Guid vacancyId,
        CancellationToken cancellationToken = default)
    {
        var stored = await _db.CandidateVacancyCultureFits.AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.VacancyId == vacancyId, cancellationToken);
        return CultureFitStoredJson.TryDeserialize(stored?.ResultJson);
    }
}
