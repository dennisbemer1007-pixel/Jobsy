using Jobsy.Api.Models;
using Jobsy.Api.Privacy;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/search")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminSearchController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;
    private readonly IPersonalDataAccessLogger _accessLog;

    public AdminSearchController(
        JobsyDbContext db,
        IUserLookupService users,
        IPersonalDataAccessLogger accessLog)
    {
        _db = db;
        _users = users;
        _accessLog = accessLog;
    }

    [HttpGet]
    public async Task<ActionResult<AdminSearchResultDto>> Search(
        [FromQuery] string? q,
        CancellationToken cancellationToken = default)
    {
        var term = (q ?? "").Trim();
        if (term.Length is < 2 or > 100)
        {
            return BadRequest(new { error = "q must be 2–100 characters." });
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var users = await SearchUsersAsync(term, cancellationToken);
        var orgs = await SearchOrgsAsync(term, cancellationToken);
        var vacancies = await SearchVacanciesAsync(term, cancellationToken);
        var invoices = await SearchInvoicesAsync(term, cancellationToken);

        await this.LogPersonalDataAccessAsync(
            _accessLog,
            actor.Id,
            PersonalDataAccessLogExtensions.ResolveActorRole(User),
            "admin.search",
            "list",
            cancellationToken: cancellationToken);

        return Ok(new AdminSearchResultDto(users, orgs, vacancies, invoices));
    }

    private async Task<IReadOnlyList<AdminSearchHitDto>> SearchUsersAsync(
        string term,
        CancellationToken ct)
    {
        Guid? asId = Guid.TryParse(term, out var id) ? id : null;
        var rows = await _db.Users.AsNoTracking()
            .Where(u =>
                (asId != null && u.Id == asId)
                || u.Email.Contains(term)
                || u.FullName.Contains(term))
            .OrderBy(u => u.Email)
            .Take(5)
            .Select(u => new { u.Id, u.Email, u.FullName, Role = u.Role.ToString() })
            .ToListAsync(ct);

        return rows.Select(u => new AdminSearchHitDto(
            u.Id.ToString("D"),
            PersonalDataMasker.MaskName(u.FullName),
            $"{PersonalDataMasker.MaskEmail(u.Email)} · {u.Role}",
            $"/admin/gebruikers?open={u.Id:D}")).ToList();
    }

    private async Task<IReadOnlyList<AdminSearchHitDto>> SearchOrgsAsync(
        string term,
        CancellationToken ct)
    {
        var rows = await _db.Companies.AsNoTracking()
            .Where(c => c.Name.Contains(term) || c.KvkNumber.Contains(term))
            .OrderBy(c => c.Name)
            .Take(5)
            .Select(c => new { c.Id, c.Name, c.KvkNumber })
            .ToListAsync(ct);

        return rows.Select(c => new AdminSearchHitDto(
            c.Id.ToString("D"),
            c.Name,
            string.IsNullOrWhiteSpace(c.KvkNumber) ? null : $"KvK {c.KvkNumber}",
            $"/admin/organisaties?open={c.Id:D}")).ToList();
    }

    private async Task<IReadOnlyList<AdminSearchHitDto>> SearchVacanciesAsync(
        string term,
        CancellationToken ct)
    {
        Guid? asId = Guid.TryParse(term, out var id) ? id : null;
        var rows = await _db.Vacancies.AsNoTracking()
            .Where(v =>
                (asId != null && v.Id == asId)
                || v.Title.Contains(term))
            .OrderByDescending(v => v.CreatedAtUtc)
            .Take(5)
            .Select(v => new { v.Id, v.Title, CompanyName = v.Company != null ? v.Company.Name : null })
            .ToListAsync(ct);

        return rows.Select(v => new AdminSearchHitDto(
            v.Id.ToString("D"),
            v.Title,
            v.CompanyName,
            $"/admin/vacatures?open={v.Id:D}")).ToList();
    }

    private async Task<IReadOnlyList<AdminSearchHitDto>> SearchInvoicesAsync(
        string term,
        CancellationToken ct)
    {
        var purchases = await _db.TokenPurchaseInvoices.AsNoTracking()
            .Where(i => i.InvoiceNumber.Contains(term))
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new { i.Id, i.InvoiceNumber, i.CompanyName })
            .ToListAsync(ct);

        var selfBills = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.InvoiceNumber.Contains(term))
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new { i.Id, i.InvoiceNumber, Name = i.SalesManagerCompanyName })
            .ToListAsync(ct);

        var hits = purchases
            .Select(i => new AdminSearchHitDto(
                i.Id.ToString("D"),
                i.InvoiceNumber,
                i.CompanyName,
                $"/admin/financien?q={Uri.EscapeDataString(i.InvoiceNumber)}"))
            .Concat(selfBills.Select(i => new AdminSearchHitDto(
                i.Id.ToString("D"),
                i.InvoiceNumber,
                i.Name,
                $"/admin/financien/uitbetalingen?q={Uri.EscapeDataString(i.InvoiceNumber)}")))
            .Take(5)
            .ToList();

        return hits;
    }
}
