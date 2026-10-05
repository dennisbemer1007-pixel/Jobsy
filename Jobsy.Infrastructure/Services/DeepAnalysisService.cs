using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class DeepAnalysisService : IDeepAnalysisService
{
    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;
    private readonly ICareerCompassGenerationService _careerCompass;
    private readonly ICompetenceDeepReportService _competenceReport;
    private readonly IKindDeepReportService? _kindReports;
    private readonly ICandidateInsightsQueue _queue;
    private readonly ILogger<DeepAnalysisService> _logger;
    private readonly AssessmentSaveGuard _saveGuard;

    public DeepAnalysisService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICareerCompassGenerationService careerCompass,
        ICompetenceDeepReportService competenceReport,
        ILogger<DeepAnalysisService> logger,
        AssessmentSaveGuard saveGuard)
        : this(db, commercial, careerCompass, competenceReport, new CandidateInsightsQueue(), logger, null, saveGuard)
    {
    }

    public DeepAnalysisService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICareerCompassGenerationService careerCompass,
        ICompetenceDeepReportService competenceReport,
        ICandidateInsightsQueue queue,
        ILogger<DeepAnalysisService> logger,
        IKindDeepReportService? kindReports,
        AssessmentSaveGuard saveGuard)
    {
        _db = db;
        _commercial = commercial;
        _careerCompass = careerCompass;
        _competenceReport = competenceReport;
        _kindReports = kindReports;
        _queue = queue;
        _logger = logger;
        _saveGuard = saveGuard;
    }

    /// <summary>Localization key for deep upsell copy (Web formats with question count).</summary>
    public static string FormatUpsellCopy(decimal priceEuro, AssessmentKind kind = AssessmentKind.Competence)
    {
        _ = priceEuro;
        _ = kind;
        return "DeepPay.Upsell";
    }

    public async Task<DeepAnalysisStateDto> GetStateAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default)
    {
        var map = await GetStatesAsync(userId, [kind], cancellationToken);
        return map[kind];
    }

    public async Task<IReadOnlyDictionary<AssessmentKind, DeepAnalysisStateDto>> GetStatesAsync(
        Guid userId,
        IReadOnlyList<AssessmentKind> kinds,
        CancellationToken cancellationToken = default)
    {
        if (kinds.Count == 0)
        {
            return new Dictionary<AssessmentKind, DeepAnalysisStateDto>();
        }

        var distinct = kinds.Distinct().ToList();
        var rows = await _db.CandidateDeepAnalyses.AsNoTracking()
            .Where(d => d.UserId == userId && distinct.Contains(d.Kind))
            .ToListAsync(cancellationToken);
        var commercial = await _commercial.GetAsync(cancellationToken);
        var result = new Dictionary<AssessmentKind, DeepAnalysisStateDto>(distinct.Count);
        foreach (var kind in distinct)
        {
            var row = rows.FirstOrDefault(r => r.Kind == kind);
            CompetenceDeepReport? competenceReport = null;
            CareerDeepReport? careerReport = null;
            if (kind == AssessmentKind.Competence)
            {
                competenceReport = await LoadOrBuildCompetenceReportAsync(userId, kind, row, cancellationToken);
            }
            else if (kind == AssessmentKind.Career && _kindReports is not null)
            {
                careerReport = await LoadOrBuildCareerReportAsync(userId, row, cancellationToken);
            }

            result[kind] = ToDto(
                kind,
                row,
                DeepAnalysisPricing.For(commercial, kind),
                competenceReport,
                careerReport);
        }

        return result;
    }

    /// <summary>
    /// Reads the stored competence report, or builds it template-only (no AI, so GET never
    /// waits on OpenAI) when the deep analysis is completed but no report has been built yet.
    /// </summary>
    private async Task<CompetenceDeepReport?> LoadOrBuildCompetenceReportAsync(
        Guid userId,
        AssessmentKind kind,
        CandidateDeepAnalysis? row,
        CancellationToken cancellationToken)
    {
        if (kind != AssessmentKind.Competence
            || row is null
            || !CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        var stored = await _competenceReport.GetStoredAsync(userId, cancellationToken);
        if (stored is not null)
        {
            return stored;
        }

        try
        {
            return await _competenceReport.BuildAndStoreAsync(userId, tryAi: false, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Competence deep-report build-on-read failed for {UserId}.", userId);
            return null;
        }
    }

    /// <summary>
    /// Reads the stored career report, or rebuilds it without waiting on the model
    /// when the deep analysis is completed but the report is missing or an old version.
    /// </summary>
    private async Task<CareerDeepReport?> LoadOrBuildCareerReportAsync(
        Guid userId,
        CandidateDeepAnalysis? row,
        CancellationToken cancellationToken)
    {
        if (row is null
            || !CandidateDeepAnalysisStatuses.IsCompleted(row.Status)
            || _kindReports is null)
        {
            return null;
        }

        try
        {
            return await _kindReports.GetCareerAsync(userId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Career deep-report build-on-read failed for {UserId}.", userId);
            return CareerDeepReportJson.Deserialize(row.ReportJson);
        }
    }

    public async Task UnlockForUserAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, cancellationToken);
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateDeepAnalysis
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = kind,
                Status = CandidateDeepAnalysisStatuses.Draft,
                AnswersJson = "{}",
                TagsJson = "[]",
                UnlockedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.CandidateDeepAnalyses.Add(row);
        }
        else if (!CandidateDeepAnalysisStatuses.IsUnlocked(row.Status))
        {
            row.Status = CandidateDeepAnalysisStatuses.Draft;
            row.UnlockedAtUtc = now;
            row.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeepAnalysisStateDto> SaveAsync(
        Guid userId,
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, cancellationToken)
            ?? throw new InvalidOperationException("Diepte-analyse is nog niet ontgrendeld. Betaal eerst via de checkout.");

        if (!CandidateDeepAnalysisStatuses.IsUnlocked(row.Status))
        {
            throw new InvalidOperationException("Diepte-analyse is nog niet ontgrendeld. Betaal eerst via de checkout.");
        }

        if (answers.Count == 0)
        {
            var existingAnswers = DeepAnalysisCatalog.ParseAnswersJson(row.AnswersJson, kind);
            if (existingAnswers.Count > 0)
            {
                throw new InvalidOperationException(
                    "Lege antwoorden overschrijven je bestaande diepte-analyse niet. Stuur de huidige antwoorden mee.");
            }

            var commercialEmpty = await _commercial.GetAsync(cancellationToken);
            return ToDto(kind, row, DeepAnalysisPricing.For(commercialEmpty, kind));
        }

        var error = DeepAnalysisCatalog.ValidateAnswers(answers, complete, kind);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        var now = DateTime.UtcNow;
        var wasCompleted = CandidateDeepAnalysisStatuses.IsCompleted(row.Status);
        var answersJson = DeepAnalysisCatalog.SerializeAnswers(answers, kind);
        var baselineAnswersJson = row.AnswersJson;
        if (wasCompleted && !complete)
        {
            await _saveGuard.SaveDraftAsync(userId, kind, AssessmentVariant.Deep, answersJson, baselineAnswersJson, cancellationToken);
        }
        else if (wasCompleted && complete)
        {
            var noOp = await _saveGuard.CommitCompleteAsync(
                userId, kind, AssessmentVariant.Deep, answersJson, null, baselineAnswersJson, baselineAnswersJson, cancellationToken);
            if (noOp)
            {
                var commercialNoOp = await _commercial.GetAsync(cancellationToken);
                return ToDto(kind, row, DeepAnalysisPricing.For(commercialNoOp, kind));
            }
        }
        row.AnswersJson = answersJson;
        row.UpdatedAtUtc = now;

        if (complete)
        {
            var tags = DeepAnalysisCatalog.DeriveEnrichedTags(answers, kind);
            row.Status = CandidateDeepAnalysisStatuses.Completed;
            row.TagsJson = CompetencyTestCatalog.SerializeTags(tags);
            row.CompletedAtUtc = now;
            row.ReportGeneratedAtUtc = now;
            await MergeTagsIntoQuickScanAsync(userId, kind, answers, tags, now, cancellationToken);
        }
        else if (!CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
        {
            row.Status = CandidateDeepAnalysisStatuses.Draft;
            row.CompletedAtUtc = null;
            row.ReportGeneratedAtUtc = null;
            row.TagsJson = "[]";
        }
        // Completed + autosave: keep status/tags/report timestamps; AnswersJson above is the
        // pending edit until the candidate finishes again with complete:true (rescore).

        await _db.SaveChangesAsync(cancellationToken);
        CompetenceDeepReport? competenceReport = null;
        if (complete)
        {
            _queue.TryEnqueue(userId);

            if (kind == AssessmentKind.Competence)
            {
                try
                {
                    competenceReport = await _competenceReport.BuildAndStoreAsync(userId, tryAi: true, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Competence deep-report build-on-complete failed for {UserId}.", userId);
                }
            }
            else if (_kindReports is not null)
            {
                try
                {
                    await _kindReports.BuildAndStoreAsync(userId, kind, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "{Kind} deep-report build-on-complete failed for {UserId}.", kind, userId);
                }
            }
        }

        var commercial = await _commercial.GetAsync(cancellationToken);
        return ToDto(kind, row, DeepAnalysisPricing.For(commercial, kind), competenceReport);
    }

    private async Task MergeTagsIntoQuickScanAsync(
        Guid userId,
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyList<string> tags,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (kind == AssessmentKind.Career)
        {
            var career = await _db.CandidateCareerInterests
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            var riasec = DeepAnalysisCatalog.ToRiasecScores(
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career));
            var compassTags = CareerTestCatalog.DeriveRiasecTags(riasec);

            if (career is null)
            {
                career = new CandidateCareerInterest
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Status = CandidateCompetencyStatuses.Completed,
                    AnswersJson = "{}",
                    CreatedAtUtc = now
                };
                _db.CandidateCareerInterests.Add(career);
            }

            career.Status = CandidateCompetencyStatuses.Completed;
            career.RealisticPercent = riasec.Realistic;
            career.InvestigativePercent = riasec.Investigative;
            career.ArtisticPercent = riasec.Artistic;
            career.SocialPercent = riasec.Social;
            career.EnterprisingPercent = riasec.Enterprising;
            career.ConventionalPercent = riasec.Conventional;
            career.HollandCode = CareerTestCatalog.HollandCode(riasec);
            career.RiasecTagsJson = CareerTestCatalog.SerializeTags(compassTags);
            var existing = CareerTestCatalog.ParseTagsJson(career.MatchTagsJson).ToList();
            foreach (var tag in tags.Concat(compassTags))
            {
                if (!existing.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    existing.Add(tag);
                }
            }

            career.MatchTagsJson = CareerTestCatalog.SerializeTags(existing);
            var compass = await _careerCompass.GenerateFromCareerDeepAsync(answers, cancellationToken);
            compass = compass with { ScoresFingerprint = CareerCompassBuilder.ScoresKey(riasec) };
            career.CompassJson = CareerCompassJson.Serialize(compass);
            career.CompletedAtUtc ??= now;
            career.UpdatedAtUtc = now;
            return;
        }

        if (kind == AssessmentKind.Culture)
        {
            var culture = await _db.CandidateCulturePersonalityProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            var scores = DeepAnalysisCatalog.ToCulturePersonalityScores(
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Culture));
            var cultureTags = CulturePersonalityCatalog.DeriveMatchTags(scores);

            if (culture is null)
            {
                culture = new CandidateCulturePersonalityProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Status = CandidateCompetencyStatuses.Completed,
                    AnswersJson = "{}",
                    CreatedAtUtc = now
                };
                _db.CandidateCulturePersonalityProfiles.Add(culture);
            }

            culture.Status = CandidateCompetencyStatuses.Completed;
            culture.AutonomyPercent = scores.Autonomy;
            culture.InformalPercent = scores.Informal;
            culture.CollaborationPercent = scores.Collaboration;
            culture.FlexibilityPercent = scores.Flexibility;
            culture.InnovationPercent = scores.Innovation;
            culture.PeopleFirstPercent = scores.PeopleFirst;
            culture.OpennessPercent = scores.Openness;
            culture.ConscientiousnessPercent = scores.Conscientiousness;
            culture.ExtraversionPercent = scores.Extraversion;
            culture.AgreeablenessPercent = scores.Agreeableness;
            culture.EmotionalStabilityPercent = scores.EmotionalStability;
            var existing = CulturePersonalityCatalog.ParseTags(culture.MatchTagsJson).ToList();
            foreach (var tag in tags.Concat(cultureTags))
            {
                if (!existing.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    existing.Add(tag);
                }
            }

            culture.MatchTagsJson = CulturePersonalityCatalog.SerializeTags(existing);
            culture.CompletedAtUtc ??= now;
            culture.UpdatedAtUtc = now;
            return;
        }

        if (kind == AssessmentKind.Values)
        {
            var values = await _db.CandidateValuesProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            var schwartz = DeepAnalysisCatalog.ToSchwartzScores(
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Values));
            var valueTags = SchwartzValuesCatalog.DeriveMatchTags(schwartz);

            if (values is null)
            {
                values = new CandidateValuesProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Status = CandidateCompetencyStatuses.Completed,
                    AnswersJson = "{}",
                    CreatedAtUtc = now
                };
                _db.CandidateValuesProfiles.Add(values);
            }

            values.Status = CandidateCompetencyStatuses.Completed;
            values.AutonomyPercent = schwartz.Autonomy;
            values.ConnectionPercent = schwartz.Connection;
            values.AchievementPercent = schwartz.Achievement;
            values.StabilityPercent = schwartz.Stability;
            values.ImpactPercent = schwartz.Impact;
            var existing = SchwartzValuesCatalog.ParseTags(values.MatchTagsJson).ToList();
            foreach (var tag in tags.Concat(valueTags))
            {
                if (!existing.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    existing.Add(tag);
                }
            }

            values.MatchTagsJson = SchwartzValuesCatalog.SerializeTags(existing);
            values.CompletedAtUtc ??= now;
            values.UpdatedAtUtc = now;
            return;
        }

        var quick = await _db.CandidateCompetencies
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (quick is null || !CandidateCompetencyStatuses.IsCompleted(quick.Status))
        {
            return;
        }

        var competenceTags = CompetencyTestCatalog.ParseTagsJson(quick.MatchTagsJson).ToList();
        foreach (var tag in tags)
        {
            if (!competenceTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                competenceTags.Add(tag);
            }
        }

        quick.MatchTagsJson = CompetencyTestCatalog.SerializeTags(competenceTags);
        quick.UpdatedAtUtc = now;
    }

    private static DeepAnalysisStateDto ToDto(
        AssessmentKind kind,
        CandidateDeepAnalysis? row,
        decimal priceEuro,
        CompetenceDeepReport? competenceReport = null,
        CareerDeepReport? careerReport = null,
        CultureDeepReport? cultureReport = null,
        ValuesDeepReport? valuesReport = null)
    {
        var status = row?.Status ?? CandidateDeepAnalysisStatuses.Locked;
        var answers = DeepAnalysisCatalog.ParseAnswersJson(row?.AnswersJson, kind);
        var unlocked = CandidateDeepAnalysisStatuses.IsUnlocked(status);
        var expected = DeepAnalysisCatalog.QuestionCountFor(kind);
        // Status alone is not enough: older/stub rows could be marked Completed without a full answer set.
        var completed = CandidateDeepAnalysisStatuses.IsCompleted(status)
                        && answers.Count >= expected;

        if (completed && row is not null)
        {
            competenceReport ??= kind == AssessmentKind.Competence
                ? CompetenceDeepReportJson.Deserialize(row.ReportJson)
                : null;
            careerReport ??= kind == AssessmentKind.Career
                ? CareerDeepReportJson.Deserialize(row.ReportJson)
                : null;
            cultureReport ??= kind == AssessmentKind.Culture
                ? CultureDeepReportJson.Deserialize(row.ReportJson)
                : null;
            valuesReport ??= kind == AssessmentKind.Values
                ? ValuesDeepReportJson.Deserialize(row.ReportJson)
                : null;
        }

        return new DeepAnalysisStateDto(
            kind,
            status,
            unlocked,
            completed,
            answers.Count,
            expected,
            priceEuro,
            CompetencyTestCatalog.ParseTagsJson(row?.TagsJson),
            row?.UnlockedAtUtc,
            row?.CompletedAtUtc,
            row?.ReportGeneratedAtUtc,
            answers,
            unlocked
                ? DeepAnalysisCatalog.QuestionsFor(kind)
                    .Select(q => new DeepAnalysisQuestionDto(
                        q.Id,
                        q.Family,
                        q.Domain,
                        q.Reverse,
                        q.PromptNl,
                        DeepAnalysisQuestionHelp.ExampleFor(q),
                        DeepAnalysisQuestionHelp.DomainLabel(q.Domain)))
                    .ToList()
                : [],
            DeepAnalysisUpsellRules.CopyNl(kind, expected),
            completed && kind == AssessmentKind.Competence ? competenceReport : null,
            completed && kind == AssessmentKind.Career ? careerReport : null,
            completed && kind == AssessmentKind.Culture ? cultureReport : null,
            completed && kind == AssessmentKind.Values ? valuesReport : null);
    }
}
