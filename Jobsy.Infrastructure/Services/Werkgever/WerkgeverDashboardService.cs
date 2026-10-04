using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.Services.Werkgever;

public sealed class WerkgeverDashboardService : IWerkgeverDashboardService
{
    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _tokens;
    private readonly IEnumerable<ITodoSource> _todoSources;
    private readonly IMemoryCache _cache;

    public WerkgeverDashboardService(
        JobsyDbContext db,
        ITokenLedgerService tokens,
        IEnumerable<ITodoSource> todoSources,
        IMemoryCache cache)
    {
        _db = db;
        _tokens = tokens;
        _todoSources = todoSources;
        _cache = cache;
    }

    public async Task<WerkgeverDashboardDto> GetDashboardAsync(
        IReadOnlyList<Guid> companyIds,
        string period,
        WerkgeverDashboardRole role,
        CancellationToken cancellationToken = default)
    {
        period = WerkgeverDashboardRules.NormalizePeriod(period);
        var scopeKey = ScopeCacheKey(companyIds);
        var cacheKey = $"wg-dash:{scopeKey}:{period}:{role}";
        if (_cache.TryGetValue(cacheKey, out WerkgeverDashboardDto? cached) && cached is not null)
        {
            return cached;
        }

        var now = DateTime.UtcNow;
        var (periodStart, previousStart, previousEnd) = WerkgeverDashboardRules.ResolvePeriodWindow(period, now);
        var ids = companyIds.ToHashSet();

        var companies = await _db.Companies.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.ParentCompanyId, c.TokensManagedByEnterprise })
            .ToListAsync(cancellationToken);
        // The parent company is not a branch. Count and list only vestigingen when children exist.
        if (companies.Any(c => c.ParentCompanyId is not null))
        {
            companies = companies.Where(c => c.ParentCompanyId is not null).ToList();
        }

        var regionRows = await _db.RegionCompanies.AsNoTracking()
            .Where(rc => ids.Contains(rc.CompanyId))
            .Select(rc => new { rc.CompanyId, rc.Region.Name, rc.RegionId })
            .ToListAsync(cancellationToken);
        var regionByCompany = regionRows
            .GroupBy(r => r.CompanyId)
            .ToDictionary(g => g.Key, g => g.First().Name);
        var regionCount = regionRows.Select(r => r.RegionId).Distinct().Count();

        var activeNow = await _db.Vacancies.AsNoTracking()
            .CountAsync(v => ids.Contains(v.CompanyId) && v.Status == VacancyStatus.Active, cancellationToken);
        var activePrev = await _db.Vacancies.AsNoTracking()
            .CountAsync(
                v => ids.Contains(v.CompanyId)
                     && v.Status == VacancyStatus.Active
                     && (v.PublishedAtUtc == null || v.PublishedAtUtc < previousEnd),
                cancellationToken);

        var newApps = await _db.Applications.AsNoTracking()
            .CountAsync(
                a => ids.Contains(a.Vacancy.CompanyId) && a.CreatedAt >= periodStart && a.CreatedAt <= now,
                cancellationToken);
        var newAppsPrev = await _db.Applications.AsNoTracking()
            .CountAsync(
                a => ids.Contains(a.Vacancy.CompanyId) && a.CreatedAt >= previousStart && a.CreatedAt < previousEnd,
                cancellationToken);

        var responseHours = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.Vacancy.CompanyId)
                        && a.RespondedAt != null
                        && a.RespondedAt >= periodStart
                        && a.RespondedAt <= now)
            .Select(a => new { a.CreatedAt, RespondedAt = a.RespondedAt!.Value })
            .ToListAsync(cancellationToken);
        var hours = responseHours
            .Select(x => (x.RespondedAt - x.CreatedAt).TotalHours)
            .Where(h => h >= 0)
            .ToList();
        var avgHours = WerkgeverDashboardRules.AverageFirstResponseHours(hours);

        var prevResponseHours = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.Vacancy.CompanyId)
                        && a.RespondedAt != null
                        && a.RespondedAt >= previousStart
                        && a.RespondedAt < previousEnd)
            .Select(a => new { a.CreatedAt, RespondedAt = a.RespondedAt!.Value })
            .ToListAsync(cancellationToken);
        var prevHours = prevResponseHours
            .Select(x => (x.RespondedAt - x.CreatedAt).TotalHours)
            .Where(h => h >= 0)
            .ToList();
        var avgHoursPrev = WerkgeverDashboardRules.AverageFirstResponseHours(prevHours);

        var hired = await _db.Applications.AsNoTracking()
            .CountAsync(
                a => ids.Contains(a.Vacancy.CompanyId)
                     && a.Status == ApplicationStatus.Hired
                     && a.RespondedAt != null
                     && a.RespondedAt >= periodStart
                     && a.RespondedAt <= now,
                cancellationToken);
        var hiredPrev = await _db.Applications.AsNoTracking()
            .CountAsync(
                a => ids.Contains(a.Vacancy.CompanyId)
                     && a.Status == ApplicationStatus.Hired
                     && a.RespondedAt != null
                     && a.RespondedAt >= previousStart
                     && a.RespondedAt < previousEnd,
                cancellationToken);

        // Funnel: counts per stage for applications created in the period (current status).
        var funnelApps = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.Vacancy.CompanyId) && a.CreatedAt >= periodStart && a.CreatedAt <= now)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var funnelMap = funnelApps.ToDictionary(x => x.Status, x => x.Count);
        var funnelApplications = newApps;
        var funnelAccepted = funnelMap.GetValueOrDefault(ApplicationStatus.Accepted)
                             + funnelMap.GetValueOrDefault(ApplicationStatus.EmployerContacting)
                             + funnelMap.GetValueOrDefault(ApplicationStatus.Hired);
        var funnelContacting = funnelMap.GetValueOrDefault(ApplicationStatus.EmployerContacting)
                               + funnelMap.GetValueOrDefault(ApplicationStatus.Hired);
        var funnelHired = funnelMap.GetValueOrDefault(ApplicationStatus.Hired);

        decimal? tokenBalance = null;
        decimal? tokensUsed = null;
        decimal? tokensAllocated = null;
        double? runway = null;
        var showBalance = role is WerkgeverDashboardRole.Bedrijfsmanager or WerkgeverDashboardRole.Vestigingsmanager
                          or WerkgeverDashboardRole.Intermediair;
        var showUsage = role == WerkgeverDashboardRole.Regiomanager;

        var lookbackStart = now.AddDays(-7 * WerkgeverDashboardRules.TokenRunwayLookbackWeeks);
        var spendRows = await _db.TokenTransactions.AsNoTracking()
            .Where(t => ids.Contains(t.CompanyId)
                        && t.Kind == TokenTransactionKind.Spend
                        && t.CreatedAt >= lookbackStart)
            .Select(t => new { t.CompanyId, t.Amount, t.CreatedAt })
            .ToListAsync(cancellationToken);
        var totalSpend = spendRows.Sum(t => Math.Abs(t.Amount));

        if (showBalance)
        {
            decimal bal = 0m;
            foreach (var c in companies)
            {
                bal += await _tokens.GetBalanceAsync(c.Id, cancellationToken);
            }

            tokenBalance = bal;
            runway = WerkgeverDashboardRules.TokenRunwayWeeks(bal, totalSpend);
        }

        if (showUsage)
        {
            tokensUsed = totalSpend;
            var allocated = await _db.TokenTransactions.AsNoTracking()
                .Where(t => ids.Contains(t.CompanyId)
                            && t.Kind == TokenTransactionKind.Allocation
                            && t.Amount > 0)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
            tokensAllocated = allocated;
        }

        // Branch rows
        var liveByCompany = await _db.Vacancies.AsNoTracking()
            .Where(v => ids.Contains(v.CompanyId) && v.Status == VacancyStatus.Active)
            .GroupBy(v => v.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, cancellationToken);

        var newByCompany = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.Vacancy.CompanyId) && a.CreatedAt >= periodStart && a.CreatedAt <= now)
            .GroupBy(a => a.Vacancy.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, cancellationToken);

        var overdueCutoff = now.AddHours(-WerkgeverDashboardRules.OverduePendingHours);
        var overdueByCompany = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.Vacancy.CompanyId)
                        && a.Status == ApplicationStatus.Pending
                        && a.CreatedAt < overdueCutoff)
            .GroupBy(a => a.Vacancy.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, cancellationToken);

        var responseRaw = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.Vacancy.CompanyId)
                        && a.RespondedAt != null
                        && a.RespondedAt >= periodStart
                        && a.RespondedAt <= now)
            .Select(a => new { a.Vacancy.CompanyId, a.CreatedAt, RespondedAt = a.RespondedAt!.Value })
            .ToListAsync(cancellationToken);
        var avgByCompany = responseRaw
            .GroupBy(x => x.CompanyId)
            .ToDictionary(
                g => g.Key,
                g => WerkgeverDashboardRules.AverageFirstResponseHours(
                    g.Select(x => (x.RespondedAt - x.CreatedAt).TotalHours).Where(h => h >= 0).ToList()));

        var managerCompanyIds = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive
                        && u.Role == UserRole.BranchManager
                        && u.CompanyId != null
                        && ids.Contains(u.CompanyId.Value))
            .Select(u => u.CompanyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var managers = managerCompanyIds.ToHashSet();

        var branchRows = new List<WerkgeverBranchRowDto>();
        foreach (var c in companies.OrderBy(x => x.Name))
        {
            var avgH = avgByCompany.GetValueOrDefault(c.Id);
            var avgDays = WerkgeverDashboardRules.HoursToDays(avgH);
            decimal? bal = null;
            if (c.TokensManagedByEnterprise || role == WerkgeverDashboardRole.Vestigingsmanager)
            {
                bal = await _tokens.GetBalanceAsync(c.Id, cancellationToken);
            }

            var status = WerkgeverDashboardRules.ResolveBranchStatus(
                overdueByCompany.GetValueOrDefault(c.Id),
                avgDays,
                managers.Contains(c.Id),
                bal);

            branchRows.Add(new WerkgeverBranchRowDto(
                c.Id,
                c.Name,
                regionByCompany.GetValueOrDefault(c.Id),
                liveByCompany.GetValueOrDefault(c.Id),
                newByCompany.GetValueOrDefault(c.Id),
                avgH,
                status.ToString()));
        }

        var kpis = new WerkgeverKpiDto(
            ActiveVacancies: activeNow,
            ActiveVacanciesDelta: activeNow - activePrev,
            NewApplications: newApps,
            NewApplicationsDeltaPercent: newAppsPrev == 0
                ? (newApps == 0 ? 0 : 100)
                : Math.Round((newApps - newAppsPrev) * 100.0 / newAppsPrev, 1),
            AvgFirstResponseHours: avgHours,
            AvgFirstResponseHoursDelta: avgHours is null || avgHoursPrev is null
                ? null
                : avgHoursPrev.Value - avgHours.Value,
            Hired: hired,
            HiredDelta: hired - hiredPrev,
            TokenBalance: tokenBalance,
            TokensUsed: tokensUsed,
            TokensAllocated: tokensAllocated,
            TokenRunwayWeeks: runway,
            ActiveVacanciesSpark: await BuildSparkAsync(
                ids, periodStart, now, SparkMetric.ActiveVacancies, cancellationToken),
            NewApplicationsSpark: await BuildSparkAsync(
                ids, periodStart, now, SparkMetric.NewApplications, cancellationToken),
            AvgFirstResponseSpark: await BuildSparkAsync(
                ids, periodStart, now, SparkMetric.AvgFirstResponse, cancellationToken),
            HiredSpark: await BuildSparkAsync(
                ids, periodStart, now, SparkMetric.Hired, cancellationToken),
            TokensSpark: new WerkgeverKpiSparkDto(BuildTokenSpark(spendRows.Select(s => (s.CreatedAt, s.Amount)).ToList(), lookbackStart, now)));

        var dto = new WerkgeverDashboardDto(
            Period: period,
            VestigingCount: companies.Count,
            RegionCount: regionCount,
            ScopeLabel: null,
            Kpis: kpis,
            Funnel: new WerkgeverFunnelDto(funnelApplications, funnelAccepted, funnelContacting, funnelHired),
            Branches: branchRows,
            ShowTokenBalance: showBalance,
            ShowTokenUsage: showUsage);

        _cache.Set(cacheKey, dto, WerkgeverDashboardRules.DashboardCacheTtl);
        return dto;
    }

    public async Task<IReadOnlyList<WerkgeverTodoItemDto>> GetTodoAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var scopeKey = ScopeCacheKey(companyIds);
        var cacheKey = $"wg-todo:{scopeKey}:{role}:{take}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<WerkgeverTodoItemDto>? cached) && cached is not null)
        {
            return cached;
        }

        var now = DateTime.UtcNow;
        var items = new List<WerkgeverTodoItemDto>();
        foreach (var source in _todoSources)
        {
            var item = await source.BuildAsync(companyIds, role, now, cancellationToken);
            if (item is not null && item.Count > 0)
            {
                items.Add(item);
            }
        }

        var ordered = items
            .OrderByDescending(i => ParseSeverity(i.Severity))
            .ThenByDescending(i => i.Count)
            .Take(take)
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<WerkgeverTodoItemDto>)ordered, WerkgeverDashboardRules.DashboardCacheTtl);
        return ordered;
    }

    private enum SparkMetric { ActiveVacancies, NewApplications, AvgFirstResponse, Hired }

    private async Task<WerkgeverKpiSparkDto> BuildSparkAsync(
        HashSet<Guid> ids,
        DateTime periodStart,
        DateTime now,
        SparkMetric metric,
        CancellationToken ct)
    {
        var points = new decimal[WerkgeverDashboardRules.SparkPointCount];
        var totalTicks = Math.Max(1, (now - periodStart).Ticks);
        for (var i = 0; i < WerkgeverDashboardRules.SparkPointCount; i++)
        {
            var sliceEnd = periodStart.AddTicks(totalTicks * (i + 1) / WerkgeverDashboardRules.SparkPointCount);
            var sliceStart = periodStart.AddTicks(totalTicks * i / WerkgeverDashboardRules.SparkPointCount);
            points[i] = metric switch
            {
                SparkMetric.ActiveVacancies => await _db.Vacancies.AsNoTracking()
                    .CountAsync(
                        v => ids.Contains(v.CompanyId)
                             && v.Status == VacancyStatus.Active
                             && (v.PublishedAtUtc == null || v.PublishedAtUtc <= sliceEnd),
                        ct),
                SparkMetric.NewApplications => await _db.Applications.AsNoTracking()
                    .CountAsync(
                        a => ids.Contains(a.Vacancy.CompanyId)
                             && a.CreatedAt >= sliceStart
                             && a.CreatedAt < sliceEnd,
                        ct),
                SparkMetric.Hired => await _db.Applications.AsNoTracking()
                    .CountAsync(
                        a => ids.Contains(a.Vacancy.CompanyId)
                             && a.Status == ApplicationStatus.Hired
                             && a.RespondedAt != null
                             && a.RespondedAt >= sliceStart
                             && a.RespondedAt < sliceEnd,
                        ct),
                SparkMetric.AvgFirstResponse => await AvgHoursInSliceAsync(ids, sliceStart, sliceEnd, ct),
                _ => 0
            };
        }

        return new WerkgeverKpiSparkDto(points);
    }

    private async Task<decimal> AvgHoursInSliceAsync(
        HashSet<Guid> ids,
        DateTime start,
        DateTime end,
        CancellationToken ct)
    {
        var rows = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.Vacancy.CompanyId)
                        && a.RespondedAt != null
                        && a.RespondedAt >= start
                        && a.RespondedAt < end)
            .Select(a => new { a.CreatedAt, RespondedAt = a.RespondedAt!.Value })
            .ToListAsync(ct);
        if (rows.Count == 0)
        {
            return 0;
        }

        return (decimal)rows.Average(x => (x.RespondedAt - x.CreatedAt).TotalHours);
    }

    private static decimal[] BuildTokenSpark(
        List<(DateTime CreatedAtUtc, decimal Amount)> spend,
        DateTime start,
        DateTime end)
    {
        var points = new decimal[WerkgeverDashboardRules.SparkPointCount];
        var totalTicks = Math.Max(1, (end - start).Ticks);
        for (var i = 0; i < WerkgeverDashboardRules.SparkPointCount; i++)
        {
            var sliceEnd = start.AddTicks(totalTicks * (i + 1) / WerkgeverDashboardRules.SparkPointCount);
            var sliceStart = start.AddTicks(totalTicks * i / WerkgeverDashboardRules.SparkPointCount);
            points[i] = spend
                .Where(s => s.CreatedAtUtc >= sliceStart && s.CreatedAtUtc < sliceEnd)
                .Sum(s => Math.Abs(s.Amount));
        }

        return points;
    }

    private static string ScopeCacheKey(IReadOnlyList<Guid> companyIds)
        => string.Join(',', companyIds.OrderBy(x => x).Select(x => x.ToString("N")));

    private static WerkgeverTodoSeverity ParseSeverity(string severity)
        => Enum.TryParse<WerkgeverTodoSeverity>(severity, true, out var s) ? s : WerkgeverTodoSeverity.Info;
}
