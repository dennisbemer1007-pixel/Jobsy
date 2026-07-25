using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalaryTableDto>>> List(
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.CompanySalaryTables
            .AsNoTracking()
            .Include(t => t.Company)
            .Include(t => t.Rates)
            .AsQueryable();

        if (accessible is not null)
        {
            query = query.Where(t => accessible.Contains(t.CompanyId));
        }

        if (companyId is not null)
        {
            if (accessible is not null && !accessible.Contains(companyId.Value) && !_companyAuth.IsAdmin(User))
            {
                return Forbid();
            }

            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var tables = await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
        return Ok(tables.Select(Map));
    }

    [HttpPost]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    [RequireCompanyAccess]
    public async Task<ActionResult<SalaryTableDto>> Upsert(
        [FromBody] UpsertSalaryTableRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Naam is verplicht." });
        }

        CompanySalaryTable table;
        if (request.Id is Guid existingId)
        {
            var existing = await _db.CompanySalaryTables
                .Include(t => t.Rates)
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.Id == existingId, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            try
            {
                await _companyAuth.EnsureCanAccessCompanyAsync(User, existing.CompanyId, cancellationToken);
            }
            catch (Core.Exceptions.ForbiddenCompanyAccessException)
            {
                return Forbid();
            }

            if (existing.CompanyId != request.CompanyId)
            {
                return BadRequest(new { message = "CompanyId komt niet overeen met de salaristabel." });
            }

            existing.Name = request.Name.Trim();
            existing.IsActive = request.IsActive;
            _db.CompanySalaryRates.RemoveRange(existing.Rates);
            table = existing;
        }
        else
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
            if (company is null)
            {
                return NotFound(new { message = "Bedrijf niet gevonden." });
            }

            table = new CompanySalaryTable
            {
                Id = Guid.NewGuid(),
                CompanyId = request.CompanyId,
                Name = request.Name.Trim(),
                IsActive = request.IsActive,
                Company = company
            };
            _db.CompanySalaryTables.Add(table);
        }

        if (request.Rates is not null)
        {
            foreach (var rate in request.Rates)
            {
                if (rate.HourlyRate <= 0 || rate.AgeYears < 15 || rate.AgeYears > 70)
                {
                    return BadRequest(new { message = "Ongeldig salaristarief." });
                }

                _db.CompanySalaryRates.Add(new CompanySalaryRate
                {
                    Id = rate.Id ?? Guid.NewGuid(),
                    SalaryTableId = table.Id,
                    AgeYears = rate.AgeYears,
                    HourlyRate = rate.HourlyRate,
                    Label = string.IsNullOrWhiteSpace(rate.Label) ? $"{rate.AgeYears}+" : rate.Label
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        var loaded = await _db.CompanySalaryTables
            .AsNoTracking()
            .Include(t => t.Company)
            .Include(t => t.Rates)
            .FirstAsync(t => t.Id == table.Id, cancellationToken);
        return Ok(Map(loaded));
    }

    private static SalaryTableDto Map(CompanySalaryTable t) => new(
        t.Id,
        t.CompanyId,
        t.Company.Name,
        t.Name,
        t.IsActive,
        t.Rates
            .OrderByDescending(r => r.AgeYears)
            .Select(r => new SalaryRateDto(r.Id, r.AgeYears, r.HourlyRate, r.Label))
            .ToList());
}
