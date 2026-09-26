using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Builds, persists, and refreshes the paid competence deep-analysis report for one candidate.
/// The template (no-AI) report is always built first so <see cref="BuildAndStoreAsync"/> never
/// leaves a candidate without a report even when OpenAI is unavailable.
/// </summary>
public sealed class CompetenceDeepReportService : ICompetenceDeepReportService
{
    private static readonly JsonSerializerOptions PreferencesJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly JobsyDbContext _db;
    private readonly INormProvider _norms;
    private readonly ICompetenceDeepReportAiService _ai;
    private readonly ICandidateInsightsQueue _queue;
    private readonly ILogger<CompetenceDeepReportService> _logger;

    public CompetenceDeepReportService(
        JobsyDbContext db,
        INormProvider norms,
        ICompetenceDeepReportAiService ai,
        ICandidateInsightsQueue queue,
        ILogger<CompetenceDeepReportService> logger)
    {
        _db = db;
        _norms = norms;
        _ai = ai;
        _queue = queue;
        _logger = logger;
    }

    public async Task<CompetenceDeepReport?> GetStoredAsync(Guid userId, CancellationToken ct)
    {
        var row = await _db.CandidateDeepAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.UserId == userId && d.Kind == AssessmentKind.Competence,
                ct);
        if (row is null || !CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        var report = CompetenceDeepReportJson.Deserialize(row.ReportJson);
        if (report is null)
        {
            return null;
        }

        if (row.ReportVersion < CompetenceDeepReportJson.CurrentReportVersion)
        {
            // Old report shape is still usable to render now; rebuild happens off the request path.
            _queue.TryEnqueue(userId);
        }

        return report;
    }

    public async Task<CompetenceDeepReport> BuildAndStoreAsync(Guid userId, bool tryAi, CancellationToken ct)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(
                d => d.UserId == userId && d.Kind == AssessmentKind.Competence,
                ct)
            ?? throw new InvalidOperationException(
                "Competentie diepte-analyse is nog niet ontgrendeld. Betaal eerst via de checkout.");

        var answers = DeepAnalysisCatalog.ParseAnswersJson(row.AnswersJson, AssessmentKind.Competence);
        var jobTitle = await TryLoadJobTitleAsync(userId, ct);
        var occupations = await TryLoadTopOccupationsAsync(userId, ct);
        var now = DateTime.UtcNow;

        string? aiSummary = null;
        IReadOnlyList<(string Title, string Body)>? aiPlan = null;
        var fromOpenAi = false;

        if (tryAi)
        {
            try
            {
                var draft = CompetenceDeepReportBuilder.Build(
                    answers, _norms, jobTitle, occupations, null, null, false, now);
                var generated = await _ai.TryGenerateAsync(draft, jobTitle, ct);
                if (generated is not null)
                {
                    aiSummary = generated.Value.Summary;
                    aiPlan = generated.Value.Steps;
                    fromOpenAi = true;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Competence deep-report AI generation failed for {UserId}.", userId);
            }
        }

        var report = CompetenceDeepReportBuilder.Build(
            answers, _norms, jobTitle, occupations, aiSummary, aiPlan, fromOpenAi, now);

        row.ReportJson = CompetenceDeepReportJson.Serialize(report);
        row.ReportVersion = CompetenceDeepReportJson.CurrentReportVersion;
        row.ReportGeneratedAtUtc = now;
        await _db.SaveChangesAsync(ct);

        return report;
    }

    public async Task RefineAiAsync(Guid userId, CancellationToken ct)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(
                d => d.UserId == userId && d.Kind == AssessmentKind.Competence,
                ct);
        if (row is null || !CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
        {
            return;
        }

        var report = CompetenceDeepReportJson.Deserialize(row.ReportJson);
        if (report is null)
        {
            // No usable stored report yet (e.g. legacy row) — build one, with AI, right away.
            await BuildAndStoreAsync(userId, tryAi: true, ct);
            return;
        }

        if (report.FromOpenAi)
        {
            return;
        }

        var generatedAt = row.ReportGeneratedAtUtc ?? report.GeneratedAtUtc;
        if (DateTime.UtcNow - generatedAt < TimeSpan.FromHours(1))
        {
            return;
        }

        try
        {
            await BuildAndStoreAsync(userId, tryAi: true, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Competence deep-report AI refine failed for {UserId}.", userId);
        }
    }

    private async Task<string?> TryLoadJobTitleAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var preferencesJson = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.PreferencesJson)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(preferencesJson))
            {
                return null;
            }

            var prefs = JsonSerializer.Deserialize<CandidatePreferencesDto>(preferencesJson, PreferencesJsonOptions);
            return prefs?.Roles?.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r))?.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<(string Title, int MatchPercent, string Reason)>?> TryLoadTopOccupationsAsync(
        Guid userId, CancellationToken ct)
    {
        var compassJson = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.CompassJson)
            .FirstOrDefaultAsync(ct);
        var compass = CareerCompassJson.TryDeserialize(compassJson);
        if (compass is not { HasOccupations: true })
        {
            return null;
        }

        var top = compass.AllOccupations
            .OrderByDescending(o => o.Percent)
            .Take(3)
            .Select(o => (o.Title, o.Percent, o.Why))
            .ToList();
        return top.Count > 0 ? top : null;
    }
}
