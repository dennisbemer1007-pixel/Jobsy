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
        var row = await LoadCompletedAsync(userId, AssessmentKind.Career, ct);
        return row is null ? null : CareerDeepReportJson.Deserialize(row.ReportJson);
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
            {
                var compass = await _careerCompass.GenerateFromCareerDeepAsync(answers, ct);
                var report = CareerDeepReportBuilder.Build(domainScores, compass, means, now);
                row.ReportJson = CareerDeepReportJson.Serialize(report);
                row.ReportVersion = report.ReportVersion;
                break;
            }
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
