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
    private readonly IMemoryCache _cache;
    private readonly ILogger<CandidateInsightsService> _logger;

    public CandidateInsightsService(
        JobsyDbContext db,
        ICompanyAuthorizationService authz,
        IUserLookupService users,
        ITokenLedgerService tokens,
        IMemoryCache cache,
        ILogger<CandidateInsightsService> logger)
    {
        _db = db;
        _authz = authz;
        _users = users;
        _tokens = tokens;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CandidateInsightsBranchDto>> GetBranchesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
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
        EnsureAllowedRole(principal);
        if (!AllowedRadiiKm.Contains(radiusKm) || !AllowedPeriodsDays.Contains(periodDays))
        {
            throw new ArgumentException("Invalid radiusKm or period.");
        }

        // AuthZ before cache — never serve a cached DTO to an unauthorized caller.
        var scope = await ResolveScopeCompaniesAsync(principal, branchId, allowAllUnion: true, cancellationToken);
        var isFullAccess = await CandidateInsightsAccess.IsFullAccessAsync(_tokens, scope.Companies, cancellationToken);

        var cacheKey = BuildCacheKey(scope.Companies.Select(c => c.Id), radiusKm, periodDays, isFullAccess);
        if (_cache.TryGetValue(cacheKey, out CandidateInsightsDto? cached) && cached is not null)
        {
            return cached;
        }

        var dto = await BuildAsync(scope.Companies, radiusKm, periodDays, isFullAccess, cancellationToken);
        _cache.Set(cacheKey, dto, CacheTtl);
        return dto;
    }

    internal static string BuildCacheKey(IEnumerable<Guid> branchIds, int radiusKm, int periodDays, bool isFullAccess)
    {
        var sorted = string.Join(',', branchIds.OrderBy(id => id).Select(id => id.ToString("D")));
        return $"candidate-insights:{sorted}:r{radiusKm}:p{periodDays}:f{(isFullAccess ? 1 : 0)}";
    }

    private async Task<CandidateInsightsDto> BuildAsync(
        IReadOnlyList<Company> companies,
        int radiusKm,
        int periodDays,
        bool isFullAccess,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var periodStart = now.AddDays(-periodDays);
        var active30Start = now.AddDays(-30);

        var origins = companies
            .Where(c => c.Location is not null)
            .Select(c => c.Location!)
            .ToList();

        var cohortUsers = await LoadCohortUsersAsync(origins, radiusKm, periodStart, cancellationToken);
        var cohortIds = cohortUsers.Select(u => u.Id).ToList();
        var cohortSize = cohortIds.Count;

        var locked = isFullAccess
            ? Array.Empty<string>()
            : new[] { LockedDreamJobs4To10, LockedDna, LockedStory5To10 };

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
            return InsufficientDto(scope, locked);
        }

        var prefsByUser = cohortUsers.ToDictionary(
            u => u.Id,
            u => MatchingProfileMapper.DeserializePrefs(u.PreferencesJson));

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

        // KPIs
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
        // Avg hours is not a headcount — still suppress when cohort is ok but we report via SuppressCount on rounded avg only when enough hours samples.
        var avgHours = CandidateInsightsPrivacy.MeetsThreshold(hourMidpoints.Count)
            ? new SuppressedCount(CandidateInsightsPrivacy.StatusOk, Math.Max(0, avgHoursRaw))
            : new SuppressedCount(CandidateInsightsPrivacy.StatusInsufficient, null);

        var (p32s, p32v) = CandidateInsightsPrivacy.SuppressCount(plus32);
        var active30 = cohortUsers.Count(u => u.LastLoginAtUtc is DateTime t && t >= active30Start);
        var (a30s, a30v) = CandidateInsightsPrivacy.SuppressCount(active30);

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
        var kpis = new InsightsKpis(
            new SuppressedCount(candStatus, candValue),
            avgHours,
            new SuppressedCount(p32s, p32v),
            new SuppressedCount(a30s, a30v),
            new SuppressedCount(matchS, matchV));

        // Dream jobs
        var dreamGroups = careerPlans
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
            .Take(isFullAccess ? 10 : 3)
            .Select(x =>
            {
                var (st, val) = CandidateInsightsPrivacy.SuppressCount(x.Count);
                return new RankedItem(x.Label, new SuppressedCount(st, val));
            })
            .ToList();

        // Work fields from preference Roles (top preferred roles; no invented category mapping)
        var roleCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var prefs in prefsByUser.Values)
        {
            foreach (var role in prefs.Roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var key = role.Trim();
                roleCounts[key] = roleCounts.GetValueOrDefault(key) + 1;
            }
        }

        var workFields = BuildDistributionFromCounts(roleCounts, cohortSize, take: 8);

        // DNA / competences / personality (locked when !full)
        InsightsDistribution? dna = null;
        InsightsDistribution? competences = null;
        InsightsDistribution? personality = null;
        if (isFullAccess)
        {
            dna = BuildRiasecDistribution(careerInterests, cohortSize);
            competences = BuildCompetencyDistribution(competencies, cohortSize);
            personality = BuildPersonalityDistribution(personalities, cohortSize);
        }

        // Priorities
        var travel = prefsByUser.Values.Count(p => p.MaxTravelMinutes is int m && m <= 20);
        var flex = prefsByUser.Values.Count(p => p.FlexibleTimes == true);
        var connection = values.Count(v => v.ConnectionPercent is int c && c >= 60);
        var stability = values.Count(v => v.StabilityPercent is int s && s >= 60);
        var priorities = new InsightsDistribution(
            CandidateInsightsPrivacy.StatusOk,
            new[]
            {
                Bucket("travel", "reistijd", travel, cohortSize),
                Bucket("flexibility", "flexibiliteit", flex, cohortSize),
                Bucket("culture", "sfeer_cultuur", connection, cohortSize),
                Bucket("stability", "zekerheid", stability, cohortSize)
            });

        // Work kinds
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

        var workKinds = new InsightsDistribution(
            CandidateInsightsPrivacy.StatusOk,
            new[]
            {
                Bucket("fulltime", "fulltime", fulltime, cohortSize),
                Bucket("parttime", "parttime", parttime, cohortSize),
                Bucket("bijbaan", "bijbaan", sideJob, cohortSize),
                Bucket("stage", "stage", internship, cohortSize),
                Bucket("vrijwilliger", "vrijwilliger", volunteer, cohortSize)
            });

        // Density
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
        var density = new List<DensityCell>();
        if (eligibleCells.Count > 0)
        {
            var valuesSorted = eligibleCells.Select(c => c.Value).OrderBy(v => v).ToList();
            var t1 = Percentile(valuesSorted, 1.0 / 3.0);
            var t2 = Percentile(valuesSorted, 2.0 / 3.0);
            foreach (var (cell, count) in eligibleCells)
            {
                var band = count <= t1 ? 1 : count <= t2 ? 2 : 3;
                var (lat, lng) = CandidateInsightsDensityGrid.CellCenter(cell.X, cell.Y);
                density.Add(new DensityCell(
                    CandidateInsightsDensityGrid.CellId(cell.X, cell.Y),
                    lat,
                    lng,
                    band));
            }
        }

        // Vacancy reach + tips
        var tipKeys = BuildTipKeys(priorities);
        var vacancyReach = vacancies
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

        // Attach checklist only on the top vacancy; others keep tip keys only
        if (vacancyReach.Count > 0)
        {
            var top = vacancyReach[0];
            vacancyReach = vacancyReach
                .Select((v, i) => i == 0
                    ? v
                    : v with { Tips = new InsightsTips(tipKeys, Checklist: null) })
                .ToList();
        }

        var trend = new InsightsTrend("insufficient_history", "Insights.Trend.InsufficientHistory");

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
            locked);
    }

    private static CandidateInsightsDto InsufficientDto(InsightsScope scope, IReadOnlyList<string> locked)
    {
        var insuff = new SuppressedCount(CandidateInsightsPrivacy.StatusInsufficient, null);
        var emptyDist = new InsightsDistribution(CandidateInsightsPrivacy.StatusInsufficient, []);
        return new CandidateInsightsDto(
            scope,
            new InsightsKpis(insuff, insuff, insuff, insuff, insuff),
            [],
            emptyDist,
            scope.IsFullAccess ? emptyDist : null,
            scope.IsFullAccess ? emptyDist : null,
            scope.IsFullAccess ? emptyDist : null,
            emptyDist,
            emptyDist,
            [],
            [],
            new InsightsTrend("insufficient_history", "Insights.Trend.InsufficientHistory"),
            locked);
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

    private static IReadOnlyList<string> BuildTipKeys(InsightsDistribution priorities)
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

    private static int Percentile(IReadOnlyList<int> sortedAscending, double p)
    {
        if (sortedAscending.Count == 0)
        {
            return 0;
        }

        var idx = (int)Math.Floor((sortedAscending.Count - 1) * p);
        return sortedAscending[Math.Clamp(idx, 0, sortedAscending.Count - 1)];
    }

    private static IReadOnlyList<CandidateMatchedVacancyDto> ParseMatches(string? json)
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
        IReadOnlyList<GeoPoint> origins,
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
