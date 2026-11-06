using Jobsy.Core.Enums;
using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateCulturePersonalityService : ICandidateCulturePersonalityService
{
    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;
    private readonly ICandidateInsightsQueue _queue;
    private readonly IAssessmentResearchService _research;
    private readonly AssessmentSaveGuard _saveGuard;
    private readonly ICandidateMatchSnapshotService _matchSnapshots;
    private readonly IFeatureFlags _flags;

    public CandidateCulturePersonalityService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        ICandidateInsightsQueue queue,
        IAssessmentResearchService research,
        ICandidateMatchSnapshotService matchSnapshots,
        AssessmentSaveGuard saveGuard,
        IFeatureFlags flags)
    {
        _db = db;
        _commercial = commercial;
        _queue = queue;
        _research = research;
        _matchSnapshots = matchSnapshots;
        _saveGuard = saveGuard;
        _flags = flags;
    }

    public async Task<CandidateCulturePersonalityStateDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var price = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Culture);
        return ToDto(row, price);
    }

    public async Task<CandidateCulturePersonalityStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCulturePersonalityProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (answers.Count == 0 && row is not null && CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            var kept = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Culture);
            return ToDto(row, kept);
        }

        var testsV2 = await _flags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
        var version = QuickScanVersionPolicy.ResolveVersionForNew(row?.QuickScanVersion, testsV2);

        var error = QuickScanAssessmentBridge.ValidateAnswers(
            AssessmentKind.Culture, answers, complete, version);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        if (answers.Count == 0)
        {
            if (row is not null)
            {
                throw new InvalidOperationException(
                    "Lege antwoorden overschrijven je bestaande scan niet. Stuur de huidige antwoorden mee.");
            }

            return ToDto(null, FlexCommercialSettings.DefaultDeepAnalysisPriceEuro);
        }

        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateCulturePersonalityProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Draft,
                AnswersJson = "{}",
                CreatedAtUtc = now
            };
            row.QuickScanVersion = version;
            _db.CandidateCulturePersonalityProfiles.Add(row);
        }

        version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        row.QuickScanHighestAnswered = Math.Max(row.QuickScanHighestAnswered, answers.Count);

        var storedAnswers = QuickScanAssessmentBridge.ParseAnswers(
            AssessmentKind.Culture, row.AnswersJson, version);
        var wasCompleted = CandidateCompetencyStatuses.IsCompleted(row.Status)
            && QuickScanAssessmentBridge.IsComplete(AssessmentKind.Culture, storedAnswers, version);
        var answersJson = QuickScanAssessmentBridge.SerializeAnswers(
            AssessmentKind.Culture, answers, version);
        var baselineAnswersJson = row.AnswersJson;
        if (wasCompleted && !complete)
        {
            await _saveGuard.SaveDraftAsync(userId, AssessmentKind.Culture, AssessmentVariant.Quick, answersJson, baselineAnswersJson, cancellationToken);
        }
        else if (wasCompleted && complete)
        {
            var noOp = await _saveGuard.CommitCompleteAsync(
                userId, AssessmentKind.Culture, AssessmentVariant.Quick, answersJson, null, baselineAnswersJson, baselineAnswersJson, cancellationToken);
            if (noOp && ScoresMatch(row, QuickScanAssessmentBridge.ScoreCulture(answers, version)))
            {
                var priceNoOp = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Culture);
                return ToDto(row, priceNoOp);
            }
        }
        row.AnswersJson = answersJson;
        row.UpdatedAtUtc = now;

        var preview = QuickScanAssessmentBridge.ScoreCulture(answers, version);
        if (complete)
        {
            if (preview is not { IsComplete: true })
            {
                throw new InvalidOperationException(
                    $"Beantwoord alle {QuickScanAssessmentBridge.FullCount(AssessmentKind.Culture, version)} stellingen om de cultuurscan af te ronden.");
            }

            ApplyScores(row, preview);
            row.Status = CandidateCompetencyStatuses.Completed;
            row.MatchTagsJson = CulturePersonalityCatalog.SerializeTags(
                QuickScanAssessmentBridge.DeriveCultureTags(preview, version));
            row.CompletedAtUtc = now;
        }
        else if (!CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            ClearScores(row);
            row.Status = CandidateCompetencyStatuses.Draft;
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
                AssessmentKind.Culture,
                answers,
                itemDurationMs: null,
                totalDurationMs: null,
                cancellationToken);
            _queue.TryEnqueue(userId);
        }

        var price = DeepAnalysisPricing.For(await _commercial.GetAsync(cancellationToken), AssessmentKind.Culture);
        return ToDto(row, price);
    }

    public async Task<CulturePersonalityScores?> GetCompletedScoresAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.UserId == userId && c.Status == CandidateCompetencyStatuses.Completed,
                cancellationToken);
        return row is null ? null : FromRow(row);
    }

    private static bool ScoresMatch(CandidateCulturePersonalityProfile row, CulturePersonalityScores? preview)
        => preview is { IsComplete: true }
           && row.AutonomyPercent == preview.Autonomy
           && row.InformalPercent == preview.Informal
           && row.CollaborationPercent == preview.Collaboration
           && row.FlexibilityPercent == preview.Flexibility
           && row.InnovationPercent == preview.Innovation
           && row.PeopleFirstPercent == preview.PeopleFirst
           && row.OpennessPercent == preview.Openness
           && row.ConscientiousnessPercent == preview.Conscientiousness
           && row.ExtraversionPercent == preview.Extraversion
           && row.AgreeablenessPercent == preview.Agreeableness
           && row.EmotionalStabilityPercent == preview.EmotionalStability;

    private static void ApplyScores(CandidateCulturePersonalityProfile row, CulturePersonalityScores s)
    {
        row.AutonomyPercent = s.Autonomy;
        row.InformalPercent = s.Informal;
        row.CollaborationPercent = s.Collaboration;
        row.FlexibilityPercent = s.Flexibility;
        row.InnovationPercent = s.Innovation;
        row.PeopleFirstPercent = s.PeopleFirst;
        row.OpennessPercent = s.Openness;
        row.ConscientiousnessPercent = s.Conscientiousness;
        row.ExtraversionPercent = s.Extraversion;
        row.AgreeablenessPercent = s.Agreeableness;
        row.EmotionalStabilityPercent = s.EmotionalStability;
    }

    private static void ClearScores(CandidateCulturePersonalityProfile row)
    {
        row.AutonomyPercent = null;
        row.InformalPercent = null;
        row.CollaborationPercent = null;
        row.FlexibilityPercent = null;
        row.InnovationPercent = null;
        row.PeopleFirstPercent = null;
        row.OpennessPercent = null;
        row.ConscientiousnessPercent = null;
        row.ExtraversionPercent = null;
        row.AgreeablenessPercent = null;
        row.EmotionalStabilityPercent = null;
    }

    private static CulturePersonalityScores? FromRow(CandidateCulturePersonalityProfile row)
    {
        var scores = new CulturePersonalityScores(
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
        return scores.IsComplete ? scores : null;
    }

    private static CandidateCulturePersonalityStateDto ToDto(
        CandidateCulturePersonalityProfile? row,
        decimal price)
    {
        if (row is null)
        {
            return new CandidateCulturePersonalityStateDto(
                CandidateCompetencyStatuses.Draft,
                new Dictionary<int, int>(),
                null,
                [],
                null,
                price,
                PreviewScores: null,
                QuickScanVersion: 0);
        }

        var version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
        var fullCount = QuickScanAssessmentBridge.FullCount(AssessmentKind.Culture, version);
        var answers = QuickScanAssessmentBridge.ParseAnswers(
            AssessmentKind.Culture, row.AnswersJson, version);
        var preview = QuickScanAssessmentBridge.ScoreCulture(answers, version);
        var depthCount = Math.Max(answers.Count, row.QuickScanHighestAnswered);
        return new CandidateCulturePersonalityStateDto(
            row.Status,
            answers,
            FromRow(row),
            CulturePersonalityCatalog.ParseTags(row.MatchTagsJson),
            row.CompletedAtUtc,
            price,
            preview,
            version,
            fullCount,
            depthCount < fullCount);
    }
}

public sealed class CompanyCultureService : ICompanyCultureService
{
    private readonly JobsyDbContext _db;
    private readonly IMemoryCache _cache;

    public CompanyCultureService(JobsyDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<CompanyCultureStateDto> GetAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyCultureProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId, cancellationToken);
        return ToDto(row);
    }

    public async Task<CompanyCultureStateDto> SaveAsync(
        Guid companyId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        // Employers answer the 12 culture items (1–12); personality facets optional for company tone.
        var cultureOnly = answers
            .Where(kv => kv.Key is >= 1 and <= 12)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var error = CulturePersonalityCatalog.ValidateAnswers(
            answers.Count >= 12 ? PadPersonalityDefaults(answers) : cultureOnly,
            requireComplete: false);
        if (error is not null && complete)
        {
            // Require culture items 1-12 complete for employer profile.
            for (var i = 1; i <= 12; i++)
            {
                if (!answers.TryGetValue(i, out var v) || !CulturePersonalityCatalog.IsValidAnswer(v))
                {
                    throw new InvalidOperationException(
                        "Beantwoord alle 12 cultuurstellingen om het bedrijfsprofiel af te ronden.");
                }
            }
        }

        var row = await _db.CompanyCultureProfiles
            .FirstOrDefaultAsync(c => c.CompanyId == companyId, cancellationToken);
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CompanyCultureProfile
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Status = CandidateCompetencyStatuses.Draft,
                AnswersJson = "{}",
                CreatedAtUtc = now
            };
            _db.CompanyCultureProfiles.Add(row);
        }

        var padded = PadPersonalityDefaults(answers);
        row.AnswersJson = CulturePersonalityCatalog.SerializeAnswers(padded);
        row.UpdatedAtUtc = now;

        var preview = CulturePersonalityCatalog.Score(padded);
        if (complete && preview is not null)
        {
            row.Status = CandidateCompetencyStatuses.Completed;
            row.Source = CompanyCultureSources.Full;
            row.AutonomyPercent = preview.Autonomy;
            row.InformalPercent = preview.Informal;
            row.CollaborationPercent = preview.Collaboration;
            row.FlexibilityPercent = preview.Flexibility;
            row.InnovationPercent = preview.Innovation;
            row.PeopleFirstPercent = preview.PeopleFirst;
            row.OpennessPercent = preview.Openness;
            row.ConscientiousnessPercent = preview.Conscientiousness;
            row.ExtraversionPercent = preview.Extraversion;
            row.AgreeablenessPercent = preview.Agreeableness;
            row.EmotionalStabilityPercent = preview.EmotionalStability;
            row.CompletedAtUtc = now;
        }
        else
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.CompletedAtUtc = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await EvictCultureCacheAsync(companyId, cancellationToken);
        return ToDto(row);
    }

    public async Task<CulturePersonalityScores?> GetCompletedScoresAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyCultureProfiles.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.CompanyId == companyId && c.Status == CandidateCompetencyStatuses.Completed,
                cancellationToken);
        if (row is null)
        {
            return null;
        }

        var scores = new CulturePersonalityScores(
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
        return scores.Autonomy is not null ? scores : null;
    }

    private async Task EvictCultureCacheAsync(Guid companyId, CancellationToken cancellationToken)
    {
        _cache.Remove(CompanyCultureCacheKeys.ForCompany(companyId));
        var childIds = await _db.Companies.AsNoTracking()
            .Where(c => c.ParentCompanyId == companyId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        foreach (var childId in childIds)
        {
            _cache.Remove(CompanyCultureCacheKeys.ForCompany(childId));
        }
    }

    private static IReadOnlyDictionary<int, int> PadPersonalityDefaults(IReadOnlyDictionary<int, int> answers)
    {
        var map = answers.ToDictionary(kv => kv.Key, kv => kv.Value);
        for (var i = 13; i <= 18; i++)
        {
            map.TryAdd(i, 3); // neutral when employer skips personality tone items
        }

        return map;
    }

    private static CompanyCultureStateDto ToDto(CompanyCultureProfile? row)
    {
        if (row is null)
        {
            return new CompanyCultureStateDto(
                CandidateCompetencyStatuses.Draft,
                new Dictionary<int, int>(),
                null,
                null,
                CompanyCultureSources.Full);
        }

        var scores = new CulturePersonalityScores(
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

        return new CompanyCultureStateDto(
            row.Status,
            CulturePersonalityCatalog.ParseAnswers(row.AnswersJson),
            row.Status == CandidateCompetencyStatuses.Completed ? scores : null,
            row.CompletedAtUtc,
            row.Source);
    }
}
