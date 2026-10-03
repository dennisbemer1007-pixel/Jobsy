using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Media;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateMatchSnapshotService : ICandidateMatchSnapshotService
{
    public static readonly TimeSpan MatchContextCacheTtl = TimeSpan.FromMinutes(5);
    private const string ContextCachePrefix = "cand-match-ctx:";

    private readonly JobsyDbContext _db;
    private readonly IVacancyDiscoveryIndex _discovery;
    private readonly IProfileVacancyMatchService _matches;
    private readonly ICandidateInsightsQueue _queue;
    private readonly IMemoryCache _cache;

    public CandidateMatchSnapshotService(
        JobsyDbContext db,
        IVacancyDiscoveryIndex discovery,
        IProfileVacancyMatchService matches,
        ICandidateInsightsQueue queue,
        IMemoryCache cache)
    {
        _db = db;
        _discovery = discovery;
        _matches = matches;
        _queue = queue;
        _cache = cache;
    }

    public async Task<(IReadOnlyList<CandidateMatchedVacancyDto> Matches, string InsightsStatus)> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateMatchSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (row is null)
        {
            _queue.TryEnqueue(userId);
            return ([], InsightsStatuses.Updating);
        }

        var matches = SanitizeMatches(CandidateMatchSnapshotJson.Deserialize(row.MatchesJson));
        if (InsightsStatuses.IsUpdating(row.Status))
        {
            _queue.TryEnqueue(userId);
            return (matches, InsightsStatuses.Updating);
        }

        var indexStamp = _discovery.LastRefreshedAtUtc;
        var staleIndex = indexStamp is DateTime idx && row.ComputedAtUtc < idx;
        if (staleIndex)
        {
            _queue.TryEnqueue(userId);
            return (matches, InsightsStatuses.Updating);
        }

        return (matches, InsightsStatuses.Ready);
    }

    public async Task SaveComputedAsync(
        Guid userId,
        IReadOnlyList<CandidateMatchedVacancyDto> matches,
        string inputFingerprint,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var row = await _db.CandidateMatchSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = new CandidateMatchSnapshot
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            _db.CandidateMatchSnapshots.Add(row);
        }

        row.MatchesJson = CandidateMatchSnapshotJson.Serialize(SanitizeMatches(matches));
        row.InputFingerprint = inputFingerprint;
        row.ComputedAtUtc = now;
        row.Status = InsightsStatuses.Ready;
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateContextCache(userId);
    }

    public Task<string> ComputeInputFingerprintAsync(Guid userId, CancellationToken cancellationToken = default)
        => ComputeInputFingerprintCheapAsync(userId, cancellationToken);

    /// <summary>Fingerprint from stored test/prefs rows only (no vacancy scoring).</summary>
    private async Task<string> ComputeInputFingerprintCheapAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userRow = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.PreferencesJson })
            .FirstOrDefaultAsync(cancellationToken);
        if (userRow is null)
        {
            return "none";
        }

        var prefs = MatchingProfileMapper.DeserializePrefs(userRow.PreferencesJson);
        var competency = await _db.CandidateCompetencies.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Status,
                c.AnswersJson,
                c.SamenwerkenPercent,
                c.ResultaatgerichtheidPercent,
                c.StressbestendigheidPercent,
                c.InnovatiePercent,
                c.ExtraversiePercent
            })
            .FirstOrDefaultAsync(cancellationToken);
        var career = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Status,
                c.AnswersJson,
                c.RealisticPercent,
                c.InvestigativePercent,
                c.ArtisticPercent,
                c.SocialPercent,
                c.EnterprisingPercent,
                c.ConventionalPercent
            })
            .FirstOrDefaultAsync(cancellationToken);
        var culture = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Status,
                c.AnswersJson,
                c.AutonomyPercent,
                c.InformalPercent,
                c.CollaborationPercent,
                c.FlexibilityPercent,
                c.InnovationPercent,
                c.PeopleFirstPercent,
                c.OpennessPercent,
                c.ConscientiousnessPercent,
                c.ExtraversionPercent,
                c.AgreeablenessPercent,
                c.EmotionalStabilityPercent
            })
            .FirstOrDefaultAsync(cancellationToken);
        var values = await _db.CandidateValuesProfiles.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Status,
                c.AnswersJson,
                c.AutonomyPercent,
                c.ConnectionPercent,
                c.AchievementPercent,
                c.StabilityPercent,
                c.ImpactPercent
            })
            .FirstOrDefaultAsync(cancellationToken);

        var competencies = competency is null
            ? null
            : ProvisionalAssessmentScores.ResolveCompetency(
                competency.Status,
                competency.AnswersJson,
                competency.SamenwerkenPercent,
                competency.ResultaatgerichtheidPercent,
                competency.StressbestendigheidPercent,
                competency.InnovatiePercent,
                competency.ExtraversiePercent).Scores;
        var riasec = career is null
            ? null
            : ProvisionalAssessmentScores.ResolveCareer(
                career.Status,
                career.AnswersJson,
                career.RealisticPercent,
                career.InvestigativePercent,
                career.ArtisticPercent,
                career.SocialPercent,
                career.EnterprisingPercent,
                career.ConventionalPercent).Scores;
        CulturePersonalityScores? storedCulture = null;
        if (culture is not null && CandidateCompetencyStatuses.IsCompleted(culture.Status))
        {
            storedCulture = new CulturePersonalityScores(
                culture.AutonomyPercent,
                culture.InformalPercent,
                culture.CollaborationPercent,
                culture.FlexibilityPercent,
                culture.InnovationPercent,
                culture.PeopleFirstPercent,
                culture.OpennessPercent,
                culture.ConscientiousnessPercent,
                culture.ExtraversionPercent,
                culture.AgreeablenessPercent,
                culture.EmotionalStabilityPercent);
        }

        var cultureScores = culture is null
            ? null
            : ProvisionalAssessmentScores.ResolveCulture(
                culture.Status, culture.AnswersJson, storedCulture).Scores;
        var valuesScores = values is null
            ? null
            : ProvisionalAssessmentScores.ResolveValues(
                values.Status,
                values.AnswersJson,
                values.AutonomyPercent,
                values.ConnectionPercent,
                values.AchievementPercent,
                values.StabilityPercent,
                values.ImpactPercent).Scores;

        return CandidateInsightsFingerprint.ForMatches(
            competencies is { IsComplete: true } ? competencies : null,
            riasec is { IsComplete: true } ? riasec : null,
            cultureScores is { IsComplete: true } ? cultureScores : null,
            valuesScores is { IsComplete: true } ? valuesScores : null,
            prefs,
            userRow.PreferencesJson);
    }

    public async Task<IReadOnlyList<CandidateMatchedVacancyDto>> ComputeLiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var context = await GetOrLoadContextAsync(userId, cancellationToken);
        if (context is null)
        {
            return [];
        }

        var vacancies = await _discovery.GetActiveAsync(cancellationToken);
        var transport = TravelReach.Fastest(TransportLabels.ParseMany(context.Prefs.PreferredTransport));
        var scored = await _matches.ScoreAsync(
            context,
            vacancies.Select(vacancy =>
            {
                int? travelMinutes = null;
                if (context.HomeLatitude is double lat && context.HomeLongitude is double lng)
                {
                    var estimate = TravelReach.Estimate(
                        lat,
                        lng,
                        vacancy.Latitude,
                        vacancy.Longitude,
                        transport);
                    travelMinutes = estimate.TravelMinutes;
                }

                return (vacancy, travelMinutes);
            }),
            cancellationToken);

        var ranked = ProfileVacancyMatchCalculator.RankScored(scored.Values);
        var byId = vacancies.ToDictionary(v => v.Id);
        var result = new List<CandidateMatchedVacancyDto>(ranked.Count);
        foreach (var match in ranked)
        {
            if (!byId.TryGetValue(match.VacancyId, out var vacancy))
            {
                continue;
            }

            var why = match.Why.Select(w => w.Text).ToList();
            if (match.IsBroadMatch
                && !string.IsNullOrWhiteSpace(match.MatchRationale)
                && !why.Contains(match.MatchRationale, StringComparer.Ordinal))
            {
                why.Insert(0, match.MatchRationale);
            }

            var imageUrl = VacancyImageUrls.ForCard(
                vacancy.ImageUrl,
                vacancy.CompanyLogoUrl,
                vacancy.Id,
                vacancy.WorkTypeLabelList?.FirstOrDefault());
            var logoUrl = VacancyImageUrls.Normalize(vacancy.CompanyLogoUrl);
            result.Add(new CandidateMatchedVacancyDto(
                vacancy.Id,
                vacancy.Title,
                vacancy.CompanyName,
                imageUrl,
                logoUrl,
                match.TotalPercent,
                match.ColorBand,
                why,
                match.Gaps.Select(g => g.Text).ToList(),
                match.IsBroadMatch,
                match.MatchRationale));
        }

        var dislikeCodes = await LoadDislikeCodesAsync(userId, cancellationToken);
        var nightShiftIds = vacancies
            .Where(v => v.LegalNightShift23To06 == true)
            .Select(v => v.Id)
            .ToHashSet();
        return DislikeMatchRules.DownRankNightShifts(
            result,
            dislikeCodes,
            m => nightShiftIds.Contains(m.Id));
    }

    private async Task<IReadOnlyList<string>> LoadDislikeCodesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var json = await _db.CandidatePrivatePreferences.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.DislikesJson)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    public void InvalidateContextCache(Guid userId)
        => _cache.Remove(ContextCachePrefix + userId);

    public async Task MarkInputsStaleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateMatchSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (row is null)
        {
            return;
        }

        if (InsightsStatuses.IsUpdating(row.Status))
        {
            return;
        }

        row.Status = InsightsStatuses.Updating;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<CandidateMatchedVacancyDto> SanitizeMatches(
        IReadOnlyList<CandidateMatchedVacancyDto> matches)
    {
        if (matches.Count == 0)
        {
            return matches;
        }

        var list = new List<CandidateMatchedVacancyDto>(matches.Count);
        foreach (var match in matches)
        {
            list.Add(match with
            {
                ImageUrl = VacancyImageUrls.ForCard(match.ImageUrl, match.CompanyLogoUrl, match.Id, null),
                CompanyLogoUrl = VacancyImageUrls.Normalize(match.CompanyLogoUrl)
            });
        }

        return list;
    }

    private async Task<ProfileVacancyMatchContext?> GetOrLoadContextAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var key = ContextCachePrefix + userId;
        if (_cache.TryGetValue(key, out ProfileVacancyMatchContext? cached) && cached is not null)
        {
            return cached;
        }

        var context = await _matches.TryLoadForUserIdAsync(userId, cancellationToken);
        if (context is not null)
        {
            _cache.Set(key, context, MatchContextCacheTtl);
        }

        return context;
    }
}
