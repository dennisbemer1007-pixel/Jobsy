using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
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

    public CandidateOnboardingService(
        JobsyDbContext db,
        ICandidateMatchSnapshotService matches,
        ICandidateInsightsQueue insightsQueue,
        ICandidateCareerPlanService careerPlans)
    {
        _db = db;
        _matches = matches;
        _insightsQueue = insightsQueue;
        _careerPlans = careerPlans;
    }

    public async Task<CandidateOnboardingStateDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        var shouldShow = await ShouldShowWizardAsync(userId, cancellationToken);
        CandidateOnboardingImpressionDto? impression = null;
        if (row.CurrentStep >= OnboardingWizardCatalog.StepCount || row.CompletedAtUtc is not null)
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
        var now = DateTime.UtcNow;
        var step = Math.Clamp(request.CurrentStep, 1, OnboardingWizardCatalog.StepCount);

        row.StepsJson = OnboardingStepAnalytics.MarkStarted(row.StepsJson, step, now);
        if (request.StepCompleted == true)
        {
            row.StepsJson = OnboardingStepAnalytics.MarkCompleted(row.StepsJson, step, now);
        }

        if (request.StepSkipped == true)
        {
            row.StepsJson = OnboardingStepAnalytics.MarkSkipped(row.StepsJson, step, now);
        }

        row.CurrentStep = step;
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
        return ToDto(row, await ShouldShowWizardAsync(userId, cancellationToken), null);
    }

    public async Task<CandidateOnboardingStateDto> CompleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        row.StepsJson = OnboardingStepAnalytics.MarkCompleted(
            row.StepsJson, OnboardingWizardCatalog.StepCount, now);
        row.CurrentStep = OnboardingWizardCatalog.StepCount;
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
        row = new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 1,
            StartedAtUtc = now,
            UpdatedAtUtc = now,
            StepsJson = OnboardingStepAnalytics.MarkStarted("[]", 1, now)
        };
        _db.CandidateOnboardings.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

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

        var strengths = TopCompetencyItems(competency.Scores, 2);
        var riasecTop = career.Scores is { } rs
            ? RiasecRanking.Rank(
                    rs.Realistic, rs.Investigative, rs.Artistic,
                    rs.Social, rs.Enterprising, rs.Conventional)
                .Take(2)
                .Select(x => x.Code)
                .ToList()
            : [];
        var riasecItems = riasecTop
            .Select(code => new OnboardingImpressionItemDto(
                code,
                DimensionLabels.For(code),
                OnboardingImpressionLibrary.RiasecSentence(code),
                career.Scores?.Get(code)))
            .ToList();

        OnboardingImpressionItemDto? cultureHighlight = null;
        if (culture.Scores is { } cs)
        {
            var best = OnboardingWizardCatalog.CultureDimensionCodes
                .Select(code => (Code: code, Pct: cs.Get(code)))
                .OrderByDescending(x => x.Pct)
                .ThenBy(x => x.Code, StringComparer.Ordinal)
                .First();
            cultureHighlight = new OnboardingImpressionItemDto(
                best.Code,
                DimensionLabels.For(best.Code),
                OnboardingImpressionLibrary.CultureSentence(best.Code),
                best.Pct);
        }

        OnboardingImpressionItemDto? topValue = null;
        if (values.Scores is { } vs)
        {
            var best = SchwartzValuesCatalog.CategoryCodes
                .Select(code => (Code: code, Pct: vs.Get(code)))
                .OrderByDescending(x => x.Pct)
                .ThenBy(x => x.Code, StringComparer.Ordinal)
                .First();
            topValue = new OnboardingImpressionItemDto(
                best.Code,
                DimensionLabels.For(best.Code),
                OnboardingImpressionLibrary.ValueSentence(best.Code),
                best.Pct);
        }

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

        var dreamSuggestions = OnboardingWizardCatalog.DreamChipsForRiasec(riasecTop);

        return new CandidateOnboardingImpressionDto(
            OnboardingImpressionLibrary.ResultLabel,
            strengths,
            riasecItems,
            cultureHighlight,
            topValue,
            liveMatches.Count,
            cards,
            dreamSuggestions,
            competency.IsProvisional,
            career.IsProvisional,
            culture.IsProvisional,
            values.IsProvisional);
    }

    private static List<OnboardingImpressionItemDto> TopCompetencyItems(CompetencyScores? scores, int take)
    {
        if (scores is null)
        {
            return [];
        }

        return CompetencyTestCatalog.CategoryCodes
            .Concat([CompetencyTestCatalog.Extraversie])
            .Select(code => (Code: code, Pct: scores.TryGet(code)))
            .Where(x => x.Pct is not null)
            .OrderByDescending(x => x.Pct)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .Take(take)
            .Select(x => new OnboardingImpressionItemDto(
                x.Code,
                DimensionLabels.For(x.Code),
                OnboardingImpressionLibrary.StrengthSentence(x.Code),
                x.Pct))
            .ToList();
    }

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
            impression);

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
