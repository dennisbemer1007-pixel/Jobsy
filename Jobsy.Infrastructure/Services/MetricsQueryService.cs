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
        }

        if (!includePlatformOnly)
        {
            metrics = metrics.Where(m => !MetricsKeys.PlatformOnly.Contains(m.Key)).ToList();
        }

        return metrics;
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
            _ => Array.Empty<MetricDrilldownItemDto>()
        };
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
}
