using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class MetricsQueryService : IMetricsQueryService
{
    private readonly JobsyDbContext _db;

    public MetricsQueryService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default)
    {
        var metricsPeriod = MetricsPeriodParser.Parse(period);
        var (from, to) = MetricsPeriodParser.ResolveRange(metricsPeriod);
        var periodKey = metricsPeriod.ToString().ToLowerInvariant();

        var vacancyQuery = _db.Vacancies.AsNoTracking().AsQueryable();
        if (companyIds is not null)
        {
            vacancyQuery = vacancyQuery.Where(v => companyIds.Contains(v.CompanyId));
        }

        var vacancyIds = await vacancyQuery.Select(v => v.Id).ToListAsync(cancellationToken);

        var purchased = await SumTokensAsync(TokenTransactionKind.Purchase, from, to, companyIds, cancellationToken);
        var spent = Math.Abs(await SumTokensAsync(TokenTransactionKind.Spend, from, to, companyIds, cancellationToken));

        var activeVacancies = await vacancyQuery.CountAsync(v => v.Status == VacancyStatus.Active, cancellationToken);
        var employerVacancies = await vacancyQuery.CountAsync(
            v => v.Status == VacancyStatus.Active && v.Company.Type == CompanyType.Employer, cancellationToken);
        var intermediaryVacancies = await vacancyQuery.CountAsync(
            v => v.Status == VacancyStatus.Active && v.Company.Type == CompanyType.Intermediary, cancellationToken);

        var applications = await _db.Applications.AsNoTracking()
            .Where(a => vacancyIds.Contains(a.VacancyId)
                        && a.EmailVerifiedAt != null
                        && a.CreatedAt >= from && a.CreatedAt <= to)
            .CountAsync(cancellationToken);

        var clicks = await _db.VacancyClicks.AsNoTracking()
            .Where(c => vacancyIds.Contains(c.VacancyId) && c.CreatedAt >= from && c.CreatedAt <= to)
            .CountAsync(cancellationToken);

        var impressions = await _db.VacancySearchImpressions.AsNoTracking()
            .Where(i => vacancyIds.Contains(i.VacancyId) && i.CreatedAt >= from && i.CreatedAt <= to)
            .CountAsync(cancellationToken);

        var shares = await _db.VacancyShares.AsNoTracking()
            .Where(s => vacancyIds.Contains(s.VacancyId) && s.CreatedAt >= from && s.CreatedAt <= to)
            .CountAsync(cancellationToken);

        var likes = await _db.VacancyLikes.AsNoTracking()
            .Where(l => vacancyIds.Contains(l.VacancyId) && l.CreatedAt >= from && l.CreatedAt <= to)
            .CountAsync(cancellationToken);

        var pushBoms = await _db.TokenTransactions.AsNoTracking()
            .Where(t => t.Reason == TokenSpendReason.PushBom && t.CreatedAt >= from && t.CreatedAt <= to)
            .Where(t => companyIds == null || companyIds.Contains(t.CompanyId))
            .CountAsync(cancellationToken);

        var extensions = await _db.TokenTransactions.AsNoTracking()
            .Where(t => t.Reason == TokenSpendReason.Extend && t.CreatedAt >= from && t.CreatedAt <= to)
            .Where(t => companyIds == null || companyIds.Contains(t.CompanyId))
            .CountAsync(cancellationToken);

        var errors = await _db.PlatformLogs.AsNoTracking()
            .Where(l => l.Level == PlatformLogLevel.Error && l.CreatedAt >= from && l.CreatedAt <= to)
            .CountAsync(cancellationToken);

        var openForWork = await _db.Users.AsNoTracking()
            .CountAsync(u => u.Role == UserRole.Candidate && u.IsActive && u.OpenForWork, cancellationToken);
        var allUsers = await _db.Users.AsNoTracking()
            .CountAsync(u => u.IsActive, cancellationToken);

        var employers = await _db.Companies.AsNoTracking()
            .CountAsync(c => c.Type == CompanyType.Employer, cancellationToken);
        var intermediaries = await _db.Companies.AsNoTracking()
            .CountAsync(c => c.Type == CompanyType.Intermediary, cancellationToken);

        var metrics = new List<MetricCountDto>
        {
            new("tokens_purchased", "Aangeschafte tokens", periodKey, purchased),
            new("tokens_spent", "Gebruikte tokens", periodKey, spent),
            new("active_vacancies", "Actieve vacatures", periodKey, activeVacancies),
            new("active_vacancies_employers", "Actieve vacatures (bedrijven)", periodKey, employerVacancies),
            new("active_vacancies_intermediaries", "Actieve vacatures (intermediairs)", periodKey, intermediaryVacancies),
            new("users_open_for_work", "Open for work", periodKey, openForWork),
            new("users_active", "Actieve gebruikers", periodKey, allUsers),
            new("applications", "Sollicitaties", periodKey, applications),
            new("impressions", "Getoond na zoekactie", periodKey, impressions),
            new("clicks", "Vacatureclicks", periodKey, clicks),
            new("shares", "Gedeelde vacatures", periodKey, shares),
            new("likes", "Gelikete vacatures", periodKey, likes),
            new("errors", "Errors", periodKey, errors),
            new("pushboms", "Pushboms", periodKey, pushBoms),
            new("extensions", "Verlengingen", periodKey, extensions),
            new("companies_employers", "Bedrijven", periodKey, employers),
            new("companies_intermediaries", "Intermediairs", periodKey, intermediaries)
        };

        if (includePlatformOnly)
        {
            var siteVisitRows = await _db.SiteVisits.AsNoTracking()
                .Where(v => v.CreatedAt >= from && v.CreatedAt <= to)
                .Select(v => new { v.Id, v.UserId, v.AnonymousKey })
                .ToListAsync(cancellationToken);

            var siteVisits = siteVisitRows.Count;
            var uniqueVisitors = siteVisitRows
                .Select(v => v.UserId is Guid uid
                    ? "u:" + uid.ToString()
                    : "a:" + (v.AnonymousKey ?? v.Id.ToString()))
                .Distinct(StringComparer.Ordinal)
                .Count();

            var clicksIndex = metrics.FindIndex(m => m.Key == "clicks");
            var insertAt = clicksIndex >= 0 ? clicksIndex + 1 : metrics.Count;
            metrics.Insert(insertAt, new MetricCountDto("site_visits_unique", "Sitebezoeken (uniek)", periodKey, uniqueVisitors));
            metrics.Insert(insertAt, new MetricCountDto("site_visits", "Sitebezoeken", periodKey, siteVisits));

            var companiesWithApi = await _db.Companies.AsNoTracking()
                .CountAsync(c => c.ApiKeys.Any(k => k.IsActive), cancellationToken);
            var companiesWithCsv = await _db.Companies.AsNoTracking()
                .CountAsync(c => c.CsvBatchImportEnabled && c.ParentCompanyId == null, cancellationToken);
            var unpublished = await _db.Vacancies.AsNoTracking()
                .CountAsync(v => v.Status == VacancyStatus.Draft && v.PublishedAtUtc == null, cancellationToken);

            var reengagementSent = await _db.Companies.AsNoTracking()
                .CountAsync(c => c.ReengagementEmailSentAtUtc != null, cancellationToken);
            var reengagementReactivated = await CountReengagementReactivatedAsync(cancellationToken);

            metrics.Add(new MetricCountDto("companies_with_api", "Bedrijven met API-koppeling", periodKey, companiesWithApi));
            metrics.Add(new MetricCountDto("companies_with_csv", "Bedrijven met CSV-import", periodKey, companiesWithCsv));
            metrics.Add(new MetricCountDto("unpublished_vacancies", "Ongepubliceerde concepten", periodKey, unpublished));
            metrics.Add(new MetricCountDto("reengagement_emails_sent", "We-missen-je mails verstuurd", periodKey, reengagementSent));
            metrics.Add(new MetricCountDto(
                "reengagement_reactivated",
                "We-missen-je conversie",
                periodKey,
                reengagementSent == 0
                    ? 0
                    : Math.Round(100m * reengagementReactivated / reengagementSent, 1)));
        }

        if (!includePlatformOnly)
        {
            metrics = metrics.Where(m => !MetricsKeys.PlatformOnly.Contains(m.Key)).ToList();
        }

        return await AttachSparklinesAsync(
            metrics,
            vacancyIds,
            from,
            to,
            metricsPeriod,
            includePlatformOnly,
            companyIds,
            cancellationToken);
    }

    public async Task<VacancyPerformanceBoardDto> GetVacancyPerformanceAsync(
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        int take = 3,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 10);
        var metricsPeriod = MetricsPeriodParser.Parse(period);
        var (from, to) = MetricsPeriodParser.ResolveRange(metricsPeriod);
        var periodKey = metricsPeriod.ToString().ToLowerInvariant();

        var vacancyQuery = _db.Vacancies.AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Active);
        if (companyIds is not null)
        {
            vacancyQuery = vacancyQuery.Where(v => companyIds.Contains(v.CompanyId));
        }

        // Project scores in the query so Top/Flop only materialize `take` rows each.
        var scored = vacancyQuery.Select(v => new
        {
            v.Id,
            v.Title,
            CompanyName = v.Company.Name,
            Clicks = v.Clicks.Count(c => c.CreatedAt >= from && c.CreatedAt <= to),
            Impressions = v.SearchImpressions.Count(i => i.CreatedAt >= from && i.CreatedAt <= to),
            Applications = v.Applications.Count(a =>
                a.EmailVerifiedAt != null && a.CreatedAt >= from && a.CreatedAt <= to)
        });

        var topRows = await scored
            .OrderByDescending(v => v.Clicks)
            .ThenByDescending(v => v.Impressions)
            .ThenByDescending(v => v.Applications)
            .ThenBy(v => v.Title)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (topRows.Count == 0)
        {
            return new VacancyPerformanceBoardDto(periodKey, [], []);
        }

        var top = topRows
            .Select(v => new VacancyPerformanceItemDto(
                v.Id, v.Title, v.CompanyName, v.Impressions, v.Clicks, v.Applications))
            .ToList();

        var topIds = top.Select(t => t.VacancyId).ToList();

        // Flop never overlaps Top. With ≤ take active vacancies, Flop stays empty.
        var flopRows = await scored
            .Where(v => !topIds.Contains(v.Id))
            .OrderBy(v => v.Clicks)
            .ThenBy(v => v.Impressions)
            .ThenBy(v => v.Applications)
            .ThenBy(v => v.Title)
            .Take(take)
            .ToListAsync(cancellationToken);

        var flop = flopRows
            .Select(v => new VacancyPerformanceItemDto(
                v.Id, v.Title, v.CompanyName, v.Impressions, v.Clicks, v.Applications))
            .ToList();

        return new VacancyPerformanceBoardDto(periodKey, top, flop);
    }

    public async Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
        string key,
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        string period,
        CancellationToken cancellationToken = default)
    {
        // Caller should Forbid platform-only keys; defensive empty when not allowed.
        if (!includePlatformOnly && MetricsKeys.PlatformOnly.Contains(key))
        {
            return Array.Empty<MetricDrilldownItemDto>();
        }

        var metricsPeriod = MetricsPeriodParser.Parse(period);
        var (from, to) = MetricsPeriodParser.ResolveRange(metricsPeriod);

        var vacancyIds = companyIds is null
            ? await _db.Vacancies.AsNoTracking().Select(v => v.Id).ToListAsync(cancellationToken)
            : await _db.Vacancies.AsNoTracking()
                .Where(v => companyIds.Contains(v.CompanyId))
                .Select(v => v.Id)
                .ToListAsync(cancellationToken);

        return key.ToLowerInvariant() switch
        {
            "tokens_purchased" or "tokens_spent" => await TokenDrilldownAsync(key, from, to, companyIds, cancellationToken),
            "applications" => await ApplicationsDrilldownAsync(vacancyIds, from, to, cancellationToken),
            "impressions" => (await _db.VacancySearchImpressions.AsNoTracking()
                    .Where(i => vacancyIds.Contains(i.VacancyId) && i.CreatedAt >= from && i.CreatedAt <= to)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new { i.Id, Title = i.Vacancy.Title, Email = i.User != null ? i.User.Email : null, i.CreatedAt })
                    .ToListAsync(cancellationToken))
                .Select(i => new MetricDrilldownItemDto(
                    i.Id, i.Title, EngagementLabel(i.Email, includePlatformOnly), i.CreatedAt, null))
                .ToList(),
            "clicks" => (await _db.VacancyClicks.AsNoTracking()
                    .Where(c => vacancyIds.Contains(c.VacancyId) && c.CreatedAt >= from && c.CreatedAt <= to)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new { c.Id, Title = c.Vacancy.Title, Email = c.User != null ? c.User.Email : null, c.CreatedAt })
                    .ToListAsync(cancellationToken))
                .Select(c => new MetricDrilldownItemDto(
                    c.Id, c.Title, EngagementLabel(c.Email, includePlatformOnly), c.CreatedAt, null))
                .ToList(),
            "shares" => await _db.VacancyShares.AsNoTracking()
                .Where(s => vacancyIds.Contains(s.VacancyId) && s.CreatedAt >= from && s.CreatedAt <= to)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new MetricDrilldownItemDto(
                    s.Id, s.Vacancy.Title, s.Channel.ToString(), s.CreatedAt, null))
                .ToListAsync(cancellationToken),
            "likes" => (await _db.VacancyLikes.AsNoTracking()
                    .Where(l => vacancyIds.Contains(l.VacancyId) && l.CreatedAt >= from && l.CreatedAt <= to)
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new { l.Id, Title = l.Vacancy.Title, Email = (string?)l.User.Email, l.CreatedAt })
                    .ToListAsync(cancellationToken))
                .Select(l => new MetricDrilldownItemDto(
                    l.Id, l.Title, EngagementLabel(l.Email, includePlatformOnly), l.CreatedAt, null))
                .ToList(),
            "site_visits" => await SiteVisitsDrilldownAsync(from, to, cancellationToken),
            "site_visits_unique" => await SiteVisitsUniqueDrilldownAsync(from, to, cancellationToken),
            "errors" => await _db.PlatformLogs.AsNoTracking()
                .Where(l => l.Level == PlatformLogLevel.Error && l.CreatedAt >= from && l.CreatedAt <= to)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new MetricDrilldownItemDto(
                    l.Id, l.Category, l.Message, l.CreatedAt, null))
                .ToListAsync(cancellationToken),
            "pushboms" => await TokenReasonDrilldownAsync(TokenSpendReason.PushBom, from, to, companyIds, cancellationToken),
            "extensions" => await TokenReasonDrilldownAsync(TokenSpendReason.Extend, from, to, companyIds, cancellationToken),
            "active_vacancies" => await ActiveVacanciesDrilldownAsync(companyIds, type: null, cancellationToken),
            "active_vacancies_employers" => await ActiveVacanciesDrilldownAsync(companyIds, CompanyType.Employer, cancellationToken),
            "active_vacancies_intermediaries" => await ActiveVacanciesDrilldownAsync(companyIds, CompanyType.Intermediary, cancellationToken),
            "users_open_for_work" => await UsersOpenForWorkDrilldownAsync(cancellationToken),
            "users_active" => await _db.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new MetricDrilldownItemDto(u.Id, u.FullName, u.Role.ToString(), DateTime.UtcNow, null))
                .ToListAsync(cancellationToken),
            "companies_employers" => await CompaniesDrilldownAsync(CompanyType.Employer, cancellationToken),
            "companies_intermediaries" => await CompaniesDrilldownAsync(CompanyType.Intermediary, cancellationToken),
            "companies_with_api" => await IntegrationCompaniesDrilldownAsync(VacancySource.Api, cancellationToken),
            "companies_with_csv" => await IntegrationCompaniesDrilldownAsync(VacancySource.Csv, cancellationToken),
            "unpublished_vacancies" => await UnpublishedVacanciesDrilldownAsync(cancellationToken),
            "reengagement_emails_sent" => await ReengagementSentDrilldownAsync(cancellationToken),
            "reengagement_reactivated" => await ReengagementReactivatedDrilldownAsync(cancellationToken),
            _ => Array.Empty<MetricDrilldownItemDto>()
        };
    }

    private async Task<int> CountReengagementReactivatedAsync(CancellationToken ct)
    {
        var sent = await _db.Companies.AsNoTracking()
            .Where(c => c.ReengagementEmailSentAtUtc != null)
            .Select(c => new { c.Id, SentAt = c.ReengagementEmailSentAtUtc!.Value })
            .ToListAsync(ct);

        var count = 0;
        foreach (var org in sent)
        {
            if (await WasActiveAfterAsync(org.Id, org.SentAt, ct))
            {
                count++;
            }
        }

        return count;
    }

    private async Task<bool> WasActiveAfterAsync(Guid orgId, DateTime afterUtc, CancellationToken ct)
    {
        var orgIds = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == orgId || c.ParentCompanyId == orgId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (await _db.Vacancies.AsNoTracking()
                .AnyAsync(v => orgIds.Contains(v.CompanyId) && v.CreatedAtUtc > afterUtc, ct))
        {
            return true;
        }

        if (await _db.Vacancies.AsNoTracking()
                .AnyAsync(v => orgIds.Contains(v.CompanyId) && v.PublishedAtUtc > afterUtc, ct))
        {
            return true;
        }

        if (await _db.Users.AsNoTracking()
                .AnyAsync(u => u.LastLoginAtUtc > afterUtc
                               && (u.CompanyId != null && orgIds.Contains(u.CompanyId.Value)
                                   || u.CompanyMemberships.Any(m => orgIds.Contains(m.CompanyId))), ct))
        {
            return true;
        }

        if (await _db.ApiKeys.AsNoTracking()
                .AnyAsync(k => orgIds.Contains(k.CompanyId) && k.LastUsedAt > afterUtc, ct))
        {
            return true;
        }

        return await _db.Companies.AsNoTracking()
            .AnyAsync(c => orgIds.Contains(c.Id) && c.LastCsvImportAtUtc > afterUtc, ct);
    }

    private async Task<List<MetricDrilldownItemDto>> IntegrationCompaniesDrilldownAsync(
        VacancySource source,
        CancellationToken ct)
    {
        if (source == VacancySource.Api)
        {
            var rows = await _db.Companies.AsNoTracking()
                .Where(c => c.ApiKeys.Any(k => k.IsActive))
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    VacancyCount = c.Vacancies.Count(v => v.CreatedVia == VacancySource.Api),
                    KeyCreated = c.ApiKeys.Where(k => k.IsActive).Max(k => (DateTime?)k.CreatedAt)
                })
                .ToListAsync(ct);

            return rows.Select(r => new MetricDrilldownItemDto(
                r.Id,
                r.Name,
                $"API · {r.VacancyCount} vacatures via API",
                r.KeyCreated ?? DateTime.UtcNow,
                r.VacancyCount)).ToList();
        }

        var csvOrgs = await _db.Companies.AsNoTracking()
            .Where(c => c.CsvBatchImportEnabled && c.ParentCompanyId == null)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.LastCsvImportAtUtc })
            .ToListAsync(ct);

        var counts = await _db.Vacancies.AsNoTracking()
            .Where(v => v.CreatedVia == VacancySource.Csv)
            .Select(v => new { v.CompanyId, ParentId = v.Company.ParentCompanyId })
            .ToListAsync(ct);

        return csvOrgs.Select(r =>
        {
            var vacancyCount = counts.Count(v => v.CompanyId == r.Id || v.ParentId == r.Id);
            return new MetricDrilldownItemDto(
                r.Id,
                r.Name,
                $"CSV · {vacancyCount} vacatures via CSV",
                r.LastCsvImportAtUtc ?? DateTime.UtcNow,
                vacancyCount);
        }).ToList();
    }

    private Task<List<MetricDrilldownItemDto>> UnpublishedVacanciesDrilldownAsync(CancellationToken ct)
        => _db.Vacancies.AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Draft && v.PublishedAtUtc == null)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => new MetricDrilldownItemDto(
                v.Id,
                v.Title,
                $"{v.Company.Name} · {v.CreatedVia} · concept",
                v.CreatedAtUtc,
                null))
            .ToListAsync(ct);

    private Task<List<MetricDrilldownItemDto>> ReengagementSentDrilldownAsync(CancellationToken ct)
        => _db.Companies.AsNoTracking()
            .Where(c => c.ReengagementEmailSentAtUtc != null)
            .OrderByDescending(c => c.ReengagementEmailSentAtUtc)
            .Select(c => new MetricDrilldownItemDto(
                c.Id,
                c.Name,
                "We missen je · verstuurd",
                c.ReengagementEmailSentAtUtc!.Value,
                null))
            .ToListAsync(ct);

    private async Task<List<MetricDrilldownItemDto>> ReengagementReactivatedDrilldownAsync(CancellationToken ct)
    {
        var sent = await _db.Companies.AsNoTracking()
            .Where(c => c.ReengagementEmailSentAtUtc != null)
            .Select(c => new { c.Id, c.Name, SentAt = c.ReengagementEmailSentAtUtc!.Value })
            .ToListAsync(ct);

        var items = new List<MetricDrilldownItemDto>();
        foreach (var org in sent)
        {
            if (!await WasActiveAfterAsync(org.Id, org.SentAt, ct))
            {
                continue;
            }

            items.Add(new MetricDrilldownItemDto(
                org.Id,
                org.Name,
                "Weer actief na we-missen-je mail",
                org.SentAt,
                null));
        }

        return items.OrderByDescending(i => i.CreatedAt).ToList();
    }

    private static string EngagementLabel(string? email, bool includePlatformOnly)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "anoniem";
        }

        return includePlatformOnly
            ? EmailServiceStub.RedactEmail(email)
            : "Gebruiker";
    }

    private async Task<List<MetricDrilldownItemDto>> SiteVisitsDrilldownAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var rows = await _db.SiteVisits.AsNoTracking()
            .Where(v => v.CreatedAt >= from && v.CreatedAt <= to)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new
            {
                v.Id,
                Path = v.Path ?? "/",
                Email = v.User != null ? v.User.Email : null,
                AnonymousKey = v.AnonymousKey,
                v.CreatedAt
            })
            .ToListAsync(ct);

        return rows.Select(v => new MetricDrilldownItemDto(
            v.Id,
            v.Path,
            v.Email is not null ? EmailServiceStub.RedactEmail(v.Email) : (v.AnonymousKey ?? "anoniem"),
            v.CreatedAt,
            null)).ToList();
    }

    private async Task<List<MetricDrilldownItemDto>> UsersOpenForWorkDrilldownAsync(CancellationToken ct)
    {
        var rows = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Candidate && u.IsActive && u.OpenForWork)
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(ct);

        return rows.Select(u => new MetricDrilldownItemDto(
            u.Id, u.FullName, EmailServiceStub.RedactEmail(u.Email), DateTime.UtcNow, null)).ToList();
    }

    private async Task<List<MetricDrilldownItemDto>> SiteVisitsUniqueDrilldownAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var rows = await _db.SiteVisits.AsNoTracking()
            .Where(v => v.CreatedAt >= from && v.CreatedAt <= to)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new
            {
                v.Id,
                v.Path,
                v.CreatedAt,
                VisitorKey = v.UserId != null
                    ? "u:" + v.UserId.Value.ToString()
                    : "a:" + (v.AnonymousKey ?? v.Id.ToString()),
                Label = v.User != null ? "Gebruiker" : (v.AnonymousKey ?? "anoniem")
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.VisitorKey)
            .Select(g =>
            {
                var first = g.OrderByDescending(x => x.CreatedAt).First();
                return new MetricDrilldownItemDto(
                    first.Id,
                    first.Label,
                    $"{g.Count()} bezoeken · {first.Path ?? "/"}",
                    first.CreatedAt,
                    g.Count());
            })
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }

    private async Task<List<MetricDrilldownItemDto>> ApplicationsDrilldownAsync(
        IReadOnlyCollection<Guid> vacancyIds,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var rows = await _db.Applications.AsNoTracking()
            .Where(a => vacancyIds.Contains(a.VacancyId)
                        && a.EmailVerifiedAt != null
                        && a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.CandidateName,
                a.CandidateCity,
                a.Status,
                VacancyTitle = a.Vacancy.Title,
                a.CreatedAt
            })
            .ToListAsync(ct);

        return rows.Select(a =>
        {
            // Progressive disclosure: names only after employer acceptance (same as applicants API).
            var reveal = a.Status is ApplicationStatus.Accepted
                or ApplicationStatus.EmployerContacting
                or ApplicationStatus.Hired;
            var title = reveal
                ? a.CandidateName
                : (string.IsNullOrWhiteSpace(a.CandidateCity) ? "Kandidaat" : a.CandidateCity);
            var subtitle = $"{a.VacancyTitle} · {a.Status}";
            return new MetricDrilldownItemDto(a.Id, title, subtitle, a.CreatedAt, null);
        }).ToList();
    }

    private async Task<List<MetricDrilldownItemDto>> ActiveVacanciesDrilldownAsync(
        IReadOnlyCollection<Guid>? companyIds,
        CompanyType? type,
        CancellationToken ct)
    {
        var query = _db.Vacancies.AsNoTracking().Where(v => v.Status == VacancyStatus.Active);
        if (companyIds is not null)
        {
            query = query.Where(v => companyIds.Contains(v.CompanyId));
        }

        if (type is not null)
        {
            query = query.Where(v => v.Company.Type == type);
        }

        return await query
            .OrderBy(v => v.Title)
            .Select(v => new MetricDrilldownItemDto(
                v.Id, v.Title, v.Company.Name, DateTime.UtcNow, null))
            .ToListAsync(ct);
    }

    private Task<List<MetricDrilldownItemDto>> CompaniesDrilldownAsync(CompanyType type, CancellationToken ct)
        => _db.Companies.AsNoTracking()
            .Where(c => c.Type == type)
            .OrderBy(c => c.Name)
            .Select(c => new MetricDrilldownItemDto(c.Id, c.Name, c.Address, DateTime.UtcNow, null))
            .ToListAsync(ct);

    private async Task<List<MetricDrilldownItemDto>> TokenDrilldownAsync(
        string key,
        DateTime from,
        DateTime to,
        IReadOnlyCollection<Guid>? companyIds,
        CancellationToken ct)
    {
        var kinds = key == "tokens_spent"
            ? new[] { TokenTransactionKind.Spend }
            : new[] { TokenTransactionKind.Purchase };

        return await _db.TokenTransactions.AsNoTracking()
            .Where(t => kinds.Contains(t.Kind) && t.CreatedAt >= from && t.CreatedAt <= to)
            .Where(t => companyIds == null || companyIds.Contains(t.CompanyId))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new MetricDrilldownItemDto(
                t.Id,
                t.Company.Name,
                $"{t.Kind}/{t.Reason}",
                t.CreatedAt,
                t.Amount))
            .ToListAsync(ct);
    }

    private async Task<List<MetricDrilldownItemDto>> TokenReasonDrilldownAsync(
        TokenSpendReason reason,
        DateTime from,
        DateTime to,
        IReadOnlyCollection<Guid>? companyIds,
        CancellationToken ct)
        => await _db.TokenTransactions.AsNoTracking()
            .Where(t => t.Reason == reason && t.CreatedAt >= from && t.CreatedAt <= to)
            .Where(t => companyIds == null || companyIds.Contains(t.CompanyId))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new MetricDrilldownItemDto(
                t.Id,
                t.Company.Name,
                t.Vacancy != null ? t.Vacancy.Title : t.Note,
                t.CreatedAt,
                t.Amount))
            .ToListAsync(ct);

    private async Task<decimal> SumTokensAsync(
        TokenTransactionKind kind,
        DateTime from,
        DateTime to,
        IReadOnlyCollection<Guid>? companyIds,
        CancellationToken ct)
    {
        var query = _db.TokenTransactions.AsNoTracking()
            .Where(t => t.Kind == kind && t.CreatedAt >= from && t.CreatedAt <= to);
        if (companyIds is not null)
        {
            query = query.Where(t => companyIds.Contains(t.CompanyId));
        }

        return await query.SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
    }

    private static readonly HashSet<string> SparklineKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "clicks", "impressions", "applications", "shares", "likes",
        "site_visits", "site_visits_unique", "tokens_purchased", "tokens_spent"
    };

    private async Task<List<MetricCountDto>> AttachSparklinesAsync(
        List<MetricCountDto> metrics,
        List<Guid> vacancyIds,
        DateTime from,
        DateTime to,
        MetricsPeriod period,
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        CancellationToken ct)
    {
        var bucketCount = SparklineBucketCount(period);
        var cache = new Dictionary<string, IReadOnlyList<decimal>>(StringComparer.OrdinalIgnoreCase);
        var result = new List<MetricCountDto>(metrics.Count);

        // Sequential: DbContext is not safe for concurrent queries.
        foreach (var metric in metrics)
        {
            if (!SparklineKeys.Contains(metric.Key))
            {
                result.Add(metric);
                continue;
            }

            if (!cache.TryGetValue(metric.Key, out var points))
            {
                points = await LoadSparklinePointsAsync(
                    metric.Key,
                    vacancyIds,
                    from,
                    to,
                    bucketCount,
                    includePlatformOnly,
                    companyIds,
                    ct);
                cache[metric.Key] = points;
            }

            result.Add(metric with { Sparkline = points });
        }

        return result;
    }

    private async Task<IReadOnlyList<decimal>> LoadSparklinePointsAsync(
        string key,
        List<Guid> vacancyIds,
        DateTime from,
        DateTime to,
        int bucketCount,
        bool includePlatformOnly,
        IReadOnlyCollection<Guid>? companyIds,
        CancellationToken ct)
    {
        switch (key.ToLowerInvariant())
        {
            case "clicks":
                return BucketTimestamps(
                    await _db.VacancyClicks.AsNoTracking()
                        .Where(c => vacancyIds.Contains(c.VacancyId) && c.CreatedAt >= from && c.CreatedAt <= to)
                        .Select(c => c.CreatedAt)
                        .ToListAsync(ct),
                    from, to, bucketCount);

            case "impressions":
                return BucketTimestamps(
                    await _db.VacancySearchImpressions.AsNoTracking()
                        .Where(i => vacancyIds.Contains(i.VacancyId) && i.CreatedAt >= from && i.CreatedAt <= to)
                        .Select(i => i.CreatedAt)
                        .ToListAsync(ct),
                    from, to, bucketCount);

            case "applications":
                return BucketTimestamps(
                    await _db.Applications.AsNoTracking()
                        .Where(a => vacancyIds.Contains(a.VacancyId)
                                    && a.EmailVerifiedAt != null
                                    && a.CreatedAt >= from && a.CreatedAt <= to)
                        .Select(a => a.CreatedAt)
                        .ToListAsync(ct),
                    from, to, bucketCount);

            case "shares":
                return BucketTimestamps(
                    await _db.VacancyShares.AsNoTracking()
                        .Where(s => vacancyIds.Contains(s.VacancyId) && s.CreatedAt >= from && s.CreatedAt <= to)
                        .Select(s => s.CreatedAt)
                        .ToListAsync(ct),
                    from, to, bucketCount);

            case "likes":
                return BucketTimestamps(
                    await _db.VacancyLikes.AsNoTracking()
                        .Where(l => vacancyIds.Contains(l.VacancyId) && l.CreatedAt >= from && l.CreatedAt <= to)
                        .Select(l => l.CreatedAt)
                        .ToListAsync(ct),
                    from, to, bucketCount);

            case "site_visits" when includePlatformOnly:
                return BucketTimestamps(
                    await _db.SiteVisits.AsNoTracking()
                        .Where(v => v.CreatedAt >= from && v.CreatedAt <= to)
                        .Select(v => v.CreatedAt)
                        .ToListAsync(ct),
                    from, to, bucketCount);

            case "site_visits_unique" when includePlatformOnly:
            {
                var rows = await _db.SiteVisits.AsNoTracking()
                    .Where(v => v.CreatedAt >= from && v.CreatedAt <= to)
                    .Select(v => new { v.CreatedAt, v.UserId, v.AnonymousKey, v.Id })
                    .ToListAsync(ct);
                return BucketUniqueVisitors(rows.Select(v => (
                    v.CreatedAt,
                    v.UserId is Guid uid ? "u:" + uid : "a:" + (v.AnonymousKey ?? v.Id.ToString())
                )).ToList(), from, to, bucketCount);
            }

            case "tokens_purchased":
            {
                var rows = await _db.TokenTransactions.AsNoTracking()
                    .Where(t => t.Kind == TokenTransactionKind.Purchase && t.CreatedAt >= from && t.CreatedAt <= to)
                    .Where(t => companyIds == null || companyIds.Contains(t.CompanyId))
                    .Select(t => new { t.CreatedAt, Amount = (decimal)Math.Abs(t.Amount) })
                    .ToListAsync(ct);
                return BucketAmounts(rows.Select(r => (r.CreatedAt, r.Amount)).ToList(), from, to, bucketCount);
            }

            case "tokens_spent":
            {
                var rows = await _db.TokenTransactions.AsNoTracking()
                    .Where(t => t.Kind == TokenTransactionKind.Spend && t.CreatedAt >= from && t.CreatedAt <= to)
                    .Where(t => companyIds == null || companyIds.Contains(t.CompanyId))
                    .Select(t => new { t.CreatedAt, Amount = (decimal)Math.Abs(t.Amount) })
                    .ToListAsync(ct);
                return BucketAmounts(rows.Select(r => (r.CreatedAt, r.Amount)).ToList(), from, to, bucketCount);
            }

            default:
                return new decimal[bucketCount];
        }
    }

    private static int SparklineBucketCount(MetricsPeriod period) => period switch
    {
        MetricsPeriod.Day => 12,
        MetricsPeriod.Week => 7,
        MetricsPeriod.Month => 10,
        MetricsPeriod.Quarter => 12,
        MetricsPeriod.Year => 12,
        _ => 7
    };

    private static int BucketIndex(DateTime stamp, DateTime from, long bucketTicks, int bucketCount)
    {
        var offset = (stamp - from).Ticks;
        var index = (int)(offset / bucketTicks);
        if (index < 0) return 0;
        if (index >= bucketCount) return bucketCount - 1;
        return index;
    }

    private static IReadOnlyList<decimal> BucketTimestamps(
        IReadOnlyList<DateTime> stamps,
        DateTime from,
        DateTime to,
        int bucketCount)
    {
        var buckets = new decimal[bucketCount];
        if (bucketCount <= 0) return buckets;

        var spanTicks = Math.Max((to - from).Ticks, TimeSpan.FromHours(1).Ticks);
        var bucketTicks = spanTicks / bucketCount;

        foreach (var stamp in stamps)
        {
            buckets[BucketIndex(stamp, from, bucketTicks, bucketCount)] += 1;
        }

        return buckets;
    }

    private static IReadOnlyList<decimal> BucketAmounts(
        IReadOnlyList<(DateTime Stamp, decimal Amount)> rows,
        DateTime from,
        DateTime to,
        int bucketCount)
    {
        var buckets = new decimal[bucketCount];
        if (bucketCount <= 0) return buckets;

        var spanTicks = Math.Max((to - from).Ticks, TimeSpan.FromHours(1).Ticks);
        var bucketTicks = spanTicks / bucketCount;

        foreach (var (stamp, amount) in rows)
        {
            buckets[BucketIndex(stamp, from, bucketTicks, bucketCount)] += amount;
        }

        return buckets;
    }

    private static IReadOnlyList<decimal> BucketUniqueVisitors(
        IReadOnlyList<(DateTime Stamp, string VisitorKey)> rows,
        DateTime from,
        DateTime to,
        int bucketCount)
    {
        var sets = Enumerable.Range(0, bucketCount).Select(_ => new HashSet<string>(StringComparer.Ordinal)).ToArray();
        if (bucketCount <= 0) return Array.Empty<decimal>();

        var spanTicks = Math.Max((to - from).Ticks, TimeSpan.FromHours(1).Ticks);
        var bucketTicks = spanTicks / bucketCount;

        foreach (var (stamp, key) in rows)
        {
            sets[BucketIndex(stamp, from, bucketTicks, bucketCount)].Add(key);
        }

        return sets.Select(s => (decimal)s.Count).ToArray();
    }
}
