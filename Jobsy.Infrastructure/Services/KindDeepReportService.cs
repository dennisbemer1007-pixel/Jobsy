using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class KindDeepReportService : IKindDeepReportService
{
    private readonly JobsyDbContext _db;
    private readonly IAssessmentNormService _norms;
    private readonly ICareerCompassGenerationService _careerCompass;

    public KindDeepReportService(
        JobsyDbContext db,
        IAssessmentNormService norms,
        ICareerCompassGenerationService careerCompass)
    {
        _db = db;
        _norms = norms;
        _careerCompass = careerCompass;
    }

    public async Task<CareerDeepReport?> GetCareerAsync(Guid userId, CancellationToken ct = default)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == AssessmentKind.Career, ct);
        if (row is null || !CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        var report = CareerDeepReportJson.Deserialize(row.ReportJson);
        var stale = row.ReportVersion < CareerDeepReportJson.CurrentReportVersion;
        var missing = string.IsNullOrWhiteSpace(row.ReportJson) || report is null;
        if (!stale && !missing)
        {
            return report;
        }

        try
        {
            var rebuilt = await BuildCareerCoreAsync(row, tryAi: false, ct);
            await _db.SaveChangesAsync(ct);
            return rebuilt;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return report;
        }
    }

    /// <summary>
    /// Always stores a career deep report. Provider output is normalised first;
    /// an invalid compass falls back to the local catalogue.
    /// </summary>
    private async Task<CareerDeepReport> BuildCareerCoreAsync(
        CandidateDeepAnalysis row,
        bool tryAi,
        CancellationToken ct)
    {
        var answers = DeepAnalysisCatalog.ParseAnswersJson(row.AnswersJson, AssessmentKind.Career);
        var domainScores = DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career);
        var riasec = DeepAnalysisCatalog.ToRiasecScores(domainScores);
        var local = CareerCompassBuilder.Build(riasec, fromDeepAnalysis: true);
        var means = await _norms.GetMeansIfReadyAsync(AssessmentKind.Career, ct);
        var now = DateTime.UtcNow;

        var storedJson = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => c.UserId == row.UserId)
            .Select(c => c.CompassJson)
            .FirstOrDefaultAsync(ct);
        var stored = CareerCompassJson.TryDeserialize(storedJson);
        var compass = local;
        if (stored is { HasOccupations: true })
        {
            // One scored list for the compass and the deep report.
            compass = stored.Strengths.Count >= 3
                ? stored
                : stored with { Strengths = local.Strengths };
        }
        else if (tryAi)
        {
            var scoresKey = CareerCompassBuilder.ScoresKey(riasec);
            if (CareerCompassAttempt.TryBegin(row.UserId, storedJson, scoresKey, now))
            {
                try
                {
                    var generated = await _careerCompass.GenerateFromCareerDeepAsync(answers, ct);
                    if (generated.HasOccupations)
                    {
                        compass = generated.Strengths.Count >= 3
                            ? generated
                            : generated with { Strengths = local.Strengths };
                    }

                    compass = compass with { ScoresFingerprint = scoresKey, ModelAttemptUtc = now };
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _ = ex;
                    compass = local with { ScoresFingerprint = scoresKey, ModelAttemptUtc = now };
                }
                finally
                {
                    CareerCompassAttempt.End(row.UserId, scoresKey);
                }

                var careerRow = await _db.CandidateCareerInterests
                    .FirstOrDefaultAsync(c => c.UserId == row.UserId, ct);
                if (careerRow is not null)
                {
                    careerRow.CompassJson = CareerCompassJson.Serialize(compass);
                    careerRow.UpdatedAtUtc = now;
                }
            }
        }

        CareerDeepReport report;
        try
        {
            report = CareerDeepReportBuilder.Build(domainScores, compass, means, now);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
            report = CareerDeepReportBuilder.Build(domainScores, local, means, now);
        }

        row.ReportJson = CareerDeepReportJson.Serialize(report);
        row.ReportVersion = report.ReportVersion;
        row.ReportGeneratedAtUtc = now;
        row.UpdatedAtUtc = now;
        return report;
    }

    public async Task<CultureDeepReport?> GetCultureAsync(Guid userId, CancellationToken ct = default)
    {
        var row = await LoadCompletedAsync(userId, AssessmentKind.Culture, ct);
        return row is null ? null : CultureDeepReportJson.Deserialize(row.ReportJson);
    }

    public async Task<ValuesDeepReport?> GetValuesAsync(Guid userId, CancellationToken ct = default)
    {
        var row = await LoadCompletedAsync(userId, AssessmentKind.Values, ct);
        return row is null ? null : ValuesDeepReportJson.Deserialize(row.ReportJson);
    }

    public async Task<object?> GetStoredAsync(Guid userId, AssessmentKind kind, CancellationToken ct = default)
        => kind switch
        {
            AssessmentKind.Career => await GetCareerAsync(userId, ct),
            AssessmentKind.Culture => await GetCultureAsync(userId, ct),
            AssessmentKind.Values => await GetValuesAsync(userId, ct),
            _ => null
        };

    public async Task BuildAndStoreAsync(Guid userId, AssessmentKind kind, CancellationToken ct = default)
    {
        if (kind is AssessmentKind.Competence)
        {
            return;
        }

        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, ct)
            ?? throw new InvalidOperationException("Diepte-analyse is nog niet ontgrendeld.");

        if (!CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
        {
            throw new InvalidOperationException("Rond de uitgebreide test eerst af.");
        }

        var answers = DeepAnalysisCatalog.ParseAnswersJson(row.AnswersJson, kind);
        var domainScores = DeepAnalysisCatalog.ScoreDomains(answers, kind);
        var means = await _norms.GetMeansIfReadyAsync(kind, ct);
        var now = DateTime.UtcNow;

        switch (kind)
        {
            case AssessmentKind.Career:
                await BuildCareerCoreAsync(row, tryAi: true, ct);
                break;
            case AssessmentKind.Culture:
                {
                    var report = CultureDeepReportBuilder.Build(domainScores, means, now);
                    row.ReportJson = CultureDeepReportJson.Serialize(report);
                    row.ReportVersion = report.ReportVersion;
                    break;
                }
            case AssessmentKind.Values:
                {
                    var report = ValuesDeepReportBuilder.Build(domainScores, means, now);
                    row.ReportJson = ValuesDeepReportJson.Serialize(report);
                    row.ReportVersion = report.ReportVersion;
                    break;
                }
        }

        row.ReportGeneratedAtUtc = now;
        row.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CandidateDeepAnalysis?> LoadCompletedAsync(
        Guid userId, AssessmentKind kind, CancellationToken ct)
    {
        var row = await _db.CandidateDeepAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, ct);
        if (row is null || !CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        return row;
    }
}
