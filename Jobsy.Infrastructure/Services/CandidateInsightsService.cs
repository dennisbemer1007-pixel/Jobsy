using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateInsightsService : ICandidateInsightsService
{
    public static readonly int[] AllowedRadiiKm = [10, 20, 30];
    public static readonly int[] AllowedPeriodsDays = [30, 90, 365];

    public const string LockedDreamJobs4To10 = "dreamJobs4to10";
    public const string LockedDna = "dna";
    public const string LockedStory5To10 = "story5to10";

    private const int CohortCap = 20_000;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
    private static readonly JsonSerializerOptions MatchJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _authz;
    private readonly IUserLookupService _users;
    private readonly ITokenLedgerService _tokens;
    private readonly IPlatformFeatureService _features;
    private readonly IUserNotificationService _notifications;
    private readonly TimeProvider _clock;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CandidateInsightsService> _logger;

    public CandidateInsightsService(
        JobsyDbContext db,
        ICompanyAuthorizationService authz,
        IUserLookupService users,
        ITokenLedgerService tokens,
        IPlatformFeatureService features,
        IUserNotificationService notifications,
        TimeProvider clock,
        IMemoryCache cache,
        ILogger<CandidateInsightsService> logger)
    {
        _db = db;
        _authz = authz;
        _users = users;
        _tokens = tokens;
        _features = features;
        _notifications = notifications;
        _clock = clock;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _features.GetAsync(cancellationToken);
        return snap.CandidateInsightsEnabled;
    }

    public async Task<IReadOnlyList<CandidateInsightsBranchDto>> GetBranchesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        EnsureAllowedRole(principal);
        var scope = await ResolveScopeCompaniesAsync(principal, branchId: null, allowAllUnion: true, cancellationToken);
        var isBranch = RoleClaimMatching.HasRole(principal, JobsyRoles.BranchManager);
        return scope.Companies
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new CandidateInsightsBranchDto(c.Id, c.Name, IsLocked: isBranch))
            .ToList();
    }

    public async Task<CandidateInsightsDto> GetInsightsAsync(
        ClaimsPrincipal principal,
        Guid? branchId,
        int radiusKm,
        int periodDays,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        EnsureAllowedRole(principal);
        if (!AllowedRadiiKm.Contains(radiusKm) || !AllowedPeriodsDays.Contains(periodDays))
        {
            throw new ArgumentException("Invalid radiusKm or period.");
        }

        // AuthZ before cache — never serve a cached DTO to an unauthorized caller.
        var scope = await ResolveScopeCompaniesAsync(principal, branchId, allowAllUnion: true, cancellationToken);
        var featureSnap = await _features.GetAsync(cancellationToken);
        var coverage = await LoadCoverageAsync(scope.Companies, cancellationToken);
        var offer = await BuildOfferAsync(principal, scope.Companies, coverage, featureSnap, cancellationToken);
        var isFullAccess = coverage.IsFull;

        var coveredIds = string.Join(',', scope.Companies
            .Where(c => IsCompanyCovered(c, coverage, scope.Companies))
            .Select(c => c.Id)
            .OrderBy(id => id)
            .Select(id => id.ToString("D")));
        var cacheKey = BuildCacheKey(scope.Companies.Select(c => c.Id), radiusKm, periodDays, isFullAccess, coveredIds);
        if (_cache.TryGetValue(cacheKey, out CandidateInsightsDto? cached) && cached is not null)
        {
            // Coverage/offer are request-specific (balance, role) — rebuild envelope.
            return cached with
            {
                Coverage = ToCoverageDto(coverage, featureSnap.CandidateInsightsUnlockPerBranch),
                Offer = offer
            };
        }

        var dto = await BuildAsync(scope.Companies, radiusKm, periodDays, isFullAccess, coverage, featureSnap, offer, cancellationToken);
        // Cache without live offer/balance — store with coverage snapshot; offer reattached above on hit.
        _cache.Set(cacheKey, dto with
        {
            Offer = offer with { WalletBalance = null, CanUnlock = false, CannotUnlockReason = null }
        }, CacheTtl);
        return dto;
    }

    internal static string BuildCacheKey(
        IEnumerable<Guid> branchIds,
        int radiusKm,
        int periodDays,
        bool isFullAccess,
        string coveredIds = "")
    {
        var sorted = string.Join(',', branchIds.OrderBy(id => id).Select(id => id.ToString("D")));
        return $"candidate-insights:{sorted}:r{radiusKm}:p{periodDays}:f{(isFullAccess ? 1 : 0)}:c{coveredIds}";
    }

    // Legacy overload kept for existing unit tests that call BuildCacheKey with 4 args.
    internal static string BuildCacheKey(IEnumerable<Guid> branchIds, int radiusKm, int periodDays, bool isFullAccess)
        => BuildCacheKey(branchIds, radiusKm, periodDays, isFullAccess, "");

    private static bool IsCompanyCovered(Company company, InsightsCoverage coverage, IReadOnlyList<Company> scope)
    {
        // Used only for cache key differentiation on partial coverage.
        if (coverage.IsFull)
        {
            return true;
        }

        // Partial: we still serve free data for all; covered ids help invalidate when another branch unlocks.
        return coverage.CoveredCount > 0 && scope.Any(c => c.Id == company.Id);
    }

    private async Task<CandidateInsightsDto> BuildAsync(
        IReadOnlyList<Company> companies,
        int radiusKm,
        int periodDays,
        bool isFullAccess,
        InsightsCoverage coverage,
        PlatformFeatureSnapshot featureSnap,
        InsightsOfferDto offer,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var periodStart = now.AddDays(-periodDays);
        var active30Start = now.AddDays(-30);

        var origins = companies
            .Where(c => c.Location is not null)
            .Select(c => c.Location!)
            .ToList();

        var cohortUsers = await LoadCohortUsersAsync(origins, radiusKm, periodStart, cancellationToken);
        var cohortIds = cohortUsers.Select(u => u.Id).ToList();
        var cohortSize = cohortIds.Count;

        var locked = isFullAccess ? Array.Empty<string>() : InsightsLockedKeys.All.ToArray();
        var coverageDto = ToCoverageDto(coverage, featureSnap.CandidateInsightsUnlockPerBranch);

        var scope = new InsightsScope(
            companies.Select(c => new InsightsBranchRef(
                c.Id,
                c.Name,
                c.Location?.Latitude ?? 0,
                c.Location?.Longitude ?? 0)).ToList(),
            radiusKm,
            periodDays,
            now,
            isFullAccess);

        if (!CandidateInsightsPrivacy.MeetsThreshold(cohortSize))
        {
            return InsufficientDto(scope, locked, coverageDto, offer);
        }

        var prefsByUser = cohortUsers.ToDictionary(
            u => u.Id,
            u => MatchingProfileMapper.DeserializePrefs(u.PreferencesJson));

        // Free KPIs always computed
        var (candStatus, candValue) = CandidateInsightsPrivacy.SuppressCount(cohortSize);

        var hourMidpoints = new List<decimal>();
        var plus32 = 0;
        foreach (var prefs in prefsByUser.Values)
        {
            var min = prefs.MinHoursPerWeek;
            var max = prefs.MaxHoursPerWeek;
            if (min is null && max is null)
            {
                continue;
            }

            var lo = min ?? max!.Value;
            var hi = max ?? min!.Value;
            hourMidpoints.Add((lo + hi) / 2m);
            if ((max ?? hi) >= 32
                || (prefs.AvailabilityPresets?.Any(p =>
                        string.Equals(p, AvailabilityPresetRules.Fulltime, StringComparison.OrdinalIgnoreCase)) ?? false))
            {
                plus32++;
            }
        }

        var avgHoursRaw = hourMidpoints.Count == 0
            ? 0
            : (int)Math.Round((double)hourMidpoints.Average(), MidpointRounding.AwayFromZero);
        var avgHours = CandidateInsightsPrivacy.MeetsThreshold(hourMidpoints.Count)
            ? new SuppressedCount(CandidateInsightsPrivacy.StatusOk, Math.Max(0, avgHoursRaw))
            : new SuppressedCount(CandidateInsightsPrivacy.StatusInsufficient, null);

        // Locked sections: do not compute when free (D8 / cheaper + no leak).
        SuppressedCount? plus32Kpi = null;
        SuppressedCount? active30Kpi = null;
        SuppressedCount? matchingKpi = null;
        IReadOnlyList<RankedItem> dreamGroups = [];
        InsightsDistribution? workFields = null;
        InsightsDistribution? dna = null;
        InsightsDistribution? competences = null;
        InsightsDistribution? personality = null;
        InsightsDistribution? priorities = null;
        InsightsDistribution? workKinds = null;
        IReadOnlyList<DensityCell> density = [];
        List<VacancyReach> vacancyReach = [];
        InsightsTrend? trend = null;

        if (isFullAccess)
        {
            var (p32s, p32v) = CandidateInsightsPrivacy.SuppressCount(plus32);
            plus32Kpi = new SuppressedCount(p32s, p32v);
            var active30 = cohortUsers.Count(u => u.LastLoginAtUtc is DateTime t && t >= active30Start);
            var (a30s, a30v) = CandidateInsightsPrivacy.SuppressCount(active30);
            active30Kpi = new SuppressedCount(a30s, a30v);

            var careerPlans = await _db.CandidateCareerPlans.AsNoTracking()
                .Where(p => cohortIds.Contains(p.UserId))
                .Select(p => new { p.UserId, p.DreamKey, p.DreamTitle })
                .ToListAsync(cancellationToken);

            var competencies = await _db.CandidateCompetencies.AsNoTracking()
                .Where(c => cohortIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
                .ToListAsync(cancellationToken);

            var careerInterests = await _db.CandidateCareerInterests.AsNoTracking()
                .Where(c => cohortIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
                .ToListAsync(cancellationToken);

            var personalities = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
                .Where(c => cohortIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
                .ToListAsync(cancellationToken);

            var values = await _db.CandidateValuesProfiles.AsNoTracking()
                .Where(c => cohortIds.Contains(c.UserId) && c.Status == CandidateCompetencyStatuses.Completed)
                .ToListAsync(cancellationToken);

            var snapshots = await _db.CandidateMatchSnapshots.AsNoTracking()
                .Where(s => cohortIds.Contains(s.UserId))
                .Select(s => new { s.UserId, s.MatchesJson })
                .ToListAsync(cancellationToken);

            var companyIds = companies.Select(c => c.Id).ToList();
            var vacancies = await _db.Vacancies.AsNoTracking()
                .Where(v => companyIds.Contains(v.CompanyId) && v.Status == VacancyStatus.Active)
                .Select(v => new
                {
                    v.Id,
                    v.Title,
                    v.CompanyId,
                    v.HourlyWage,
                    v.SalaryTableId,
                    v.FlexibleTimes,
                    v.CulturePillarsJson
                })
                .ToListAsync(cancellationToken);

            var companyNames = companies.ToDictionary(c => c.Id, c => c.Name);

            var vacancyIds = vacancies.Select(v => v.Id).ToHashSet();
            var matchingCandidateIds = new HashSet<Guid>();
            var perVacancyMatch = vacancies.ToDictionary(v => v.Id, _ => new HashSet<Guid>());
            foreach (var snap in snapshots)
            {
                foreach (var match in ParseMatches(snap.MatchesJson))
                {
                    if (match.MatchPercent < 70 || !vacancyIds.Contains(match.Id))
                    {
                        continue;
                    }

                    matchingCandidateIds.Add(snap.UserId);
                    perVacancyMatch[match.Id].Add(snap.UserId);
                }
            }

            var (matchS, matchV) = CandidateInsightsPrivacy.SuppressCount(matchingCandidateIds.Count);
            matchingKpi = new SuppressedCount(matchS, matchV);

            dreamGroups = careerPlans
                .Where(p => !string.IsNullOrWhiteSpace(p.DreamKey))
                .GroupBy(p => p.DreamKey.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var label = g
                        .Select(x => x.DreamTitle?.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .GroupBy(t => t!, StringComparer.OrdinalIgnoreCase)
                        .OrderByDescending(x => x.Count())
                        .Select(x => x.Key)
                        .FirstOrDefault() ?? g.Key;
                    return (Key: g.Key, Label: label, Count: g.Select(x => x.UserId).Distinct().Count());
                })
                .Where(x => CandidateInsightsPrivacy.MeetsThreshold(x.Count))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .Select(x =>
                {
                    var (st, val) = CandidateInsightsPrivacy.SuppressCount(x.Count);
                    return new RankedItem(x.Label, new SuppressedCount(st, val));
                })
                .ToList();

            var roleCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var prefs in prefsByUser.Values)
            {
                foreach (var role in prefs.Roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var key = role.Trim();
                    roleCounts[key] = roleCounts.GetValueOrDefault(key) + 1;
                }
            }

            workFields = BuildDistributionFromCounts(roleCounts, cohortSize, take: 8);
            dna = BuildRiasecDistribution(careerInterests, cohortSize);
            competences = BuildCompetencyDistribution(competencies, cohortSize);
            personality = BuildPersonalityDistribution(personalities, cohortSize);

            var travel = prefsByUser.Values.Count(p => p.MaxTravelMinutes is int m && m <= 20);
            var flex = prefsByUser.Values.Count(p => p.FlexibleTimes == true);
            var connection = values.Count(v => v.ConnectionPercent is int c && c >= 60);
            var stability = values.Count(v => v.StabilityPercent is int s && s >= 60);
            priorities = new InsightsDistribution(
                CandidateInsightsPrivacy.StatusOk,
                new[]
                {
                    Bucket("travel", "reistijd", travel, cohortSize),
                    Bucket("flexibility", "flexibiliteit", flex, cohortSize),
                    Bucket("culture", "sfeer_cultuur", connection, cohortSize),
                    Bucket("stability", "zekerheid", stability, cohortSize)
                });

            var fulltime = 0;
            var parttime = 0;
            var sideJob = 0;
            var internship = 0;
            var volunteer = 0;
            foreach (var prefs in prefsByUser.Values)
            {
                var presets = prefs.AvailabilityPresets ?? [];
                var maxH = prefs.MaxHoursPerWeek;
                var minH = prefs.MinHoursPerWeek;
                var isFt = presets.Any(p => string.Equals(p, AvailabilityPresetRules.Fulltime, StringComparison.OrdinalIgnoreCase))
                           || maxH >= 32;
                var isPt = presets.Any(p => string.Equals(p, AvailabilityPresetRules.Parttime, StringComparison.OrdinalIgnoreCase))
                           || (maxH is >= 12 and <= 31)
                           || (minH is >= 12 and <= 31 && maxH is null or < 32);
                var isSide = presets.Any(p => p is AvailabilityPresetRules.School
                        or AvailabilityPresetRules.Weekend
                        or AvailabilityPresetRules.Evening
                        or AvailabilityPresetRules.Holiday)
                    || (maxH is not null && maxH < 12);

                if (isFt)
                {
                    fulltime++;
                }
                else if (isPt)
                {
                    parttime++;
                }
                else if (isSide)
                {
                    sideJob++;
                }

                foreach (var role in prefs.Roles)
                {
                    var kind = VacancyKindLabels.ParseOrDefault(role);
                    if (kind == VacancyKind.Internship)
                    {
                        internship++;
                        break;
                    }

                    if (kind == VacancyKind.Volunteer)
                    {
                        volunteer++;
                        break;
                    }
                }
            }

            workKinds = new InsightsDistribution(
                CandidateInsightsPrivacy.StatusOk,
                new[]
                {
                    Bucket("fulltime", "fulltime", fulltime, cohortSize),
                    Bucket("parttime", "parttime", parttime, cohortSize),
                    Bucket("bijbaan", "bijbaan", sideJob, cohortSize),
                    Bucket("stage", "stage", internship, cohortSize),
                    Bucket("vrijwilliger", "vrijwilliger", volunteer, cohortSize)
                });

            var cellCounts = new Dictionary<(int X, int Y), int>();
            foreach (var user in cohortUsers)
            {
                if (user.HomeLocation is null)
                {
                    continue;
                }

                var cell = CandidateInsightsDensityGrid.ToCell(user.HomeLocation);
                cellCounts[cell] = cellCounts.GetValueOrDefault(cell) + 1;
            }

            var eligibleCells = cellCounts
                .Where(kv => CandidateInsightsPrivacy.MeetsThreshold(kv.Value))
                .OrderByDescending(kv => kv.Value)
                .ToList();
            var densityList = new List<DensityCell>();
            if (eligibleCells.Count > 0)
            {
                var valuesSorted = eligibleCells.Select(c => c.Value).OrderBy(v => v).ToList();
                var t1 = Percentile(valuesSorted, 1.0 / 3.0);
                var t2 = Percentile(valuesSorted, 2.0 / 3.0);
                foreach (var (cell, count) in eligibleCells)
                {
                    var band = count <= t1 ? 1 : count <= t2 ? 2 : 3;
                    var (lat, lng) = CandidateInsightsDensityGrid.CellCenter(cell.X, cell.Y);
                    densityList.Add(new DensityCell(
                        CandidateInsightsDensityGrid.CellId(cell.X, cell.Y),
                        lat,
                        lng,
                        band));
                }
            }

            density = densityList;

            var tipKeys = BuildTipKeys(priorities);
            vacancyReach = vacancies
                .Select(v =>
                {
                    var matchCount = perVacancyMatch[v.Id].Count;
                    var (st, val) = CandidateInsightsPrivacy.SuppressCount(matchCount);
                    var pillars = CulturePillarCatalog.Deserialize(v.CulturePillarsJson);
                    var checklist = new InsightsVacancyChecklist(
                        SalaryMentioned: v.HourlyWage > 0 || v.SalaryTableId is not null,
                        FlexibleHours: v.FlexibleTimes,
                        AtmosphereAndTeam: CulturePillarCatalog.HasProfile(pillars));
                    return new
                    {
                        Reach = new VacancyReach(
                            v.Id,
                            v.Title,
                            companyNames.GetValueOrDefault(v.CompanyId, ""),
                            new SuppressedCount(st, val),
                            new InsightsTips(tipKeys, checklist)),
                        MatchCount = matchCount
                    };
                })
                .OrderByDescending(x => x.MatchCount)
                .ThenBy(x => x.Reach.Title, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Reach)
                .ToList();

            if (vacancyReach.Count > 0)
            {
                vacancyReach = vacancyReach
                    .Select((v, i) => i == 0
                        ? v
                        : v with { Tips = new InsightsTips(tipKeys, Checklist: null) })
                    .ToList();
            }

            trend = new InsightsTrend("insufficient_history", "Insights.Trend.InsufficientHistory");
            // TODO(D8): reminder e-mail when unlock expires within RenewWindowDays
        }

        var kpis = new InsightsKpis(
            new SuppressedCount(candStatus, candValue),
            avgHours,
            plus32Kpi,
            active30Kpi,
            matchingKpi);

        return new CandidateInsightsDto(
            scope,
            kpis,
            dreamGroups,
            workFields,
            dna,
            competences,
            personality,
            priorities,
            workKinds,
            density,
            vacancyReach,
            trend,
            locked,
            coverageDto,
            offer);
    }

    private static CandidateInsightsDto InsufficientDto(
        InsightsScope scope,
        IReadOnlyList<string> locked,
        InsightsCoverageDto coverage,
        InsightsOfferDto offer)
    {
        var insuff = new SuppressedCount(CandidateInsightsPrivacy.StatusInsufficient, null);
        return new CandidateInsightsDto(
            scope,
            new InsightsKpis(
                insuff,
                insuff,
                scope.IsFullAccess ? insuff : null,
                scope.IsFullAccess ? insuff : null,
                scope.IsFullAccess ? insuff : null),
            [],
            scope.IsFullAccess ? new InsightsDistribution(CandidateInsightsPrivacy.StatusInsufficient, []) : null,
            scope.IsFullAccess ? new InsightsDistribution(CandidateInsightsPrivacy.StatusInsufficient, []) : null,
            scope.IsFullAccess ? new InsightsDistribution(CandidateInsightsPrivacy.StatusInsufficient, []) : null,
            scope.IsFullAccess ? new InsightsDistribution(CandidateInsightsPrivacy.StatusInsufficient, []) : null,
            scope.IsFullAccess ? new InsightsDistribution(CandidateInsightsPrivacy.StatusInsufficient, []) : null,
            scope.IsFullAccess ? new InsightsDistribution(CandidateInsightsPrivacy.StatusInsufficient, []) : null,
            [],
            [],
            scope.IsFullAccess
                ? new InsightsTrend("insufficient_history", "Insights.Trend.InsufficientHistory")
                : null,
            locked,
            coverage,
            offer);
    }

    private static InsightsDistributionBucket Bucket(string key, string label, int numerator, int denominator)
    {
        var (st, pct) = CandidateInsightsPrivacy.SuppressPercent(numerator, denominator);
        return new InsightsDistributionBucket(key, label, new SuppressedPercent(st, pct));
    }

    private static InsightsDistribution BuildDistributionFromCounts(
        Dictionary<string, int> counts,
        int cohortSize,
        int take)
    {
        var buckets = counts
            .Where(kv => CandidateInsightsPrivacy.MeetsThreshold(kv.Value))
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(kv => Bucket(kv.Key, kv.Key, kv.Value, cohortSize))
            .ToList();
        return new InsightsDistribution(
            buckets.Count == 0 ? CandidateInsightsPrivacy.StatusInsufficient : CandidateInsightsPrivacy.StatusOk,
            buckets);
    }

    private static InsightsDistribution BuildRiasecDistribution(
        List<CandidateCareerInterest> interests,
        int cohortSize)
    {
        var keys = new[]
        {
            ("R", "Realistic", (Func<CandidateCareerInterest, int?>)(i => i.RealisticPercent)),
            ("I", "Investigative", i => i.InvestigativePercent),
            ("A", "Artistic", i => i.ArtisticPercent),
            ("S", "Social", i => i.SocialPercent),
            ("E", "Enterprising", i => i.EnterprisingPercent),
            ("C", "Conventional", i => i.ConventionalPercent)
        };

        var strongest = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in interests)
        {
            string? bestKey = null;
            var best = -1;
            foreach (var (code, label, getter) in keys)
            {
                var v = getter(row) ?? -1;
                if (v > best)
                {
                    best = v;
                    bestKey = label;
                }
            }

            if (bestKey is null || best < 0)
            {
                continue;
            }

            strongest[bestKey] = strongest.GetValueOrDefault(bestKey) + 1;
        }

        return BuildDistributionFromCounts(strongest, cohortSize, take: 6);
    }

    private static InsightsDistribution BuildCompetencyDistribution(
        List<CandidateCompetency> rows,
        int cohortSize)
    {
        var dims = new (string Key, Func<CandidateCompetency, int?> Getter)[]
        {
            (CompetencyTestCatalog.Samenwerken, c => c.SamenwerkenPercent),
            (CompetencyTestCatalog.Resultaatgerichtheid, c => c.ResultaatgerichtheidPercent),
            (CompetencyTestCatalog.Stressbestendigheid, c => c.StressbestendigheidPercent),
            (CompetencyTestCatalog.Innovatie, c => c.InnovatiePercent),
            (CompetencyTestCatalog.Extraversie, c => c.ExtraversiePercent)
        };
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (key, getter) in dims)
        {
            counts[key] = rows.Count(r => getter(r) is int v && v >= 60);
        }

        return new InsightsDistribution(
            CandidateInsightsPrivacy.StatusOk,
            dims.Select(d => Bucket(d.Key, d.Key, counts[d.Key], cohortSize)).ToList());
    }

    private static InsightsDistribution BuildPersonalityDistribution(
        List<CandidateCulturePersonalityProfile> rows,
        int cohortSize)
    {
        var dims = new (string Key, Func<CandidateCulturePersonalityProfile, int?> Getter)[]
        {
            (CulturePersonalityCatalog.Autonomy, r => r.AutonomyPercent),
            (CulturePersonalityCatalog.Informal, r => r.InformalPercent),
            (CulturePersonalityCatalog.Collaboration, r => r.CollaborationPercent),
            (CulturePersonalityCatalog.Flexibility, r => r.FlexibilityPercent),
            (CulturePersonalityCatalog.Innovation, r => r.InnovationPercent),
            (CulturePersonalityCatalog.PeopleFirst, r => r.PeopleFirstPercent)
        };
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (key, getter) in dims)
        {
            counts[key] = rows.Count(r => getter(r) is int v && v >= 60);
        }

        return new InsightsDistribution(
            CandidateInsightsPrivacy.StatusOk,
            dims.Select(d => Bucket(d.Key, d.Key, counts[d.Key], cohortSize)).ToList());
    }

    private static List<string> BuildTipKeys(InsightsDistribution priorities)
    {
        var tips = new List<string>();
        foreach (var bucket in priorities.Buckets
                     .Where(b => b.Share.Status == CandidateInsightsPrivacy.StatusOk)
                     .OrderByDescending(b => b.Share.Percent ?? 0))
        {
            tips.Add(bucket.Key switch
            {
                "travel" => "Insights.Tip.Travel",
                "flexibility" => "Insights.Tip.Flexibility",
                "culture" => "Insights.Tip.Culture",
                "stability" => "Insights.Tip.Stability",
                _ => "Insights.Tip.Generic"
            });
            if (tips.Count >= 2)
            {
                break;
            }
        }

        if (tips.Count == 0)
        {
            tips.Add("Insights.Tip.Generic");
        }

        return tips;
    }

    private static int Percentile(List<int> sortedAscending, double p)
    {
        if (sortedAscending.Count == 0)
        {
            return 0;
        }

        var idx = (int)Math.Floor((sortedAscending.Count - 1) * p);
        return sortedAscending[Math.Clamp(idx, 0, sortedAscending.Count - 1)];
    }

    private static List<CandidateMatchedVacancyDto> ParseMatches(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CandidateMatchedVacancyDto>>(json, MatchJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<List<User>> LoadCohortUsersAsync(
        List<GeoPoint> origins,
        int radiusKm,
        DateTime periodStart,
        CancellationToken cancellationToken)
    {
        if (origins.Count == 0)
        {
            return [];
        }

        HashSet<Guid> spatialIds;
        if (_db.Database.IsNpgsql())
        {
            spatialIds = [];
            var role = (int)UserRole.Candidate;
            var degrees = radiusKm / 111.32;
            foreach (var origin in origins)
            {
                var ids = await _db.Database
                    .SqlQueryRaw<Guid>(
                        """
                        SELECT u."Id" AS "Value"
                        FROM "Users" u
                        WHERE u."IsActive" = TRUE
                          AND u."Role" = {0}
                          AND u."HomeLocation" IS NOT NULL
                          AND u."LastLoginAtUtc" IS NOT NULL
                          AND u."LastLoginAtUtc" >= {1}
                          AND ST_DWithin(
                            u."HomeLocation",
                            ST_SetSRID(ST_MakePoint({2}, {3}), 4326),
                            {4})
                        """,
                        role,
                        periodStart,
                        origin.Longitude,
                        origin.Latitude,
                        degrees)
                    .ToListAsync(cancellationToken);
                foreach (var id in ids)
                {
                    spatialIds.Add(id);
                }
            }
        }
        else
        {
            var candidates = await _db.Users.AsNoTracking()
                .Where(u =>
                    u.Role == UserRole.Candidate
                    && u.IsActive
                    && u.HomeLocation != null
                    && u.LastLoginAtUtc != null
                    && u.LastLoginAtUtc >= periodStart)
                .ToListAsync(cancellationToken);

            spatialIds = candidates
                .Where(u => origins.Any(o => u.HomeLocation is not null
                                             && GeoDistance.IsWithinKm(o, u.HomeLocation, radiusKm)))
                .Select(u => u.Id)
                .ToHashSet();
        }

        if (spatialIds.Count == 0)
        {
            return [];
        }

        if (spatialIds.Count > CohortCap)
        {
            _logger.LogWarning(
                "Candidate insights cohort capped from {Raw} to {Cap}",
                spatialIds.Count,
                CohortCap);
            spatialIds = spatialIds.Take(CohortCap).ToHashSet();
        }

        var users = await _db.Users.AsNoTracking()
            .Where(u => spatialIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users
            .Where(u =>
                u.HomeLocation is not null
                && CandidateConsentRules.HasCurrentTestAiConsent(u)
                && CandidateConsentRules.CanUseCandidateFeatures(u))
            .ToList();
    }

    public async Task<CandidateInsightsUnlockResultDto> UnlockAsync(
        ClaimsPrincipal principal,
        string scope,
        Guid? branchId,
        string idempotencyKey,
        Guid? unlockRequestId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        EnsureAllowedRole(principal);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new CandidateInsightsException("missing_idempotency_key", "Idempotency-Key is verplicht.", 400);
        }

        var key = idempotencyKey.Trim();
        if (key.Length > 128)
        {
            throw new CandidateInsightsException("invalid_idempotency_key", "Idempotency-Key is te lang.", 400);
        }

        var existingByKey = await _db.CandidateInsightsUnlocks.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdempotencyKey == key, cancellationToken);
        if (existingByKey is not null)
        {
            var bal = await _tokens.GetBalanceAsync(existingByKey.WalletCompanyId, cancellationToken);
            return new CandidateInsightsUnlockResultDto(
                existingByKey.Id,
                existingByKey.ExpiresAtUtc,
                0m,
                bal);
        }

        if (RoleClaimMatching.HasRole(principal, JobsyRoles.RegionalManager))
        {
            throw new CandidateInsightsException("read_only", "Regiomanager kan inzichten niet ontgrendelen.", 403);
        }

        var featureSnap = await _features.GetAsync(cancellationToken);
        var perBranch = featureSnap.CandidateInsightsUnlockPerBranch;
        var scopeKind = scope?.Trim().ToLowerInvariant() switch
        {
            "company" => CandidateInsightsUnlockScopeKind.Company,
            "branch" => CandidateInsightsUnlockScopeKind.Branch,
            _ => throw new CandidateInsightsException("invalid_scope", "Scope moet 'company' of 'branch' zijn.", 400)
        };

        if (scopeKind == CandidateInsightsUnlockScopeKind.Company && perBranch)
        {
            // BM may still buy company-wide even when per-branch is on (covers all).
            if (!RoleClaimMatching.HasRole(principal, JobsyRoles.EnterpriseManager))
            {
                throw new CandidateInsightsException("needs_branch_scope", "Ontgrendel per vestiging.", 403);
            }
        }

        if (scopeKind == CandidateInsightsUnlockScopeKind.Branch && branchId is null)
        {
            throw new CandidateInsightsException("needs_branch_scope", "branchId is verplicht voor vestiging-ontgrendeling.", 400);
        }

        var user = await _users.FindByPrincipalAsync(principal, cancellationToken)
                   ?? throw new ForbiddenCompanyAccessException(branchId ?? Guid.Empty);

        Company targetCompany;
        Guid walletCompanyId;
        Guid? spendBranchCompanyId = null;

        if (scopeKind == CandidateInsightsUnlockScopeKind.Branch)
        {
            var bid = branchId!.Value;
            await _authz.EnsureCanAccessCompanyAsync(principal, bid, cancellationToken);
            targetCompany = await _db.Companies.FirstOrDefaultAsync(c => c.Id == bid, cancellationToken)
                            ?? throw new CandidateInsightsException("branch_not_found", "Vestiging niet gevonden.", 404);

            if (RoleClaimMatching.HasRole(principal, JobsyRoles.BranchManager))
            {
                if (!perBranch)
                {
                    throw new CandidateInsightsException("needs_branch_scope", "Ontgrendelen per vestiging staat uit.", 403);
                }

                if (user.CompanyId != bid)
                {
                    throw new CandidateInsightsException("forbidden_branch", "Je mag alleen je eigen vestiging ontgrendelen.", 403);
                }

                // VM pays from allocated branch wallet.
                walletCompanyId = bid;
                spendBranchCompanyId = bid;
            }
            else
            {
                walletCompanyId = CandidateInsightsAccess.ResolveWalletCompanyId(targetCompany);
            }
        }
        else
        {
            if (!RoleClaimMatching.HasRole(principal, JobsyRoles.EnterpriseManager))
            {
                throw new CandidateInsightsException("read_only", "Alleen de bedrijfsmanager ontgrendelt organisatie-breed.", 403);
            }

            var scopeCompanies = await ResolveScopeCompaniesAsync(principal, null, allowAllUnion: true, cancellationToken);
            targetCompany = scopeCompanies.Companies.FirstOrDefault(c => c.ParentCompanyId is null)
                            ?? scopeCompanies.Companies[0];
            // Prefer organisation root: if BM's home is org, use that.
            if (user.CompanyId is Guid homeId)
            {
                var home = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == homeId, cancellationToken);
                if (home is not null && home.ParentCompanyId is null)
                {
                    targetCompany = home;
                }
                else if (home?.ParentCompanyId is Guid parent)
                {
                    var org = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == parent, cancellationToken);
                    if (org is not null)
                    {
                        targetCompany = org;
                    }
                }
            }

            walletCompanyId = CandidateInsightsAccess.ResolveWalletCompanyId(targetCompany);
        }

        var scopeCompanyId = scopeKind == CandidateInsightsUnlockScopeKind.Company
            ? walletCompanyId
            : targetCompany.Id;

        var now = _clock.GetUtcNow().UtcDateTime;
        var active = await _db.CandidateInsightsUnlocks
            .Where(u =>
                u.WalletCompanyId == walletCompanyId
                && u.ScopeKind == scopeKind
                && u.ScopeCompanyId == scopeCompanyId
                && u.ExpiresAtUtc > now)
            .OrderByDescending(u => u.ExpiresAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (active is not null)
        {
            var renewWindowStart = active.ExpiresAtUtc.AddDays(-CandidateInsightsAccess.RenewWindowDays);
            if (now < renewWindowStart)
            {
                var bal = await _tokens.GetBalanceAsync(walletCompanyId, cancellationToken);
                return new CandidateInsightsUnlockResultDto(active.Id, active.ExpiresAtUtc, 0m, bal);
            }
        }

        var durationDays = CandidateInsightsAccess.ClampUnlockDays(featureSnap.CandidateInsightsUnlockDays);
        var expiresAt = active is not null && now >= active.ExpiresAtUtc.AddDays(-CandidateInsightsAccess.RenewWindowDays)
            ? active.ExpiresAtUtc.AddDays(durationDays) // stack
            : now.AddDays(durationDays);

        var cost = await _tokens.GetCostAsync(TokenSpendReason.InsightsUnlock, cancellationToken)
                   ?? CandidateInsightsAccess.DefaultUnlockCostTokens;

        CandidateInsightsUnlock? created = null;
        var note = $"Kandidaatinzichten {(scopeKind == CandidateInsightsUnlockScopeKind.Company ? "organisatie" : "vestiging")} t/m {expiresAt:yyyy-MM-dd}";

        var outcome = await _tokens.TrySpendAsync(
            walletCompanyId,
            TokenSpendReason.InsightsUnlock,
            vacancyId: null,
            actorUserId: user.Id,
            branchCompanyId: spendBranchCompanyId,
            note: note,
            onSuccessBeforeCommit: async ct =>
            {
                var txEntry = _db.ChangeTracker.Entries<TokenTransaction>()
                    .Select(e => e.Entity)
                    .FirstOrDefault(t =>
                        t.CompanyId == walletCompanyId
                        && t.Reason == TokenSpendReason.InsightsUnlock
                        && t.Kind == TokenTransactionKind.Spend
                        && t.Id != Guid.Empty);
                if (txEntry is null)
                {
                    throw new InvalidOperationException("Token transaction missing before unlock insert.");
                }

                created = new CandidateInsightsUnlock
                {
                    Id = Guid.NewGuid(),
                    WalletCompanyId = walletCompanyId,
                    ScopeKind = scopeKind,
                    ScopeCompanyId = scopeCompanyId,
                    UnlockedAtUtc = now,
                    ExpiresAtUtc = expiresAt,
                    PriceTokens = cost,
                    DurationDays = durationDays,
                    ActorUserId = user.Id,
                    TokenTransactionId = txEntry.Id,
                    IdempotencyKey = key
                };
                _db.CandidateInsightsUnlocks.Add(created);
                await Task.CompletedTask;
            },
            cancellationToken: cancellationToken);

        if (!outcome.Succeeded || outcome.Transaction is null)
        {
            throw new CandidateInsightsException(
                "insufficient_tokens",
                outcome.ErrorMessage ?? "Onvoldoende tokens.",
                402);
        }

        if (created is null)
        {
            created = await _db.CandidateInsightsUnlocks
                .FirstOrDefaultAsync(u => u.IdempotencyKey == key, cancellationToken)
                ?? throw new InvalidOperationException("Unlock row missing after spend.");
        }

        if (unlockRequestId is Guid reqId)
        {
            var req = await _db.CandidateInsightsUnlockRequests
                .FirstOrDefaultAsync(r => r.Id == reqId, cancellationToken);
            if (req is not null && req.Status == CandidateInsightsUnlockRequestStatus.Open)
            {
                req.Status = CandidateInsightsUnlockRequestStatus.Ontgrendeld;
                req.HandledByUserId = user.Id;
                req.HandledAtUtc = now;
                await NotifyUnlockRequestHandledAsync(req, unlocked: true, cancellationToken);
            }
        }
        else if (scopeKind == CandidateInsightsUnlockScopeKind.Branch)
        {
            // Auto-close open request for this branch when BM/VM unlocks.
            var open = await _db.CandidateInsightsUnlockRequests
                .Where(r => r.BranchCompanyId == scopeCompanyId && r.Status == CandidateInsightsUnlockRequestStatus.Open)
                .ToListAsync(cancellationToken);
            foreach (var req in open)
            {
                req.Status = CandidateInsightsUnlockRequestStatus.Ontgrendeld;
                req.HandledByUserId = user.Id;
                req.HandledAtUtc = now;
                await NotifyUnlockRequestHandledAsync(req, unlocked: true, cancellationToken);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        // TODO(D8): reminder — schedule expiry reminder e-mail hook here.

        return new CandidateInsightsUnlockResultDto(
            created.Id,
            created.ExpiresAtUtc,
            outcome.Transaction.Amount < 0 ? -outcome.Transaction.Amount : cost,
            outcome.Balance);
    }

    public async Task<CandidateInsightsUnlockRequestDto> CreateUnlockRequestAsync(
        ClaimsPrincipal principal,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        EnsureAllowedRole(principal);

        if (!RoleClaimMatching.HasRole(principal, JobsyRoles.BranchManager))
        {
            throw new CandidateInsightsException("forbidden", "Alleen de vestigingsmanager kan inzichten aanvragen.", 403);
        }

        var user = await _users.FindByPrincipalAsync(principal, cancellationToken)
                   ?? throw new ForbiddenCompanyAccessException(branchId);
        if (user.CompanyId != branchId)
        {
            throw new CandidateInsightsException("forbidden_branch", "Je mag alleen voor je eigen vestiging aanvragen.", 403);
        }

        await _authz.EnsureCanAccessCompanyAsync(principal, branchId, cancellationToken);
        var branch = await _db.Companies.FirstOrDefaultAsync(c => c.Id == branchId, cancellationToken)
                     ?? throw new CandidateInsightsException("branch_not_found", "Vestiging niet gevonden.", 404);

        var walletId = CandidateInsightsAccess.ResolveWalletCompanyId(branch);
        var existingOpen = await _db.CandidateInsightsUnlockRequests
            .AnyAsync(
                r => r.BranchCompanyId == branchId && r.Status == CandidateInsightsUnlockRequestStatus.Open,
                cancellationToken);
        if (existingOpen)
        {
            throw new CandidateInsightsException(
                "already_open",
                "Er staat al een openstaande aanvraag voor deze vestiging.",
                409);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var row = new CandidateInsightsUnlockRequest
        {
            Id = Guid.NewGuid(),
            WalletCompanyId = walletId,
            BranchCompanyId = branchId,
            RequestedByUserId = user.Id,
            Status = CandidateInsightsUnlockRequestStatus.Open,
            CreatedAtUtc = now
        };
        _db.CandidateInsightsUnlockRequests.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        await NotifyBedrijfsmanagersInsightsRequestAsync(row, branch.Name, cancellationToken);

        return new CandidateInsightsUnlockRequestDto(
            row.Id,
            row.BranchCompanyId,
            branch.Name,
            row.RequestedByUserId,
            row.Status.ToString(),
            row.CreatedAtUtc,
            null);
    }

    public async Task<CandidateInsightsUnlockRequestDto> RejectUnlockRequestAsync(
        ClaimsPrincipal principal,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabledAsync(cancellationToken);
        EnsureAllowedRole(principal);

        if (!RoleClaimMatching.HasRole(principal, JobsyRoles.EnterpriseManager))
        {
            throw new CandidateInsightsException("forbidden", "Alleen de bedrijfsmanager kan afwijzen.", 403);
        }

        var user = await _users.FindByPrincipalAsync(principal, cancellationToken)
                   ?? throw new ForbiddenCompanyAccessException(Guid.Empty);
        var req = await _db.CandidateInsightsUnlockRequests
            .Include(r => r.BranchCompany)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new CandidateInsightsException("not_found", "Aanvraag niet gevonden.", 404);

        await _authz.EnsureCanAccessCompanyAsync(principal, req.BranchCompanyId, cancellationToken);

        if (req.Status != CandidateInsightsUnlockRequestStatus.Open)
        {
            throw new CandidateInsightsException("not_open", "Aanvraag is niet meer open.", 409);
        }

        req.Status = CandidateInsightsUnlockRequestStatus.Afgewezen;
        req.HandledByUserId = user.Id;
        req.HandledAtUtc = _clock.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(cancellationToken);
        await NotifyUnlockRequestHandledAsync(req, unlocked: false, cancellationToken);

        return new CandidateInsightsUnlockRequestDto(
            req.Id,
            req.BranchCompanyId,
            req.BranchCompany.Name,
            req.RequestedByUserId,
            req.Status.ToString(),
            req.CreatedAtUtc,
            req.HandledAtUtc);
    }

    public async Task<(string FileName, string Csv)> ExportCsvAsync(
        ClaimsPrincipal principal,
        Guid? branchId,
        int radiusKm,
        int periodDays,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetInsightsAsync(principal, branchId, radiusKm, periodDays, cancellationToken);
        if (!dto.Scope.IsFullAccess)
        {
            throw new CandidateInsightsException("locked", "Export vereist volledige inzichten.", 403);
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("section,key,label,value,status");
        void Row(string section, string key, string label, int? value, string status)
            => sb.AppendLine($"{Escape(section)},{Escape(key)},{Escape(label)},{value?.ToString() ?? ""},{Escape(status)}");

        Row("kpi", "CandidatesInRadius", "Kandidaten", dto.Kpis.CandidatesInRadius.Value, dto.Kpis.CandidatesInRadius.Status);
        Row("kpi", "AvgHoursPerWeek", "Uren", dto.Kpis.AvgHoursPerWeek.Value, dto.Kpis.AvgHoursPerWeek.Status);
        if (dto.Kpis.Candidates32PlusHours is { } h32)
        {
            Row("kpi", "Candidates32PlusHours", "32+", h32.Value, h32.Status);
        }

        if (dto.Kpis.Active30d is { } a30)
        {
            Row("kpi", "Active30d", "Actief30d", a30.Value, a30.Status);
        }

        if (dto.Kpis.MatchingYourVacancies is { } match)
        {
            Row("kpi", "MatchingYourVacancies", "Match", match.Value, match.Status);
        }

        foreach (var item in dto.DreamJobsTop)
        {
            Row("dreamJobs", item.Label, item.Label, item.Count.Value, item.Count.Status);
        }

        void Dist(string section, InsightsDistribution? dist)
        {
            if (dist is null)
            {
                return;
            }

            foreach (var b in dist.Buckets)
            {
                Row(section, b.Key, b.Label, b.Share.Percent, b.Share.Status);
            }
        }

        Dist("workFields", dto.WorkFields);
        Dist("priorities", dto.Priorities);
        Dist("workKinds", dto.WorkKinds);
        Dist("dna", dto.DnaRiasec);
        Dist("competences", dto.Competences);
        Dist("personality", dto.Personality);

        foreach (var v in dto.Vacancies)
        {
            Row("vacancies", v.VacancyId.ToString("D"), v.Title, v.MatchingCandidates.Value, v.MatchingCandidates.Status);
        }

        static string Escape(string s)
        {
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }

            return s;
        }

        var fileName = $"kandidaatinzichten-{dto.Scope.RadiusKm}km-{dto.Scope.PeriodDays}d.csv";
        return (fileName, sb.ToString());
    }

    private async Task EnsureFeatureEnabledAsync(CancellationToken cancellationToken)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            throw new CandidateInsightsException("feature_disabled", "Kandidaatinzichten is uitgeschakeld.", 404);
        }
    }

    private async Task<InsightsCoverage> LoadCoverageAsync(
        IReadOnlyList<Company> scopeCompanies,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var walletIds = scopeCompanies.Select(CandidateInsightsAccess.ResolveWalletCompanyId).Distinct().ToList();
        var scopeIds = scopeCompanies.Select(c => c.Id).ToList();
        // Also load company-wide unlocks whose ScopeCompanyId is the wallet.
        var unlocks = await _db.CandidateInsightsUnlocks.AsNoTracking()
            .Where(u => u.ExpiresAtUtc > now
                        && walletIds.Contains(u.WalletCompanyId)
                        && (scopeIds.Contains(u.ScopeCompanyId) || walletIds.Contains(u.ScopeCompanyId)))
            .ToListAsync(cancellationToken);
        return CandidateInsightsAccess.GetCoverage(scopeCompanies, unlocks, now);
    }

    private async Task<InsightsOfferDto> BuildOfferAsync(
        ClaimsPrincipal principal,
        IReadOnlyList<Company> scopeCompanies,
        InsightsCoverage coverage,
        PlatformFeatureSnapshot featureSnap,
        CancellationToken cancellationToken)
    {
        var perBranch = featureSnap.CandidateInsightsUnlockPerBranch;
        var scopeKind = perBranch ? "branch" : "company";
        var price = await _tokens.GetCostAsync(TokenSpendReason.InsightsUnlock, cancellationToken)
                    ?? CandidateInsightsAccess.DefaultUnlockCostTokens;
        var days = CandidateInsightsAccess.ClampUnlockDays(featureSnap.CandidateInsightsUnlockDays);

        decimal? balance = null;
        string? reason = null;
        var canUnlock = true;

        if (!featureSnap.CandidateInsightsEnabled)
        {
            canUnlock = false;
            reason = "FeatureDisabled";
        }
        else if (RoleClaimMatching.HasRole(principal, JobsyRoles.RegionalManager))
        {
            canUnlock = false;
            reason = "ReadOnlyRole";
        }
        else if (RoleClaimMatching.HasRole(principal, JobsyRoles.BranchManager))
        {
            if (!perBranch)
            {
                canUnlock = false;
                reason = "NeedsBranchScope";
            }
            else
            {
                var walletId = scopeCompanies.Count > 0
                    ? scopeCompanies[0].Id
                    : Guid.Empty;
                balance = await _tokens.GetBalanceAsync(walletId, cancellationToken);
                if (balance < price && !coverage.CanRenew && coverage.IsFull)
                {
                    // already full — renew may still need balance later
                }
                else if (balance < price)
                {
                    canUnlock = false;
                    reason = "InsufficientBalance";
                }
            }
        }
        else if (RoleClaimMatching.HasRole(principal, JobsyRoles.EnterpriseManager))
        {
            var walletId = scopeCompanies.Count > 0
                ? CandidateInsightsAccess.ResolveWalletCompanyId(scopeCompanies[0])
                : Guid.Empty;
            // Prefer organisation wallet
            foreach (var c in scopeCompanies)
            {
                var w = CandidateInsightsAccess.ResolveWalletCompanyId(c);
                if (c.ParentCompanyId is null)
                {
                    walletId = c.Id;
                    break;
                }

                walletId = w;
            }

            balance = await _tokens.GetBalanceAsync(walletId, cancellationToken);
            if (balance < price && !(coverage.IsFull && !coverage.CanRenew))
            {
                canUnlock = false;
                reason = "InsufficientBalance";
            }
        }

        if (coverage.IsFull && !coverage.CanRenew)
        {
            canUnlock = false;
            reason ??= "AlreadyUnlocked";
        }

        return new InsightsOfferDto(price, days, scopeKind, canUnlock, reason, balance);
    }

    private static InsightsCoverageDto ToCoverageDto(InsightsCoverage coverage, bool perBranch)
        => new(
            coverage.IsFull,
            coverage.CoveredCount,
            coverage.TotalCount,
            coverage.ExpiresAtUtc,
            coverage.CanRenew,
            perBranch ? "branch" : "company");

    private async Task NotifyBedrijfsmanagersInsightsRequestAsync(
        CandidateInsightsUnlockRequest row,
        string branchName,
        CancellationToken cancellationToken)
    {
        var managers = await (
            from u in _db.Users.AsNoTracking()
            where u.IsActive && u.Role == UserRole.EnterpriseManager
                  && (u.CompanyId == row.WalletCompanyId
                      || _db.UserCompanies.Any(uc => uc.UserId == u.Id && uc.CompanyId == row.WalletCompanyId))
            select u.Id
        ).Distinct().ToListAsync(cancellationToken);

        foreach (var managerId in managers)
        {
            await _notifications.CreateAsync(new NotificationCreateRequest(
                managerId,
                $"{branchName} vraagt Kandidaatinzichten aan",
                "Bekijk de aanvraag in Te doen.",
                "insights_unlock_request",
                DeepLink: "/werkgever/te-doen",
                ActionLabel: "Ontgrendelen",
                ActionUrl: $"/werkgever/kandidaatinzichten?request={row.Id:D}",
                RelatedEntityType: nameof(CandidateInsightsUnlockRequest),
                RelatedEntityId: row.Id), cancellationToken);
        }
    }

    private async Task NotifyUnlockRequestHandledAsync(
        CandidateInsightsUnlockRequest req,
        bool unlocked,
        CancellationToken cancellationToken)
    {
        var title = unlocked ? "Kandidaatinzichten ontgrendeld" : "Aanvraag afgewezen";
        var body = unlocked
            ? "Je bedrijfsmanager heeft Kandidaatinzichten ontgrendeld."
            : "Je aanvraag voor Kandidaatinzichten is afgewezen.";
        await _notifications.CreateAsync(new NotificationCreateRequest(
            req.RequestedByUserId,
            title,
            body,
            "insights_unlock_request",
            DeepLink: "/werkgever/kandidaatinzichten",
            RelatedEntityType: nameof(CandidateInsightsUnlockRequest),
            RelatedEntityId: req.Id), cancellationToken);
    }

    private async Task<ScopeCompanies> ResolveScopeCompaniesAsync(
        ClaimsPrincipal principal,
        Guid? branchId,
        bool allowAllUnion,
        CancellationToken cancellationToken)
    {
        var accessible = await _authz.GetAccessibleCompanyIdsAsync(principal, cancellationToken);
        if (accessible is null || accessible.Count == 0)
        {
            throw new ForbiddenCompanyAccessException(branchId ?? Guid.Empty);
        }

        var user = await _users.FindByPrincipalAsync(principal, cancellationToken)
                   ?? throw new ForbiddenCompanyAccessException(branchId ?? Guid.Empty);

        if (RoleClaimMatching.HasRole(principal, JobsyRoles.BranchManager))
        {
            var ownId = user.CompanyId
                        ?? throw new ForbiddenCompanyAccessException(branchId ?? Guid.Empty);
            if (!accessible.Contains(ownId))
            {
                throw new ForbiddenCompanyAccessException(ownId);
            }

            if (branchId is Guid requested && requested != ownId)
            {
                throw new ForbiddenCompanyAccessException(requested);
            }

            var own = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == ownId, cancellationToken)
                ?? throw new ForbiddenCompanyAccessException(ownId);
            return new ScopeCompanies([own]);
        }

        if (branchId is Guid id)
        {
            if (!accessible.Contains(id))
            {
                throw new ForbiddenCompanyAccessException(id);
            }

            var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new ForbiddenCompanyAccessException(id);
            return new ScopeCompanies([company]);
        }

        // "Alle vestigingen"
        if (!allowAllUnion
            || !(RoleClaimMatching.HasRole(principal, JobsyRoles.EnterpriseManager)
                 || RoleClaimMatching.HasRole(principal, JobsyRoles.RegionalManager))
            || accessible.Count < 2)
        {
            // Fall back to home / first accessible when union not allowed
            var fallbackId = user.CompanyId is Guid home && accessible.Contains(home)
                ? home
                : accessible.First();
            var fallback = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == fallbackId, cancellationToken)
                ?? throw new ForbiddenCompanyAccessException(fallbackId);
            return new ScopeCompanies([fallback]);
        }

        var list = await _db.Companies.AsNoTracking()
            .Where(c => accessible.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        if (list.Count == 0)
        {
            throw new ForbiddenCompanyAccessException(Guid.Empty);
        }

        return new ScopeCompanies(list);
    }

    private static void EnsureAllowedRole(ClaimsPrincipal principal)
    {
        if (RoleClaimMatching.HasRole(principal, JobsyRoles.BranchManager)
            || RoleClaimMatching.HasRole(principal, JobsyRoles.RegionalManager)
            || RoleClaimMatching.HasRole(principal, JobsyRoles.EnterpriseManager))
        {
            return;
        }

        throw new ForbiddenCompanyAccessException(Guid.Empty);
    }

    private sealed record ScopeCompanies(IReadOnlyList<Company> Companies);
}
