using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class DashboardLiveOverlay : IDashboardLiveOverlay
{
    private readonly JobsyDbContext _db;
    private readonly ICommissionLedgerService _ledger;
    private readonly IAmbassadeurSettingsService _ambassadeurSettings;

    public DashboardLiveOverlay(
        JobsyDbContext db,
        ICommissionLedgerService ledger,
        IAmbassadeurSettingsService ambassadeurSettings)
    {
        _db = db;
        _ledger = ledger;
        _ambassadeurSettings = ambassadeurSettings;
    }

    public async Task<IReadOnlyList<MetricCountDto>> OverlayMetricsAsync(
        IReadOnlyList<MetricCountDto> cached,
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default)
    {
        if (cached.Count == 0)
        {
            return cached;
        }

        var needed = cached
            .Select(m => m.Key)
            .Where(DashboardLiveMetricKeys.All.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (needed.Count == 0)
        {
            return cached;
        }

        var live = await LoadLiveMetricValuesAsync(needed, includePlatformOnly, companyIds, period, cancellationToken);
        return cached
            .Select(m => live.TryGetValue(m.Key, out var value) ? m with { Value = value } : m)
            .ToList();
    }

    public async Task<ClientPerformanceBoardDto> OverlayClientsAsync(
        ClientPerformanceBoardDto cached,
        IReadOnlyCollection<Guid>? companyIds,
        CancellationToken cancellationToken = default)
    {
        if (cached.Clients.Count == 0)
        {
            return cached;
        }

        var ids = cached.Clients.Select(c => c.CompanyId).ToList();
        var live = await LoadLiveClientRowsAsync(ids, cancellationToken);
        var rows = cached.Clients
            .Select(row =>
            {
                live.TryGetValue(row.CompanyId, out var overlay);
                return row with
                {
                    ActiveVacancies = overlay.ActiveVacancies,
                    ApplicationsPending = overlay.ApplicationsPending,
                    ActiveBoosts = overlay.ActiveBoosts
                };
            })
            .ToList();

        return cached with { Clients = rows };
    }

    public async Task<SalesManagerDashboardDto> OverlaySalesAsync(
        SalesManagerDashboardDto cached,
        CancellationToken cancellationToken = default)
    {
        var suppliers = await _db.Companies.AsNoTracking()
            .Where(c => c.ReferredBySalesManagerUserId == cached.UserId)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.KvkNumber,
                c.FirstYearSupplierSlot,
                c.FirstYearStartedAt,
                HasPaid = _db.SupplierOnboardingCheckouts.Any(o =>
                    o.CompanyId == c.Id && o.Status == SupplierOnboardingCheckoutStatus.Credited)
            })
            .ToListAsync(cancellationToken);

        var ledger = await _ledger.ListEntriesAsync(cached.UserId, cancellationToken);

        return cached with
        {
            Suppliers = suppliers.Select(s => new ReferredSupplierDto(
                s.Id, s.Name, s.KvkNumber, s.FirstYearSupplierSlot, s.FirstYearStartedAt, s.HasPaid)).ToList(),
            RecentLedger = ledger.Take(50).Select(e => new CommissionEntryDto(
                e.Id,
                e.Kind.ToString(),
                e.AmountExVat,
                e.VatAmount,
                e.Note,
                e.CompanyId,
                e.Company?.Name,
                e.CreatedAt,
                e.SelfBillingInvoiceId)).ToList()
        };
    }

    public async Task<AmbassadeurDashboardDto> OverlayAmbassadeurAsync(
        AmbassadeurDashboardDto cached,
        CancellationToken cancellationToken = default)
    {
        var candidateIds = await _db.Users.AsNoTracking()
            .Where(u => u.ReferredByAmbassadeurUserId == cached.UserId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var registered = candidateIds.Count;
        var applications = registered == 0
            ? 0
            : await _db.Applications.AsNoTracking()
                .CountAsync(
                    a => a.CandidateUserId != null && candidateIds.Contains(a.CandidateUserId.Value),
                    cancellationToken);

        var profile = await _db.AmbassadeurProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == cached.UserId, cancellationToken);
        var settings = await _ambassadeurSettings.GetAsync(cancellationToken);
        var current = AmbassadeurCommissionRules.ResolveCurrentPercentage(
            registered,
            profile?.BaseCommissionPercentage ?? AmbassadeurCommissionRules.DefaultBaseCommissionPercentage,
            settings.CandidateThreshold,
            settings.PercentPerThreshold,
            settings.MaxCommissionPercentage,
            profile?.CommissionPercentageOverride);

        var threshold = Math.Max(1, settings.CandidateThreshold);
        var nextTierAt = ((registered / threshold) + 1) * threshold;
        var untilNext = Math.Max(0, nextTierAt - registered);
        if (current >= settings.MaxCommissionPercentage && profile?.CommissionPercentageOverride is null)
        {
            untilNext = 0;
        }

        var recentCandidates = await _db.Users.AsNoTracking()
            .Where(u => u.ReferredByAmbassadeurUserId == cached.UserId)
            .OrderByDescending(u => u.Id)
            .Take(12)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                ApplicationCount = _db.Applications.Count(a => a.CandidateUserId == u.Id)
            })
            .ToListAsync(cancellationToken);

        return cached with
        {
            RegisteredCandidates = registered,
            CandidateApplications = applications,
            CurrentCommissionPercentage = current,
            CandidatesUntilNextTier = untilNext,
            RecentCandidates = recentCandidates
                .Select(c => new ReferredCandidateDto(
                    c.Id, AmbassadeurDashboardService.Initials(c.FullName), null, c.ApplicationCount))
                .ToList()
        };
    }

    private async Task<Dictionary<string, decimal>> LoadLiveMetricValuesAsync(
        HashSet<string> needed,
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var vacancyQuery = _db.Vacancies.AsNoTracking().AsQueryable();
        if (companyIds is not null)
        {
            vacancyQuery = vacancyQuery.Where(v => companyIds.Contains(v.CompanyId));
        }

        if (needed.Contains("applications_pending"))
        {
            var vacancyIds = await vacancyQuery.Select(v => v.Id).ToListAsync(cancellationToken);
            values["applications_pending"] = await _db.Applications.AsNoTracking()
                .CountAsync(
                    a => vacancyIds.Contains(a.VacancyId)
                         && a.EmailVerifiedAt != null
                         && a.Status == ApplicationStatus.Pending,
                    cancellationToken);
        }

        if (needed.Contains("active_vacancies")
            || needed.Contains("active_vacancies_employers")
            || needed.Contains("active_vacancies_intermediaries"))
        {
            values["active_vacancies"] = await vacancyQuery
                .CountAsync(v => v.Status == VacancyStatus.Active, cancellationToken);
            values["active_vacancies_employers"] = await vacancyQuery.CountAsync(
                v => v.Status == VacancyStatus.Active && v.Company.Type == CompanyType.Employer,
                cancellationToken);
            values["active_vacancies_intermediaries"] = await vacancyQuery.CountAsync(
                v => v.Status == VacancyStatus.Active && v.Company.Type == CompanyType.Intermediary,
                cancellationToken);
        }

        if (needed.Contains("active_boosts"))
        {
            var utcNow = DateTime.UtcNow;
            var highlightedRows = await vacancyQuery
                .Where(v => v.Status == VacancyStatus.Active && v.IsHighlighted)
                .Select(v => new { v.IsHighlighted, v.HighlightedUntil })
                .ToListAsync(cancellationToken);
            values["active_boosts"] = highlightedRows.Count(v =>
                VacancyHighlightRules.IsActive(v.IsHighlighted, v.HighlightedUntil, utcNow));
        }

        if (includePlatformOnly && needed.Contains("unpublished_vacancies"))
        {
            values["unpublished_vacancies"] = await _db.Vacancies.AsNoTracking()
                .CountAsync(
                    v => v.Status == VacancyStatus.Draft && v.PublishedAtUtc == null,
                    cancellationToken);
        }

        if (includePlatformOnly && (needed.Contains("users_open_for_work") || needed.Contains("users_active")))
        {
            values["users_open_for_work"] = await _db.Users.AsNoTracking()
                .CountAsync(
                    u => u.Role == UserRole.Candidate && u.IsActive && u.OpenForWork,
                    cancellationToken);
            values["users_active"] = await _db.Users.AsNoTracking()
                .CountAsync(u => u.IsActive, cancellationToken);
        }

        if (includePlatformOnly && needed.Contains("errors"))
        {
            var metricsPeriod = MetricsPeriodParser.Parse(period);
            var (from, to) = MetricsPeriodParser.ResolveRange(metricsPeriod);
            values["errors"] = await _db.PlatformLogs.AsNoTracking()
                .CountAsync(
                    l => l.Level == PlatformLogLevel.Error && l.CreatedAt >= from && l.CreatedAt <= to,
                    cancellationToken);
        }

        return values;
    }

    private async Task<Dictionary<Guid, (int ActiveVacancies, int ApplicationsPending, int ActiveBoosts)>> LoadLiveClientRowsAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken)
    {
        var active = await _db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId) && v.Status == VacancyStatus.Active)
            .GroupBy(v => v.CompanyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var pending = await _db.Applications.AsNoTracking()
            .Where(a => companyIds.Contains(a.Vacancy.CompanyId)
                        && a.EmailVerifiedAt != null
                        && a.Status == ApplicationStatus.Pending)
            .GroupBy(a => a.Vacancy.CompanyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var boostRows = await _db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId) && v.Status == VacancyStatus.Active && v.IsHighlighted)
            .Select(v => new { v.CompanyId, v.IsHighlighted, v.HighlightedUntil })
            .ToListAsync(cancellationToken);
        var boosts = boostRows
            .Where(v => VacancyHighlightRules.IsActive(v.IsHighlighted, v.HighlightedUntil, utcNow))
            .GroupBy(v => v.CompanyId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new Dictionary<Guid, (int, int, int)>();
        foreach (var id in companyIds)
        {
            result[id] = (
                active.GetValueOrDefault(id),
                pending.GetValueOrDefault(id),
                boosts.GetValueOrDefault(id));
        }

        return result;
    }
}
