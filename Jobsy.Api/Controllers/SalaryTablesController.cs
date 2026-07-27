using System.Security.Claims;
using System.Text.Json;
using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/salary-tables")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public class SalaryTablesController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;

    public SalaryTablesController(JobsyDbContext db, ICompanyAuthorizationService companyAuth)
    {
        _db = db;
        _companyAuth = companyAuth;
    }

    /// <summary>
    /// Lists organization salary tables. With <paramref name="companyId"/> (vestiging),
    /// returns only tables that vestiging may use (WML + allowed custom tables).
    /// Without companyId, returns org-owned tables for management (EnterpriseManager view).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalaryTableDto>>> List(
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);

        if (companyId is Guid branchId)
        {
            if (accessible is not null && !accessible.Contains(branchId) && !_companyAuth.IsAdmin(User))
            {
                return Forbid();
            }

            var organizationId = await WmlSalaryTableService.ResolveOrganizationIdAsync(_db, branchId, cancellationToken);
            if (organizationId is null)
            {
                return NotFound(new { message = "Bedrijf niet gevonden." });
            }

            var forBranch = await _db.CompanySalaryTables
                .AsNoTracking()
                .Include(t => t.Company)
                .Include(t => t.Rates)
                .Include(t => t.AllowedBranches)
                .ThenInclude(b => b.Company)
                .Where(t => t.IsActive && (
                    (t.CompanyId == organizationId.Value
                     && (t.IsSystemWml || t.AllowedBranches.Any(b => b.CompanyId == branchId)))
                    || (t.CompanyId == branchId
                        && !t.IsSystemWml
                        && t.Name != WmlSalaryTableService.TableName
                        && t.Name != WmlSalaryTableService.LegacyTableName)))
                .OrderByDescending(t => t.IsSystemWml)
                .ThenBy(t => t.Name)
                .ToListAsync(cancellationToken);

            var vacancyCounts = await VacancyCountsAsync(forBranch.Select(t => t.Id), cancellationToken);
            return Ok(forBranch.Select(t => Map(t, vacancyCounts.GetValueOrDefault(t.Id))));
        }

        // Management list: organization-owned tables only (one WML per org).
        List<Guid> organizationIds;
        if (accessible is null)
        {
            organizationIds = await _db.Companies
                .AsNoTracking()
                .Where(c => c.ParentCompanyId == null)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var accessibleList = accessible.ToList();
            var orgs = await _db.Companies
                .AsNoTracking()
                .Where(c => c.ParentCompanyId == null && accessibleList.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            var parentsOfBranches = await _db.Companies
                .AsNoTracking()
                .Where(c => accessibleList.Contains(c.Id) && c.ParentCompanyId != null)
                .Select(c => c.ParentCompanyId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            organizationIds = orgs.Concat(parentsOfBranches).Distinct().ToList();
        }

        var tables = await _db.CompanySalaryTables
            .AsNoTracking()
            .Include(t => t.Company)
            .Include(t => t.Rates)
            .Include(t => t.AllowedBranches)
            .ThenInclude(b => b.Company)
            .Where(t => organizationIds.Contains(t.CompanyId))
            .OrderByDescending(t => t.IsSystemWml)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var counts = await VacancyCountsAsync(tables.Select(t => t.Id), cancellationToken);
        return Ok(tables.Select(t => Map(t, counts.GetValueOrDefault(t.Id))));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalaryTableDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var table = await _db.CompanySalaryTables
            .AsNoTracking()
            .Include(t => t.Company)
            .Include(t => t.Rates)
            .Include(t => t.AllowedBranches)
            .ThenInclude(b => b.Company)
            .Include(t => t.ChangeLogs)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (table is null)
        {
            return NotFound();
        }

        try
        {
            await EnsureCanManageOrganizationTablesAsync(table.CompanyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var count = await _db.Vacancies.CountAsync(v => v.SalaryTableId == id, cancellationToken);
        return Ok(Map(table, count, includeLogs: true));
    }

    [HttpGet("{id:guid}/vacancies")]
    public async Task<ActionResult<IEnumerable<SalaryTableVacancyDto>>> ListVacancies(
        Guid id,
        CancellationToken cancellationToken)
    {
        var table = await _db.CompanySalaryTables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (table is null)
        {
            return NotFound();
        }

        try
        {
            await EnsureCanManageOrganizationTablesAsync(table.CompanyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var vacancies = await _db.Vacancies
            .AsNoTracking()
            .Include(v => v.Company)
            .Where(v => v.SalaryTableId == id)
            .OrderBy(v => v.Title)
            .Select(v => new SalaryTableVacancyDto(
                v.Id,
                v.Title,
                v.Company.Name,
                v.Status.ToString()))
            .ToListAsync(cancellationToken);

        return Ok(vacancies);
    }

    [HttpPost]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<SalaryTableDto>> Upsert(
        [FromBody] UpsertSalaryTableRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Naam is verplicht." });
        }

        var organizationId = await WmlSalaryTableService.ResolveOrganizationIdAsync(
            _db, request.CompanyId, cancellationToken);
        if (organizationId is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
        }

        if (organizationId.Value != request.CompanyId)
        {
            return BadRequest(new { message = "Salaristabellen horen bij het bedrijf (organisatie), niet bij een vestiging." });
        }

        try
        {
            await EnsureCanManageOrganizationTablesAsync(organizationId.Value, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var branchIds = (request.AllowedBranchIds ?? []).Distinct().ToList();
        if (branchIds.Count > 0)
        {
            var validBranches = await _db.Companies
                .AsNoTracking()
                .Where(c => branchIds.Contains(c.Id)
                            && (c.Id == organizationId.Value || c.ParentCompanyId == organizationId.Value))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            if (validBranches.Count != branchIds.Count)
            {
                return BadRequest(new { message = "Een of meer vestigingen horen niet bij dit bedrijf." });
            }
        }

        CompanySalaryTable table;
        string action;
        string logMessage;

        if (request.Id is Guid existingId)
        {
            var existing = await _db.CompanySalaryTables
                .Include(t => t.Rates)
                .Include(t => t.AllowedBranches)
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.Id == existingId, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            try
            {
                await EnsureCanManageOrganizationTablesAsync(existing.CompanyId, cancellationToken);
            }
            catch (Core.Exceptions.ForbiddenCompanyAccessException)
            {
                return Forbid();
            }

            if (existing.CompanyId != organizationId.Value)
            {
                return BadRequest(new { message = "CompanyId komt niet overeen met de salaristabel." });
            }

            if (existing.IsSystemWml)
            {
                return BadRequest(new { message = "Wettelijk Minimumloon kan niet handmatig worden gewijzigd." });
            }

            existing.Name = request.Name.Trim();
            existing.IsActive = request.IsActive;
            _db.CompanySalaryRates.RemoveRange(existing.Rates);
            existing.Rates.Clear();
            _db.CompanySalaryTableAllowedBranches.RemoveRange(existing.AllowedBranches);
            existing.AllowedBranches.Clear();
            table = existing;
            action = "Updated";
            logMessage = $"Salaristabel '{existing.Name}' gewijzigd.";
        }
        else
        {
            var name = request.Name.Trim();
            if (name.Equals(WmlSalaryTableService.TableName, StringComparison.OrdinalIgnoreCase)
                || name.Equals(WmlSalaryTableService.LegacyTableName, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Deze naam is gereserveerd voor het Wettelijk Minimumloon." });
            }

            var company = await _db.Companies.FirstAsync(c => c.Id == organizationId.Value, cancellationToken);
            table = new CompanySalaryTable
            {
                Id = Guid.NewGuid(),
                CompanyId = organizationId.Value,
                Name = name,
                IsActive = request.IsActive,
                IsSystemWml = false,
                Company = company
            };
            _db.CompanySalaryTables.Add(table);
            action = "Created";
            logMessage = $"Salaristabel '{name}' aangemaakt.";
        }

        if (request.Rates is null || request.Rates.Count == 0)
        {
            return BadRequest(new { message = "Vul uurlonen per leeftijd in." });
        }

        foreach (var rate in request.Rates)
        {
            if (rate.HourlyRate <= 0 || rate.AgeYears < 15 || rate.AgeYears > 70)
            {
                return BadRequest(new { message = "Ongeldig salaristarief." });
            }

            table.Rates.Add(new CompanySalaryRate
            {
                Id = rate.Id ?? Guid.NewGuid(),
                SalaryTableId = table.Id,
                AgeYears = rate.AgeYears,
                HourlyRate = rate.HourlyRate,
                Label = string.IsNullOrWhiteSpace(rate.Label)
                    ? (rate.AgeYears >= 21 ? "21+" : rate.AgeYears.ToString())
                    : rate.Label.Trim()
            });
        }

        foreach (var branchId in branchIds)
        {
            table.AllowedBranches.Add(new CompanySalaryTableAllowedBranch
            {
                SalaryTableId = table.Id,
                CompanyId = branchId
            });
        }

        var details = JsonSerializer.Serialize(new
        {
            table.Name,
            table.IsActive,
            Rates = table.Rates.Select(r => new { r.AgeYears, r.HourlyRate, r.Label }),
            AllowedBranchIds = branchIds
        });

        _db.CompanySalaryTableChangeLogs.Add(new CompanySalaryTableChangeLog
        {
            Id = Guid.NewGuid(),
            SalaryTableId = table.Id,
            Action = action,
            ActorUserId = TryGetUserId(),
            ActorEmail = User.FindFirstValue(ClaimTypes.Email),
            Message = logMessage,
            DetailsJson = details,
            CreatedAt = DateTime.UtcNow
        });

        // Also write a platform log for admin visibility.
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = Core.Enums.PlatformLogLevel.Info,
            Category = "SalaryTables",
            Message = logMessage,
            DetailsJson = details,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        var loaded = await _db.CompanySalaryTables
            .AsNoTracking()
            .Include(t => t.Company)
            .Include(t => t.Rates)
            .Include(t => t.AllowedBranches)
            .ThenInclude(b => b.Company)
            .Include(t => t.ChangeLogs)
            .FirstAsync(t => t.Id == table.Id, cancellationToken);
        var count = await _db.Vacancies.CountAsync(v => v.SalaryTableId == table.Id, cancellationToken);
        return Ok(Map(loaded, count, includeLogs: true));
    }

    private async Task EnsureCanManageOrganizationTablesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (await _companyAuth.CanAccessCompanyAsync(User, organizationId, cancellationToken))
        {
            return;
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is null)
        {
            return;
        }

        var canViaBranch = await _db.Companies
            .AsNoTracking()
            .AnyAsync(
                c => accessible.Contains(c.Id)
                     && (c.Id == organizationId || c.ParentCompanyId == organizationId),
                cancellationToken);
        if (!canViaBranch)
        {
            throw new Core.Exceptions.ForbiddenCompanyAccessException(organizationId);
        }
    }

    private Guid? TryGetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private async Task<Dictionary<Guid, int>> VacancyCountsAsync(
        IEnumerable<Guid> tableIds,
        CancellationToken cancellationToken)
    {
        var ids = tableIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _db.Vacancies
            .AsNoTracking()
            .Where(v => v.SalaryTableId != null && ids.Contains(v.SalaryTableId.Value))
            .GroupBy(v => v.SalaryTableId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
    }

    private static SalaryTableDto Map(CompanySalaryTable t, int vacancyCount, bool includeLogs = false) => new(
        t.Id,
        t.CompanyId,
        t.Company.Name,
        t.IsSystemWml ? WmlSalaryTableService.TableName : t.Name,
        t.IsActive,
        t.IsSystemWml,
        vacancyCount,
        t.AllowedBranches.Select(b => b.CompanyId).ToList(),
        t.AllowedBranches.Select(b => b.Company?.Name ?? b.CompanyId.ToString()).ToList(),
        t.Rates
            .OrderByDescending(r => r.AgeYears)
            .Select(r => new SalaryRateDto(r.Id, r.AgeYears, r.HourlyRate, r.Label))
            .ToList(),
        includeLogs
            ? t.ChangeLogs
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new SalaryTableChangeLogDto(l.Id, l.Action, l.ActorEmail, l.Message, l.CreatedAt))
                .ToList()
            : null);
}
