using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateInsightsComputer : ICandidateInsightsComputer
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly JobsyDbContext _db;
    private readonly IWhoAmIGenerationService _whoAmIGenerate;
    private readonly ICandidateMatchSnapshotService _matches;
    private readonly ICompetenceDeepReportService _competenceReport;
    private readonly ICandidateCareerPlanService _careerPlans;
    private readonly IFeatureFlags _features;
    private readonly ILogger<CandidateInsightsComputer> _logger;

    public CandidateInsightsComputer(
        JobsyDbContext db,
        IWhoAmIGenerationService whoAmIGenerate,
        ICandidateMatchSnapshotService matches,
        ICompetenceDeepReportService competenceReport,
        ICandidateCareerPlanService careerPlans,
        IFeatureFlags features,
        ILogger<CandidateInsightsComputer> logger)
    {
        _db = db;
        _whoAmIGenerate = whoAmIGenerate;
        _matches = matches;
        _competenceReport = competenceReport;
        _careerPlans = careerPlans;
        _features = features;
        _logger = logger;
    }

    public async Task RecomputeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _matches.InvalidateContextCache(userId);

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var prefs = TryReadPreferences(user.PreferencesJson);
        var highlights = WhoAmIProfileHighlights.FromPreferences(prefs);

        var competencyRow = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var careerRow = await _db.CandidateCareerInterests
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var cultureRow = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var valuesRow = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        var competencyResolved = ProvisionalAssessmentScores.ResolveCompetency(
            competencyRow?.Status,
            competencyRow?.AnswersJson,
            competencyRow?.SamenwerkenPercent,
            competencyRow?.ResultaatgerichtheidPercent,
            competencyRow?.StressbestendigheidPercent,
            competencyRow?.InnovatiePercent,
            competencyRow?.ExtraversiePercent);
        var careerResolved = ProvisionalAssessmentScores.ResolveCareer(
            careerRow?.Status,
            careerRow?.AnswersJson,
            careerRow?.RealisticPercent,
            careerRow?.InvestigativePercent,
            careerRow?.ArtisticPercent,
            careerRow?.SocialPercent,
            careerRow?.EnterprisingPercent,
            careerRow?.ConventionalPercent);
        var cultureResolved = ProvisionalAssessmentScores.ResolveCulture(
            cultureRow?.Status,
            cultureRow?.AnswersJson,
            ReadCulture(cultureRow));
        var valuesResolved = ProvisionalAssessmentScores.ResolveValues(
            valuesRow?.Status,
            valuesRow?.AnswersJson,
            valuesRow?.AutonomyPercent,
            valuesRow?.ConnectionPercent,
            valuesRow?.AchievementPercent,
            valuesRow?.StabilityPercent,
            valuesRow?.ImpactPercent);

        // WhoAmI story still requires completed tests (not provisional).
        var competency = competencyResolved is { IsProvisional: false, Scores: { IsComplete: true } }
            ? competencyResolved.Scores
            : null;
        var career = careerResolved is { IsProvisional: false, Scores: { IsComplete: true } }
            ? careerResolved.Scores
            : null;
        var culture = cultureResolved is { IsProvisional: false, Scores: { IsComplete: true } }
            ? cultureResolved.Scores
            : null;
        var values = valuesResolved is { IsProvisional: false, Scores: { IsComplete: true } }
            ? valuesResolved.Scores
            : null;

        var deepDone = await _db.CandidateDeepAnalyses.AsNoTracking()
            .AnyAsync(
                d => d.UserId == userId
                     && d.Kind == AssessmentKind.Career
                     && d.Status == CandidateDeepAnalysisStatuses.Completed,
                cancellationToken);

        // Compass can use provisional RIASEC so Carrière has a first signal after the wizard.
        await RefreshCompassAsync(
            careerRow,
            careerResolved.Scores is { IsComplete: true } ? careerResolved.Scores : career,
            deepDone,
            cancellationToken);
        await RefreshWhoAmIAsync(userId, competency, career, culture, values, highlights, cancellationToken);
        await RefreshCompetenceDeepReportAsync(userId, cancellationToken);

        var employersOn = await _features.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
        if (employersOn)
        {
            try
            {
                var matchFingerprint = await _matches.ComputeInputFingerprintAsync(userId, cancellationToken);
                var liveMatches = await _matches.ComputeLiveAsync(userId, cancellationToken);
                await _matches.SaveComputedAsync(userId, liveMatches, matchFingerprint, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Match snapshot recompute failed for {UserId}.", userId);
            }
        }

        try
        {
            var snapshot = BuildCareerSnapshot(
                competencyResolved.Scores,
                careerResolved.Scores,
                cultureResolved.Scores,
                valuesResolved.Scores);
            await _careerPlans.TryGeneratePendingAsync(userId, snapshot, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Pending career plan generation failed for {UserId}.", userId);
        }
    }

    private static HorizonCareerProfileSnapshot BuildCareerSnapshot(
        CompetencyScores? competency,
        RiasecScores? career,
        CulturePersonalityScores? culture,
        SchwartzValuesScores? values)
    {
        var strengths = new List<string>();
        var gaps = new List<string>();
        if (competency is not null)
        {
            foreach (var code in CompetencyTestCatalog.CategoryCodes)
            {
                var label = WhoAmIKeywords.EverydayCompetency(code);
                var score = competency.Get(code);
                if (score >= 60)
                {
                    strengths.Add(label);
                }
                else if (score is > 0 and < 50)
                {
                    gaps.Add(label);
                }
            }
        }

        if (career is { IsComplete: true })
        {
            foreach (var code in CareerTestCatalog.RiasecCodes.OrderByDescending(career.Get).Take(2))
            {
                strengths.Add(CareerCompassBuilder.TypeLabel(code));
            }
        }

        if (culture is not null)
        {
            foreach (var code in OnboardingWizardCatalog.CultureDimensionCodes
                         .OrderByDescending(culture.Get)
                         .Take(2))
            {
                strengths.Add(CulturePersonalityCatalog.EverydayLabel(code));
            }
        }

        if (values is { IsComplete: true })
        {
            foreach (var code in SchwartzValuesCatalog.CategoryCodes.OrderByDescending(values.Get).Take(2))
            {
                strengths.Add(SchwartzValuesCatalog.EverydayLabel(code));
            }
        }

        return new HorizonCareerProfileSnapshot(
            strengths.Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList(),
            gaps.Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList(),
            HasDnaSignal: strengths.Count > 0 || gaps.Count > 0);
    }

    private async Task RefreshCompassAsync(
        CandidateCareerInterest? careerRow,
        RiasecScores? career,
        bool deepDone,
        CancellationToken cancellationToken)
    {
        if (careerRow is null || career is not { IsComplete: true })
        {
            return;
        }

        // Keep a stored compass that already has occupations. Replacing it with a
        // second local score list made the same job show two different percents.
        // A deep compass that collapsed to a handful of titles is topped up in place.
        var stored = CareerCompassJson.TryDeserialize(careerRow.CompassJson);
        var key = CareerCompassBuilder.ScoresKey(career);
        if (stored is { HasOccupations: true })
        {
            var fresh = string.Equals(stored.ScoresFingerprint, key, StringComparison.Ordinal);
            if (stored.FromDeepAnalysis)
            {
                var repaired = CareerCompassSanitize.EnsureDepth(stored, career) with { ScoresFingerprint = key };
                if (fresh && SameOccupations(stored, repaired))
                {
                    return;
                }

                careerRow.CompassJson = CareerCompassJson.Serialize(repaired);
                careerRow.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            if (fresh)
            {
                return;
            }
        }

        var built = CareerCompassBuilder.Build(career, deepDone);
        var compass = deepDone
            ? CareerCompassSanitize.EnsureDepth(built, career)
            : built;
        var json = CareerCompassJson.Serialize(compass);
        if (string.Equals(careerRow.CompassJson, json, StringComparison.Ordinal))
        {
            return;
        }

        careerRow.CompassJson = json;
        careerRow.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool SameOccupations(CareerCompassSnapshot left, CareerCompassSnapshot right)
    {
        var a = left.AllOccupations.Select(j => j.Title + ":" + CareerCompassBuilder.FormatPercent(j.Percent)).OrderBy(x => x, StringComparer.Ordinal);
        var b = right.AllOccupations.Select(j => j.Title + ":" + CareerCompassBuilder.FormatPercent(j.Percent)).OrderBy(x => x, StringComparer.Ordinal);
        return a.SequenceEqual(b, StringComparer.Ordinal);
    }

    private async Task RefreshWhoAmIAsync(
        Guid userId,
        CompetencyScores? competency,
        RiasecScores? career,
        CulturePersonalityScores? culture,
        SchwartzValuesScores? values,
        WhoAmIProfileHighlights highlights,
        CancellationToken cancellationToken)
    {
        if (competency is not { IsComplete: true }
            || career is not { IsComplete: true }
            || culture is not { IsComplete: true })
        {
            return;
        }

        var (competenceTraits, competenceDeep) = await WhoAmICompetenceLoader.LoadAsync(_db, userId, cancellationToken);
        var competenceSource = WhoAmICompetenceSource.FingerprintSuffix(competenceTraits, competenceDeep);
        var competence = competenceDeep ? competenceTraits : null;
        var fingerprint = WhoAmICompleteness.Fingerprint(competency, career, culture, highlights, values, competenceSource);
        var stored = await _db.CandidateWhoAmIProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var now = DateTime.UtcNow;
        var sanitized = stored is null ? null : WhoAmIStoryBuilder.Sanitize(stored.StoryText);
        var fingerprintMatch = stored is not null
                               && string.Equals(stored.InputFingerprint, fingerprint, StringComparison.Ordinal)
                               && sanitized is not null;
        var storyOk = false;
        if (sanitized is not null)
        {
            var sheet = CandidateFactSheet.ForWhoAmI(competency, career, culture, highlights, values);
            storyOk = CandidateFactGuard.RejectionReason(sanitized, sheet) is null
                      && WhoAmIStoryBuilder.Accepts(sanitized, highlights, competency, culture, career, values);
        }

        var forced = WhoAmIForceRegeneration.Consume(userId);
        var shouldGenerate = forced
                             || (stored is not null
                                 && WhoAmIForceRegeneration.ShouldGenerate(
                                     fingerprintMatch,
                                     stored.FromOpenAi,
                                     storyOk,
                                     stored.LastAttemptUtc,
                                     now,
                                     forced: false));
        if (!shouldGenerate)
        {
            if (stored is null || !fingerprintMatch)
            {
                await StoreLocalWhoAmIAsync(
                    userId, stored, competency, career, culture, highlights, values, fingerprint, now, cancellationToken, competence);
            }

            return;
        }

        try
        {
            if (stored is null)
            {
                stored = new CandidateWhoAmIProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAtUtc = now
                };
                _db.CandidateWhoAmIProfiles.Add(stored);
            }

            stored.LastAttemptUtc = now;
            stored.UpdatedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            var generated = await _whoAmIGenerate.GenerateAsync(
                competency, career, culture, highlights, values, competence, cancellationToken);
            var employersForStory = await _features.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
            var storyText = generated.FromOpenAi
                ? generated.Story ?? ""
                : WhoAmIStoryBuilder.Build(competency, career, culture, highlights, values, employersForStory, competence: competence);
            stored.StoryText = storyText;
            stored.KeywordsJson = JsonSerializer.Serialize(generated.Keywords, Json);
            stored.InputFingerprint = fingerprint;
            stored.FromOpenAi = generated.FromOpenAi;
            stored.StoryGeneratedAtUtc = now;
            stored.LastAttemptUtc = now;
            stored.UpdatedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "WhoAmI story recompute failed for {UserId}.", userId);
        }
    }

    private async Task StoreLocalWhoAmIAsync(
        Guid userId,
        CandidateWhoAmIProfile? stored,
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights highlights,
        SchwartzValuesScores? values,
        string fingerprint,
        DateTime now,
        CancellationToken cancellationToken,
        IReadOnlyList<(string Code, int Score)>? competence = null)
    {
        var employers = await _features.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);
        var story = WhoAmIStoryBuilder.Build(competency, career, culture, highlights, values, employers, competence: competence);
        var keywords = WhoAmIKeywords.FromScores(competency, career, culture, values);
        if (stored is null)
        {
            stored = new CandidateWhoAmIProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now
            };
            _db.CandidateWhoAmIProfiles.Add(stored);
        }

        stored.StoryText = story;
        stored.KeywordsJson = JsonSerializer.Serialize(keywords, Json);
        stored.InputFingerprint = fingerprint;
        stored.FromOpenAi = false;
        stored.StoryGeneratedAtUtc = now;
        stored.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshCompetenceDeepReportAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            // Retries a stale (>24h old, template-only) AI summary when a prior completion-time
            // AI call failed or timed out.
            await _competenceReport.RefineAiAsync(userId, cancellationToken);

            var row = await _db.CandidateDeepAnalyses.AsNoTracking()
                .FirstOrDefaultAsync(
                    d => d.UserId == userId && d.Kind == AssessmentKind.Competence,
                    cancellationToken);
            if (row is null || !CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
            {
                return;
            }

            var stale = row.ReportVersion < CompetenceDeepReportJson.CurrentReportVersion;
            var missing = string.IsNullOrWhiteSpace(row.ReportJson)
                          || CompetenceDeepReportJson.Deserialize(row.ReportJson) is null;
            if (stale || missing)
            {
                await _competenceReport.BuildAndStoreAsync(userId, tryAi: true, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Competence deep-report recompute failed for {UserId}.", userId);
        }
    }

    private static CulturePersonalityScores? ReadCulture(CandidateCulturePersonalityProfile? row)
    {
        if (row is null || !CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        var culture = new CulturePersonalityScores(
            row.AutonomyPercent,
            row.InformalPercent,
            row.CollaborationPercent,
            row.FlexibilityPercent,
            row.InnovationPercent,
            row.PeopleFirstPercent,
            row.OpennessPercent,
            row.ConscientiousnessPercent,
            row.ExtraversionPercent,
            row.AgreeablenessPercent,
            row.EmotionalStabilityPercent);
        return culture is { IsComplete: true } ? culture : null;
    }

    private static CandidatePreferencesDto? TryReadPreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
