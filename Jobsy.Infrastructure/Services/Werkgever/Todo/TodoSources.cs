using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.Werkgever.Todo;

internal static class TodoDtoFactory
{
    public static WerkgeverTodoItemDto Create(
        WerkgeverTodoKind kind,
        WerkgeverTodoSeverity severity,
        string titleKey,
        IReadOnlyList<string> titleArgs,
        string? metaKey,
        IReadOnlyList<string>? metaArgs,
        WerkgeverTodoActionKind action,
        string href,
        int count,
        IReadOnlyList<Guid> companyIds)
        => new(
            Kind: kind.ToString(),
            Severity: severity.ToString(),
            TitleKey: titleKey,
            TitleArgs: titleArgs,
            MetaKey: metaKey,
            MetaArgs: metaArgs,
            ActionKind: action == WerkgeverTodoActionKind.None ? "" : action.ToString(),
            Href: href,
            Count: count,
            CompanyIds: companyIds);
}

public sealed class PublishRequestsTodoSource : ITodoSource
{
    private readonly JobsyDbContext _db;
    public PublishRequestsTodoSource(JobsyDbContext db) => _db = db;
    public WerkgeverTodoKind Kind => WerkgeverTodoKind.PublishRequests;

    public async Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var ids = companyIds.ToHashSet();
        var rows = await _db.Vacancies.AsNoTracking()
            .Where(v => ids.Contains(v.CompanyId) && v.Status == VacancyStatus.PendingApproval)
            .Select(v => new { v.CompanyId, v.Company.Name, v.Title })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var action = role switch
        {
            WerkgeverDashboardRole.Bedrijfsmanager => WerkgeverTodoActionKind.Beoordelen,
            _ => WerkgeverTodoActionKind.None
        };
        var metaKey = role switch
        {
            WerkgeverDashboardRole.Regiomanager => "WgTodo.PublishRequests.MetaRm",
            WerkgeverDashboardRole.Vestigingsmanager => "WgTodo.PublishRequests.MetaVm",
            _ => "WgTodo.PublishRequests.Meta"
        };
        var names = rows.Select(r => r.Name).Distinct().Take(3).ToList();
        return TodoDtoFactory.Create(
            Kind,
            WerkgeverTodoSeverity.Warning,
            "WgTodo.PublishRequests.Title",
            [rows.Count.ToString()],
            metaKey,
            [string.Join(", ", names)],
            action,
            "/werkgever/vacatures?tab=wacht",
            rows.Count,
            rows.Select(r => r.CompanyId).Distinct().ToList());
    }
}

public sealed class ApplicationsOverdueTodoSource : ITodoSource
{
    private readonly JobsyDbContext _db;
    public ApplicationsOverdueTodoSource(JobsyDbContext db) => _db = db;
    public WerkgeverTodoKind Kind => WerkgeverTodoKind.ApplicationsOverdue;

    public async Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var ids = companyIds.ToHashSet();
        var cutoff = utcNow.AddHours(-WerkgeverDashboardRules.OverduePendingHours);
        var rows = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.Vacancy.CompanyId)
                        && a.Status == ApplicationStatus.Pending
                        && a.CreatedAt < cutoff)
            .GroupBy(a => new { a.Vacancy.CompanyId, a.Vacancy.Company.Name })
            .Select(g => new { g.Key.CompanyId, g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var total = rows.Sum(r => r.Count);
        var top = rows.Take(2).Select(r => $"{r.Name} ({r.Count})");
        return TodoDtoFactory.Create(
            Kind,
            WerkgeverTodoSeverity.Danger,
            "WgTodo.ApplicationsOverdue.Title",
            [total.ToString()],
            "WgTodo.ApplicationsOverdue.Meta",
            [string.Join(", ", top)],
            WerkgeverTodoActionKind.Bekijken,
            "/werkgever/sollicitaties?filter=overdue",
            total,
            rows.Select(r => r.CompanyId).ToList());
    }
}

/// <summary>Included because <see cref="Jobsy.Core.Entities.Vacancy.EndDate"/> exists.</summary>
public sealed class VacanciesExpiringTodoSource : ITodoSource
{
    private readonly JobsyDbContext _db;
    public VacanciesExpiringTodoSource(JobsyDbContext db) => _db = db;
    public WerkgeverTodoKind Kind => WerkgeverTodoKind.VacanciesExpiring;

    public async Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var ids = companyIds.ToHashSet();
        var today = DateOnly.FromDateTime(utcNow);
        var until = today.AddDays(WerkgeverDashboardRules.VacanciesExpiringDays);
        var rows = await _db.Vacancies.AsNoTracking()
            .Where(v => ids.Contains(v.CompanyId)
                        && v.Status == VacancyStatus.Active
                        && v.EndDate >= today
                        && v.EndDate <= until)
            .Select(v => new { v.CompanyId, v.Company.Name })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var action = role switch
        {
            WerkgeverDashboardRole.Bedrijfsmanager or WerkgeverDashboardRole.Vestigingsmanager
                => WerkgeverTodoActionKind.Verlengen,
            WerkgeverDashboardRole.Regiomanager => WerkgeverTodoActionKind.Bekijken,
            _ => WerkgeverTodoActionKind.None
        };
        var names = rows.Select(r => r.Name).Distinct().Take(3);
        return TodoDtoFactory.Create(
            Kind,
            WerkgeverTodoSeverity.Warning,
            "WgTodo.VacanciesExpiring.Title",
            [rows.Count.ToString()],
            "WgTodo.VacanciesExpiring.Meta",
            [string.Join(", ", names)],
            action,
            "/werkgever/vacatures?tab=verloopt",
            rows.Count,
            rows.Select(r => r.CompanyId).Distinct().ToList());
    }
}

public sealed class LowTokensTodoSource : ITodoSource
{
    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _tokens;

    public LowTokensTodoSource(JobsyDbContext db, ITokenLedgerService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public WerkgeverTodoKind Kind => WerkgeverTodoKind.LowTokens;

    public async Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        // RM: hidden. Only when tokens are managed per vestiging (enterprise allocation).
        if (role == WerkgeverDashboardRole.Regiomanager)
        {
            return null;
        }

        var ids = companyIds.ToHashSet();
        var managed = await _db.Companies.AsNoTracking()
            .Where(c => ids.Contains(c.Id) && c.TokensManagedByEnterprise)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);
        if (managed.Count == 0)
        {
            return null;
        }

        var low = new List<(Guid Id, string Name, decimal Balance)>();
        foreach (var c in managed)
        {
            var bal = await _tokens.GetBalanceAsync(c.Id, cancellationToken);
            if (bal <= WerkgeverDashboardRules.LowTokenBalance)
            {
                low.Add((c.Id, c.Name, bal));
            }
        }

        if (low.Count == 0)
        {
            return null;
        }

        var first = low.OrderBy(x => x.Balance).First();
        var action = role switch
        {
            WerkgeverDashboardRole.Bedrijfsmanager => WerkgeverTodoActionKind.TokensVerdelen,
            WerkgeverDashboardRole.Vestigingsmanager => WerkgeverTodoActionKind.TokensAanvragen,
            _ => WerkgeverTodoActionKind.None
        };
        var metaKey = role == WerkgeverDashboardRole.Vestigingsmanager
            ? "WgTodo.LowTokens.MetaVm"
            : "WgTodo.LowTokens.Meta";
        var oneToken = first.Balance == 1m;
        return TodoDtoFactory.Create(
            Kind,
            WerkgeverTodoSeverity.Warning,
            oneToken ? "WgTodo.LowTokens.TitleOne" : "WgTodo.LowTokens.Title",
            oneToken ? [first.Name] : [first.Name, first.Balance.ToString("0")],
            metaKey,
            null,
            action,
            role == WerkgeverDashboardRole.Vestigingsmanager
                ? "/werkgever/tokens?aanvragen=1"
                : "/werkgever/tokens/verbruik",
            low.Count,
            low.Select(x => x.Id).ToList());
    }
}

public sealed class NoManagerTodoSource : ITodoSource
{
    private readonly JobsyDbContext _db;
    public NoManagerTodoSource(JobsyDbContext db) => _db = db;
    public WerkgeverTodoKind Kind => WerkgeverTodoKind.NoManager;

    public async Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        // VM never sees this (own vestiging).
        if (role == WerkgeverDashboardRole.Vestigingsmanager)
        {
            return null;
        }

        var ids = companyIds.ToHashSet();
        // Only child vestigingen (or all non-parent orgs that look like branches).
        var companies = await _db.Companies.AsNoTracking()
            .Where(c => ids.Contains(c.Id) && c.ParentCompanyId != null)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);
        if (companies.Count == 0)
        {
            return null;
        }

        var withManager = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive
                        && u.Role == UserRole.BranchManager
                        && u.CompanyId != null
                        && ids.Contains(u.CompanyId.Value))
            .Select(u => u.CompanyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var managed = withManager.ToHashSet();
        var missing = companies.Where(c => !managed.Contains(c.Id)).ToList();
        if (missing.Count == 0)
        {
            return null;
        }

        var first = missing[0];
        var action = role == WerkgeverDashboardRole.Bedrijfsmanager
            ? WerkgeverTodoActionKind.IemandUitnodigen
            : WerkgeverTodoActionKind.None;
        var metaKey = role == WerkgeverDashboardRole.Regiomanager
            ? "WgTodo.NoManager.MetaRm"
            : "WgTodo.NoManager.Meta";
        return TodoDtoFactory.Create(
            Kind,
            WerkgeverTodoSeverity.Info,
            "WgTodo.NoManager.Title",
            [first.Name],
            metaKey,
            null,
            action,
            $"/werkgever/organisatie/team?invite=vestiging:{first.Id:D}",
            missing.Count,
            missing.Select(m => m.Id).ToList());
    }
}

public sealed class TakeoversTodoSource : ITodoSource
{
    private readonly JobsyDbContext _db;
    public TakeoversTodoSource(JobsyDbContext db) => _db = db;
    public WerkgeverTodoKind Kind => WerkgeverTodoKind.Takeovers;

    public async Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (role == WerkgeverDashboardRole.Regiomanager)
        {
            return null;
        }

        var ids = companyIds.ToHashSet();
        var rows = await _db.EstablishmentTakeoverRequests.AsNoTracking()
            .Where(t => ids.Contains(t.TargetCompanyId) && t.Status == TakeoverRequestStatus.Pending)
            .Select(t => new { t.TargetCompanyId, t.TargetCompany.Name, t.CreatedAt })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var first = rows[0];
        return TodoDtoFactory.Create(
            Kind,
            WerkgeverTodoSeverity.Warning,
            "WgTodo.Takeovers.Title",
            [first.Name],
            "WgTodo.Takeovers.Meta",
            [first.CreatedAt.ToString("dd-MM")],
            WerkgeverTodoActionKind.Beoordelen,
            "/werkgever/overnames",
            rows.Count,
            rows.Select(r => r.TargetCompanyId).Distinct().ToList());
    }
}

public sealed class TokenRequestsTodoSource : ITodoSource
{
    private readonly JobsyDbContext _db;
    public TokenRequestsTodoSource(JobsyDbContext db) => _db = db;
    public WerkgeverTodoKind Kind => WerkgeverTodoKind.TokenRequests;

    public async Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        // BM only actionable; VM sees own open requests as info on tokens page.
        if (role is not WerkgeverDashboardRole.Bedrijfsmanager)
        {
            return null;
        }

        var ids = companyIds.ToHashSet();
        var rows = await _db.TokenRequests.AsNoTracking()
            .Where(r => r.Status == TokenRequestStatus.Open
                        && (ids.Contains(r.BranchCompanyId) || ids.Contains(r.OrganisationCompanyId)))
            .Select(r => new { r.Id, r.BranchCompanyId, BranchName = r.BranchCompany.Name, r.Amount })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var first = rows[0];
        var titleArgs = rows.Count == 1
            ? new[] { first.BranchName, first.Amount.ToString() }
            : new[] { rows.Count.ToString(), "" };

        return TodoDtoFactory.Create(
            Kind,
            WerkgeverTodoSeverity.Warning,
            rows.Count == 1 ? "WgTodo.TokenRequests.TitleOne" : "WgTodo.TokenRequests.TitleMany",
            titleArgs,
            "WgTodo.TokenRequests.Meta",
            null,
            WerkgeverTodoActionKind.TokensVerdelen,
            $"/werkgever/tokens?request={first.Id:D}",
            rows.Count,
            rows.Select(r => r.BranchCompanyId).Distinct().ToList());
    }
}

public sealed class InsightsRequestsTodoSource : ITodoSource
{
    private readonly JobsyDbContext _db;
    public InsightsRequestsTodoSource(JobsyDbContext db) => _db = db;
    public WerkgeverTodoKind Kind => WerkgeverTodoKind.InsightsRequests;

    public async Task<WerkgeverTodoItemDto?> BuildAsync(
        IReadOnlyList<Guid> companyIds,
        WerkgeverDashboardRole role,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (role is not WerkgeverDashboardRole.Bedrijfsmanager)
        {
            return null;
        }

        var ids = companyIds.ToHashSet();
        var rows = await _db.CandidateInsightsUnlockRequests.AsNoTracking()
            .Where(r => r.Status == CandidateInsightsUnlockRequestStatus.Open
                        && (ids.Contains(r.BranchCompanyId) || ids.Contains(r.WalletCompanyId)))
            .Select(r => new { r.Id, r.BranchCompanyId, BranchName = r.BranchCompany.Name })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var first = rows[0];
        var titleArgs = rows.Count == 1
            ? new[] { first.BranchName }
            : new[] { rows.Count.ToString() };

        return TodoDtoFactory.Create(
            Kind,
            WerkgeverTodoSeverity.Warning,
            rows.Count == 1 ? "WgTodo.InsightsRequests.TitleOne" : "WgTodo.InsightsRequests.TitleMany",
            titleArgs,
            "WgTodo.InsightsRequests.Meta",
            null,
            WerkgeverTodoActionKind.Beoordelen,
            $"/werkgever/kandidaatinzichten?request={first.Id:D}",
            rows.Count,
            rows.Select(r => r.BranchCompanyId).Distinct().ToList());
    }
}
