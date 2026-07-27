using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
public class CompaniesController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IKvkService _kvk;
    private readonly IUserLookupService _users;

    public CompaniesController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IKvkService kvk,
        IUserLookupService users)
    {
        _db = db;
        _companyAuth = companyAuth;
        _kvk = kvk;
        _users = users;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<CompanySummaryDto>>> GetMine(CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.Companies.AsNoTracking().AsQueryable();

        if (accessible is not null)
        {
            query = query.Where(c => accessible.Contains(c.Id));
        }

        var companies = await query
            .OrderBy(c => c.Name)
            .Select(c => new CompanySummaryDto(
                c.Id,
                c.Name,
                c.Address,
                c.KvkNumber,
                c.TokenTransactions.Sum(t => t.Amount),
                c.Vacancies.Count(v => v.Status == VacancyStatus.Active)))
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }

    /// <summary>
    /// Registers a KVK establishment as a vestiging (child company) within employer scope.
    /// </summary>
    [HttpPost("from-kvk")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<CompanySummaryDto>> RegisterFromKvk(
        [FromBody] RegisterEstablishmentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.KvkNumber) || string.IsNullOrWhiteSpace(request.KvkEstablishmentId))
        {
            return BadRequest(new { message = "KVK-nummer en vestigings-id zijn verplicht." });
        }

        var establishments = await _kvk.GetEstablishmentsAsync(request.KvkNumber.Trim(), cancellationToken);
        var match = establishments.FirstOrDefault(e =>
            e.KvkEstablishmentId.Equals(request.KvkEstablishmentId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return NotFound(new { message = "Vestiging niet gevonden in KVK-stub." });
        }

        if (match.IsInUse || await _db.Companies.AnyAsync(
                c => c.KvkEstablishmentId == match.KvkEstablishmentId, cancellationToken))
        {
            return BadRequest(new { message = "Deze vestiging is al geregistreerd." });
        }

        Guid? parentId = request.ParentCompanyId;
        if (parentId is null)
        {
            var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
            parentId = actor?.CompanyId;
        }

        if (parentId is not null)
        {
            var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
            if (accessible is not null && !accessible.Contains(parentId.Value) && !_companyAuth.IsAdmin(User))
            {
                return Forbid();
            }

            var parent = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == parentId.Value, cancellationToken);
            if (parent is null)
            {
                return NotFound(new { message = "Parent-bedrijf niet gevonden." });
            }

            if (!string.Equals(parent.KvkNumber, match.KvkNumber, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "KVK-nummer van vestiging moet overeenkomen met het parent-bedrijf." });
            }
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = match.Name,
            KvkNumber = match.KvkNumber,
            KvkEstablishmentId = match.KvkEstablishmentId,
            Address = match.Address,
            Location = new GeoPoint(match.Latitude, match.Longitude),
            Type = CompanyType.Employer,
            ParentCompanyId = parentId
        };

        _db.Companies.Add(company);
        await Jobsy.Infrastructure.Services.WmlSalaryTableService.EnsureForCompanyAsync(_db, company.Id, cancellationToken);

        // Grant membership to the inviting enterprise manager.
        var manager = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (manager is not null)
        {
            var alreadyMember = await _db.UserCompanies.AnyAsync(
                uc => uc.UserId == manager.Id && uc.CompanyId == company.Id, cancellationToken);
            if (!alreadyMember)
            {
                _db.UserCompanies.Add(new UserCompany { UserId = manager.Id, CompanyId = company.Id });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMine), new CompanySummaryDto(
            company.Id,
            company.Name,
            company.Address,
            company.KvkNumber,
            0,
            0));
    }
}
