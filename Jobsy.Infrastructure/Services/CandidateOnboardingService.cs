using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateOnboardingService : ICandidateOnboardingService
{
    private readonly JobsyDbContext _db;
    private readonly ICandidateMatchSnapshotService _matches;
    private readonly ICandidateInsightsQueue _insightsQueue;
    private readonly ICandidateCareerPlanService _careerPlans;
    private readonly IFeatureFlags _featureFlags;

    public CandidateOnboardingService(
        JobsyDbContext db,
        ICandidateMatchSnapshotService matches,
        ICandidateInsightsQueue insightsQueue,
        ICandidateCareerPlanService careerPlans,
        IFeatureFlags featureFlags)
    {
        _db = db;
        _matches = matches;
        _insightsQueue = insightsQueue;
        _careerPlans = careerPlans;
        _featureFlags = featureFlags;
    }

    public async Task<CandidateOnboardingStateDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        await AlignWizardVersionOnReadAsync(row, persist: true, cancellationToken);
        var shouldShow = await ShouldShowWizardAsync(userId, cancellationToken);
        CandidateOnboardingImpressionDto? impression = null;
        if (row.FinishReached
            || row.CurrentStep >= ActiveStepCount(row.WizardVersion)
            || row.CompletedAtUtc is not null)
        {
            impression = await BuildImpressionAsync(userId, cancellationToken);
        }

        return ToDto(row, shouldShow, impression);
    }

    public async Task<CandidateOnboardingStateDto> SaveProgressAsync(
        Guid userId,
        CandidateOnboardingProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        await AlignWizardVersionOnReadAsync(row, persist: true, cancellationToken);
        var now = DateTime.UtcNow;
        var version = row.WizardVersion;
        var maxStep = ActiveStepCount(version);
        var step = Math.Clamp(request.CurrentStep, 1, maxStep);

        row.StepsJson = OnboardingStepAnalytics.MarkStarted(row.StepsJson, step, now, version);
        if (request.StepCompleted == true)
        {
            row.StepsJson = OnboardingStepAnalytics.MarkCompleted(row.StepsJson, step, now, version);
        }

        if (request.StepSkipped == true)
        {
            row.StepsJson = OnboardingStepAnalytics.MarkSkipped(row.StepsJson, step, now, version);
        }

        row.CurrentStep = step;
        if (request.FinishReached == true)
        {
            row.FinishReached = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            row.Source = request.Source.Trim();
            if (row.Source.Length > 64)
            {
                row.Source = row.Source[..64];
            }
        }

        row.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        CandidateOnboardingImpressionDto? impression = null;
        if (row.FinishReached)
        {
            impression = await BuildImpressionAsync(userId, cancellationToken);
        }

        return ToDto(row, await ShouldShowWizardAsync(userId, cancellationToken), impression);
    }

    public async Task<CandidateOnboardingStateDto> CompleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        await AlignWizardVersionOnReadAsync(row, persist: true, cancellationToken);
        var now = DateTime.UtcNow;
        var version = row.WizardVersion;
        var lastStep = ActiveStepCount(version);
        row.StepsJson = OnboardingStepAnalytics.MarkCompleted(
            row.StepsJson, lastStep, now, version);
        row.CurrentStep = lastStep;
        row.FinishReached = true;
        row.CompletedAtUtc ??= now;
        row.UpdatedAtUtc = now;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is not null)
        {
            user.CandidateHowToCompletedAt ??= now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Background: insights/matches + career plan generation (no AI in this request).
        _insightsQueue.TryEnqueue(userId);
        await _careerPlans.EnqueueGenerationIfNeededAsync(userId, cancellationToken);

        var impression = await BuildImpressionAsync(userId, cancellationToken);
        return ToDto(row, shouldShow: false, impression);
    }

    public async Task<bool> ShouldShowWizardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.Role != UserRole.Candidate)
        {
            return false;
        }

        if (user.CandidateHowToCompletedAt is not null)
        {
            return false;
        }

        var onboarding = await _db.CandidateOnboardings.AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == userId, cancellationToken);
        if (onboarding?.CompletedAtUtc is not null)
        {
            return false;
        }

        // Existing candidates with a complete-enough profile: never force the wizard.
        var prefs = MatchingProfileMapper.DeserializePrefs(user.PreferencesJson);
        var hasBasics = !string.IsNullOrWhiteSpace(user.FullName)
                        && prefs.MaxTravelMinutes is > 0
                        && !string.IsNullOrWhiteSpace(prefs.PreferredTransport);
        var hasEducation = MatchProfileCompleteness.HasEducationLevel(prefs.Educations);
        if (hasBasics && hasEducation)
        {
            return false;
        }

        return true;
    }

    private async Task<CandidateOnboarding> EnsureRowAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _db.CandidateOnboardings
            .FirstOrDefaultAsync(o => o.UserId == userId, cancellationToken);
        if (row is not null)
        {
            return row;
        }

        var now = DateTime.UtcNow;
        var passportOn = await IsPassportEnabledAsync(cancellationToken);
        var version = passportOn
            ? OnboardingWizardCatalog.WizardVersionV3
            : OnboardingWizardCatalog.WizardVersionV2;
        row = new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 1,
            WizardVersion = version,
            FinishReached = false,
            StartedAtUtc = now,
            UpdatedAtUtc = now,
            StepsJson = OnboardingStepAnalytics.MarkStarted("[]", 1, now, version)
        };
        _db.CandidateOnboardings.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    /// <summary>
    /// Flag ON: incomplete v1/v2 → v3 on read (answers untouched; only step pointer moves).
    /// Flag OFF: incomplete v3 → v2 on read via <see cref="OnboardingWizardCatalog.MapV3ToV2"/>.
    /// Completed rows are never reopened or remapped.
    /// </summary>
    private async Task AlignWizardVersionOnReadAsync(
        CandidateOnboarding row,
        bool persist,
        CancellationToken cancellationToken)
    {
        if (row.CompletedAtUtc is not null)
        {
            return;
        }

        var passportOn = await IsPassportEnabledAsync(cancellationToken);
        var targetVersion = passportOn
            ? OnboardingWizardCatalog.WizardVersionV3
            : OnboardingWizardCatalog.WizardVersionV2;

        if (row.WizardVersion == targetVersion)
        {
            // Still migrate raw v1 leftovers when flag is OFF (legacy path).
            if (!passportOn && row.WizardVersion < OnboardingWizardCatalog.WizardVersionV2)
            {
                await MigrateV1ToV2Async(row, persist, cancellationToken);
            }

            return;
        }

        if (passportOn)
        {
            // v1 → v2 map first, then v2 → v3.
            var fromVersion = row.WizardVersion;
            if (fromVersion < OnboardingWizardCatalog.WizardVersionV2)
            {
                var v1Step = row.CurrentStep;
                row.CurrentStep = OnboardingWizardCatalog.MapV1Step(v1Step);
                if (OnboardingWizardCatalog.MapV1ShowsFinish(v1Step))
                {
                    row.FinishReached = true;
                }

                fromVersion = OnboardingWizardCatalog.WizardVersionV2;
            }

            if (fromVersion < OnboardingWizardCatalog.WizardVersionV3)
            {
                row.CurrentStep = OnboardingWizardCatalog.MapV2ToV3(row.CurrentStep);
            }

            row.WizardVersion = OnboardingWizardCatalog.WizardVersionV3;
        }
        else
        {
            // Flag OFF: map v3 back to v2 so the classic wizard never sees v3 pointers.
            if (row.WizardVersion >= OnboardingWizardCatalog.WizardVersionV3)
            {
                row.CurrentStep = OnboardingWizardCatalog.MapV3ToV2(row.CurrentStep);
                row.WizardVersion = OnboardingWizardCatalog.WizardVersionV2;
            }
            else if (row.WizardVersion < OnboardingWizardCatalog.WizardVersionV2)
            {
                await MigrateV1ToV2Async(row, persist, cancellationToken);
                return;
            }
        }

        row.UpdatedAtUtc = DateTime.UtcNow;
        if (persist)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task MigrateV1ToV2Async(
        CandidateOnboarding row,
        bool persist,
        CancellationToken cancellationToken)
    {
        if (row.WizardVersion >= OnboardingWizardCatalog.WizardVersionV2
            || row.CompletedAtUtc is not null)
        {
            return;
        }

        var v1Step = row.CurrentStep;
        row.CurrentStep = OnboardingWizardCatalog.MapV1Step(v1Step);
        if (OnboardingWizardCatalog.MapV1ShowsFinish(v1Step))
        {
            row.FinishReached = true;
        }

        row.WizardVersion = OnboardingWizardCatalog.WizardVersionV2;
        row.UpdatedAtUtc = DateTime.UtcNow;
        if (persist)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> IsPassportEnabledAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _featureFlags.IsEnabledAsync(PlatformFeature.CandidatePassport, cancellationToken);
        }
        catch
        {
            return FeatureFlagSnapshot.Defaults.CandidatePassportEnabled;
        }
    }

    private static int ActiveStepCount(int wizardVersion)
        => wizardVersion >= OnboardingWizardCatalog.WizardVersionV3
            ? OnboardingWizardCatalog.V3StepCount
            : OnboardingWizardCatalog.StepCount;

    private async Task<CandidateOnboardingImpressionDto> BuildImpressionAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var competencyRow = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var careerRow = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var cultureRow = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var valuesRow = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var prefs = MatchingProfileMapper.DeserializePrefs(user?.PreferencesJson);
        var dream = await _careerPlans.GetDreamTitleAsync(userId, cancellationToken);

        var competency = ProvisionalAssessmentScores.ResolveCompetency(
            competencyRow?.Status,
            competencyRow?.AnswersJson,
            competencyRow?.SamenwerkenPercent,
            competencyRow?.ResultaatgerichtheidPercent,
            competencyRow?.StressbestendigheidPercent,
            competencyRow?.InnovatiePercent,
            competencyRow?.ExtraversiePercent);
        var career = ProvisionalAssessmentScores.ResolveCareer(
            careerRow?.Status,
            careerRow?.AnswersJson,
            careerRow?.RealisticPercent,
            careerRow?.InvestigativePercent,
            careerRow?.ArtisticPercent,
            careerRow?.SocialPercent,
            careerRow?.EnterprisingPercent,
            careerRow?.ConventionalPercent);
        CulturePersonalityScores? storedCulture = null;
        if (cultureRow is not null && CandidateCompetencyStatuses.IsCompleted(cultureRow.Status))
        {
            storedCulture = new CulturePersonalityScores(
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
        }

        var culture = ProvisionalAssessmentScores.ResolveCulture(
            cultureRow?.Status, cultureRow?.AnswersJson, storedCulture);
        var values = ProvisionalAssessmentScores.ResolveValues(
            valuesRow?.Status,
            valuesRow?.AnswersJson,
            valuesRow?.AutonomyPercent,
            valuesRow?.ConnectionPercent,
            valuesRow?.AchievementPercent,
            valuesRow?.StabilityPercent,
            valuesRow?.ImpactPercent);

        var core = OnboardingImpressionComposer.Compose(
            competency.Scores,
            career.Scores,
            culture.Scores,
            values.Scores);
        var strengths = core.Strengths.Select(ToImpressionItem).ToList();
        var riasecItems = core.RiasecTop.Select(ToImpressionItem).ToList();
        var cultureHighlight = core.CultureHighlight is { } ch ? ToImpressionItem(ch) : null;
        var topValue = core.TopValue is { } tv ? ToImpressionItem(tv) : null;

        var liveMatches = await _matches.ComputeLiveAsync(userId, cancellationToken);
        var topStrengthLabel = strengths.FirstOrDefault()?.Label;
        var cards = liveMatches.Take(3).Select(m =>
        {
            var travelHint = TryParseTravelMinutes(m.Why);
            return new OnboardingMatchCardDto(
                m.Id,
                m.Title,
                m.CompanyName,
                m.MatchPercent,
                OnboardingImpressionLibrary.MatchWhyLine(topStrengthLabel, dream, travelHint),
                IsProvisional: competency.IsProvisional
                               || career.IsProvisional
                               || culture.IsProvisional);
        }).ToList();

        return new CandidateOnboardingImpressionDto(
            OnboardingImpressionLibrary.ResultLabel,
            strengths,
            riasecItems,
            cultureHighlight,
            topValue,
            liveMatches.Count,
            cards,
            competency.IsProvisional,
            career.IsProvisional,
            culture.IsProvisional,
            values.IsProvisional);
    }

    private static OnboardingImpressionItemDto ToImpressionItem(OnboardingImpressionCoreItem item)
        => new(item.Code, item.Label, item.Sentence, item.Percent);

    private static CandidateOnboardingStateDto ToDto(
        CandidateOnboarding row,
        bool shouldShow,
        CandidateOnboardingImpressionDto? impression)
        => new(
            row.CurrentStep,
            row.StartedAtUtc,
            row.CompletedAtUtc,
            row.CompletedAtUtc is not null,
            shouldShow,
            row.Source,
            OnboardingStepAnalytics.Parse(row.StepsJson),
            impression,
            row.WizardVersion,
            row.FinishReached);

    private static int? TryParseTravelMinutes(IReadOnlyList<string>? why)
    {
        if (why is null)
        {
            return null;
        }

        foreach (var line in why)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // e.g. "12 minuten reizen" / "reistijd 12 min"
            var digits = new string(line.Where(char.IsDigit).ToArray());
            if (digits.Length > 0
                && int.TryParse(digits, out var mins)
                && mins is > 0 and < 180
                && line.Contains("min", StringComparison.OrdinalIgnoreCase))
            {
                return mins;
            }
        }

        return null;
    }
}
