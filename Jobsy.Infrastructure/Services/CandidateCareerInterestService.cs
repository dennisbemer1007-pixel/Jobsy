using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateCareerInterestService : ICandidateCareerInterestService
{
    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;
    private readonly ICandidateMatchSnapshotService _matchSnapshots;
    private readonly ICandidateInsightsQueue _queue;
    private readonly IAssessmentResearchService _research;
    private readonly AssessmentSaveGuard _saveGuard;
    private readonly IFeatureFlags _flags;

    public CandidateCareerInterestService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICandidateMatchSnapshotService matchSnapshots,
        ICandidateInsightsQueue queue,
        IAssessmentResearchService research,
        AssessmentSaveGuard saveGuard,
        IFeatureFlags flags)
    {
        _db = db;
        _commercial = commercial;
        _matchSnapshots = matchSnapshots;
        _queue = queue;
        _research = research;
        _saveGuard = saveGuard;
        _flags = flags;
    }

    public async Task<CandidateCareerInterestStateDto> GetAsync(
        Guid userId,
        bool includeMatches = true,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        return await ComposeDtoAsync(userId, row, includeMatches, cancellationToken);
    }

    public async Task<CandidateCareerInterestStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCareerInterests
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (answers.Count == 0 && row is not null && CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return await ComposeDtoAsync(userId, row, includeMatches: true, cancellationToken);
        }

        var testsV2 = await _flags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var version = QuickScanVersionPolicy.ResolveVersionForNew(row?.QuickScanVersion, testsV2);

        var error = QuickScanAssessmentBridge.ValidateAnswers(
            AssessmentKind.Career, answers, complete, version);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        if (answers.Count == 0)
        {
            if (row is not null)
            {
                throw new InvalidOperationException(
                    "Lege antwoorden overschrijven je bestaande test niet. Stuur de huidige antwoorden mee.");
            }

            return await ComposeDtoAsync(userId, null, includeMatches: true, cancellationToken);
        }

        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateCareerInterest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Draft,
                AnswersJson = "{}",
                CreatedAtUtc = now
            };
            row.QuickScanVersion = version;
            _db.CandidateCareerInterests.Add(row);
        }

        version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        row.QuickScanHighestAnswered = Math.Max(row.QuickScanHighestAnswered, answers.Count);

        var storedAnswers = QuickScanAssessmentBridge.ParseAnswers(
            AssessmentKind.Career, row.AnswersJson, version);
        var wasCompleted = CandidateCompetencyStatuses.IsCompleted(row.Status)
            && QuickScanAssessmentBridge.IsComplete(AssessmentKind.Career, storedAnswers, version);
        var answersJson = QuickScanAssessmentBridge.SerializeAnswers(
            AssessmentKind.Career, answers, version);
        var baselineAnswersJson = row.AnswersJson;
        if (wasCompleted && !complete)
        {
            await _saveGuard.SaveDraftAsync(userId, AssessmentKind.Career, AssessmentVariant.Quick, answersJson, baselineAnswersJson, cancellationToken);
        }
        else if (wasCompleted && complete)
        {
            var noOp = await _saveGuard.CommitCompleteAsync(
                userId, AssessmentKind.Career, AssessmentVariant.Quick, answersJson, null,
                previousSnapshotJson: baselineAnswersJson, completedAnswersJson: baselineAnswersJson, cancellationToken);
            if (noOp && ScoresMatch(row, QuickScanAssessmentBridge.ScoreCareer(answers, version)))
            {
                return await ComposeDtoAsync(userId, row, includeMatches: true, cancellationToken);
            }
        }
        row.AnswersJson = answersJson;
        row.UpdatedAtUtc = now;


        var preview = QuickScanAssessmentBridge.ScoreCareer(answers, version);
        if (complete)
        {
            if (preview is not { IsComplete: true })
            {
                throw new InvalidOperationException(
                    $"Beantwoord alle {QuickScanAssessmentBridge.FullCount(AssessmentKind.Career, version)} vragen om de beroepentest af te ronden.");
            }

            var tags = QuickScanAssessmentBridge.DeriveCareerTags(preview, version);
            row.Status = CandidateCompetencyStatuses.Completed;
            row.RealisticPercent = preview.Realistic;
            row.InvestigativePercent = preview.Investigative;
            row.ArtisticPercent = preview.Artistic;
            row.SocialPercent = preview.Social;
            row.EnterprisingPercent = preview.Enterprising;
            row.ConventionalPercent = preview.Conventional;
            row.HollandCode = QuickScanAssessmentBridge.HollandCode(preview, version);
            row.RiasecTagsJson = CareerTestCatalog.SerializeTags(tags);
            row.MatchTagsJson = CareerTestCatalog.SerializeTags(tags);
            row.CompletedAtUtc = now;
        }
        else if (!CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.RealisticPercent = null;
            row.InvestigativePercent = null;
            row.ArtisticPercent = null;
            row.SocialPercent = null;
            row.EnterprisingPercent = null;
            row.ConventionalPercent = null;
            row.HollandCode = "";
            row.RiasecTagsJson = "[]";
            row.MatchTagsJson = "[]";
            row.CompletedAtUtc = null;
        }
        // Completed + autosave: keep status/scores; AnswersJson above is the pending edit
        // until the candidate finishes again with complete:true (rescore).

        await _db.SaveChangesAsync(cancellationToken);
        await _matchSnapshots.MarkInputsStaleAsync(userId, cancellationToken);
        if (complete)
        {
            await ResearchQuickScanSync.AfterCompleteAsync(
                _research,
                userId,
                AssessmentKind.Career,
                answers,
                itemDurationMs: null,
                totalDurationMs: null,
                cancellationToken);
            _queue.TryEnqueue(userId);
        }

        return await ComposeDtoAsync(userId, row, includeMatches: true, cancellationToken);
    }

    public async Task<RiasecScores?> GetCompletedScoresAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var deepScores = await PreferredDeepCareerAsync(userId, cancellationToken);
        if (deepScores is { IsComplete: true })
        {
            return deepScores;
        }

        var row = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        return CareerTestCatalog.CompletedScoresOrNull(
            row.Status,
            row.RealisticPercent,
            row.InvestigativePercent,
            row.ArtisticPercent,
            row.SocialPercent,
            row.EnterprisingPercent,
            row.ConventionalPercent);
    }

    public async Task<IReadOnlyList<string>> GetCompletedTagsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null || !CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return [];
        }

        return CareerTestCatalog.ParseTagsJson(row.RiasecTagsJson);
    }

    private async Task<CandidateCareerInterestStateDto> ComposeDtoAsync(
        Guid userId,
        CandidateCareerInterest? row,
        bool includeMatches,
        CancellationToken cancellationToken)
    {
        var commercial = await _commercial.GetAsync(cancellationToken);
        var price = DeepAnalysisPricing.For(commercial, AssessmentKind.Career);
        IReadOnlyList<CandidateMatchedVacancyDto> matches = [];
        var matchStatus = InsightsStatuses.Ready;
        if (includeMatches)
        {
            (matches, matchStatus) = await _matchSnapshots.GetAsync(userId, cancellationToken);
        }

        var completed = CareerTestCatalog.CompletedScoresOrNull(
            row?.Status,
            row?.RealisticPercent,
            row?.InvestigativePercent,
            row?.ArtisticPercent,
            row?.SocialPercent,
            row?.EnterprisingPercent,
            row?.ConventionalPercent);
        var deepScores = await PreferredDeepCareerAsync(userId, cancellationToken);
        var preferred = deepScores ?? completed;
        var fromDeep = deepScores is { IsComplete: true };
        var education = await EducationLabelAsync(userId, cancellationToken);
        var (compass, compassUpdating) = ResolveCompass(row, fromDeep, preferred, education);
        if (compassUpdating && preferred is { IsComplete: true })
        {
            _queue.TryEnqueue(userId);
        }

        var insights = InsightsStatuses.IsUpdating(matchStatus) || compassUpdating
            ? InsightsStatuses.Updating
            : InsightsStatuses.Ready;
        return ToDto(row, price, matches, compass, insights, preferred);
    }

    private async Task<RiasecScores?> PreferredDeepCareerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var deep = await _db.CandidateDeepAnalyses.AsNoTracking()
            .Where(d => d.UserId == userId
                        && d.Kind == AssessmentKind.Career
                        && d.Status == CandidateDeepAnalysisStatuses.Completed)
            .Select(d => new { d.Status, d.AnswersJson, d.ReportJson })
            .FirstOrDefaultAsync(cancellationToken);
        return deep is null ? null : StoredDeepScores.Career(deep.Status, deep.AnswersJson, deep.ReportJson);
    }

    private async Task<string?> EducationLabelAsync(Guid userId, CancellationToken cancellationToken)
    {
        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(cancellationToken);
        var prefs = MatchingProfileMapper.DeserializePrefs(json);
        return CandidateEducationLabel.From(prefs.Educations, prefs.EducationDirection);
    }

    private static bool ScoresMatch(CandidateCareerInterest row, RiasecScores? preview)
        => preview is { IsComplete: true }
           && row.RealisticPercent == preview.Realistic
           && row.InvestigativePercent == preview.Investigative
           && row.ArtisticPercent == preview.Artistic
           && row.SocialPercent == preview.Social
           && row.EnterprisingPercent == preview.Enterprising
           && row.ConventionalPercent == preview.Conventional;

    private static CandidateCareerInterestStateDto ToDto(
        CandidateCareerInterest? row,
        decimal deepAnalysisPriceEuro,
        IReadOnlyList<CandidateMatchedVacancyDto> matches,
        CareerCompassSnapshot compass,
        string insightsStatus,
        RiasecScores? scoreOverride = null)
    {
        var version = AssessmentTestVersions.Normalize(row?.QuickScanVersion);
        var fullCount = QuickScanAssessmentBridge.FullCount(AssessmentKind.Career, version);
        var answers = QuickScanAssessmentBridge.ParseAnswers(
            AssessmentKind.Career, row?.AnswersJson, version);
        var preview = QuickScanAssessmentBridge.ScoreCareer(answers, version);
        var completed = scoreOverride is { IsComplete: true }
            ? scoreOverride
            : CareerTestCatalog.CompletedScoresOrNull(
                row?.Status,
                row?.RealisticPercent,
                row?.InvestigativePercent,
                row?.ArtisticPercent,
                row?.SocialPercent,
                row?.EnterprisingPercent,
                row?.ConventionalPercent);
        var depthCount = Math.Max(answers.Count, row?.QuickScanHighestAnswered ?? 0);
        return new CandidateCareerInterestStateDto(
            row?.Status ?? CandidateCompetencyStatuses.Draft,
            answers,
            answers.Count,
            fullCount,
            completed,
            preview,
            row?.HollandCode ?? "",
            row?.CompletedAtUtc,
            row?.UpdatedAtUtc,
            QuickScanAssessmentBridge.CareerQuestions(version)
                .Select(q => new CareerQuestionDto(q.Id, q.Category, q.Reverse, q.TextKey))
                .ToList(),
            CareerTestCatalog.ParseTagsJson(row?.RiasecTagsJson),
            CareerTestCatalog.ParseTagsJson(row?.MatchTagsJson),
            DeepAnalysisService.FormatUpsellCopy(deepAnalysisPriceEuro, AssessmentKind.Career),
            matches,
            compass,
            insightsStatus,
            version,
            fullCount,
            depthCount < fullCount);
    }

    /// <summary>
    /// Prefer a stored compass. A completed test with no stored occupations is built in memory
    /// so the result page is not stuck on the empty prompt while the worker persists it.
    /// </summary>
    private static (CareerCompassSnapshot Compass, bool Updating) ResolveCompass(
        CandidateCareerInterest? row,
        bool fromDeepAnalysis,
        RiasecScores? scores,
        string? education)
    {
        var stored = CareerCompassJson.TryDeserialize(row?.CompassJson);
        var key = scores is { IsComplete: true } scored ? CareerCompassBuilder.ScoresKey(scored) : null;
        if (scores is { IsComplete: true })
        {
            var built = CareerCompassBuilder.CopyReasons(
                CareerCompassBuilder.Build(scores, fromDeepAnalysis || stored?.FromDeepAnalysis == true, education),
                stored);
            var fresh = stored is { HasOccupations: true }
                        && key is not null
                        && string.Equals(stored.ScoresFingerprint, key, StringComparison.Ordinal)
                        && SameOccupations(stored, built);
            return built.HasOccupations
                ? (fresh ? stored! : built, !fresh)
                : (CareerCompassSnapshot.Empty(fromDeepAnalysis), true);
        }

        return (CareerCompassSnapshot.Empty(fromDeepAnalysis), false);
    }

    private static bool SameOccupations(CareerCompassSnapshot left, CareerCompassSnapshot right)
    {
        var a = left.AllOccupations.Select(job => job.Title + ":" + CareerCompassBuilder.FormatPercent(job.Percent));
        var b = right.AllOccupations.Select(job => job.Title + ":" + CareerCompassBuilder.FormatPercent(job.Percent));
        return a.SequenceEqual(b, StringComparer.Ordinal);
    }
}
