using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
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
                c.Vacancies.Count(v => v.Status == VacancyStatus.Active),
                c.ParentCompanyId,
                c.TokensManagedByEnterprise,
                c.CsvBatchImportEnabled,
                c.DirectContactEnabled,
                c.ContactPreferMail,
                c.ContactPreferPhone,
                c.ContactPreferWhatsApp,
                c.ContactEmail,
                c.ContactPhone,
                c.ContactWhatsApp,
                c.KvkEstablishmentId))
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

        return CreatedAtAction(nameof(GetMine), MapSummary(company, 0, 0));
    }

    /// <summary>
    /// Registers or links a KVK establishment as an intermediary end-client.
    /// </summary>
    [HttpPost("intermediary-clients/from-kvk")]
    [Authorize(Roles = $"{JobsyRoles.Intermediary},{JobsyRoles.Admin}")]
    public async Task<ActionResult<CompanySummaryDto>> RegisterIntermediaryClientFromKvk(
        [FromBody] RegisterEstablishmentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.KvkNumber) || string.IsNullOrWhiteSpace(request.KvkEstablishmentId))
        {
            return BadRequest(new { message = "KVK-nummer en vestigings-id zijn verplicht." });
        }

        var kvkNumber = request.KvkNumber.Trim();
        var kvkEstablishmentId = request.KvkEstablishmentId.Trim();
        var establishments = await _kvk.GetEstablishmentsAsync(kvkNumber, cancellationToken);
        var match = establishments.FirstOrDefault(e =>
            e.KvkEstablishmentId.Equals(kvkEstablishmentId, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return NotFound(new { message = "Vestiging niet gevonden in KVK-stub." });
        }

        var existing = await _db.Companies
            .FirstOrDefaultAsync(c => c.KvkEstablishmentId == match.KvkEstablishmentId, cancellationToken);
        if (existing is not null)
        {
            await EnsureActorMembershipAsync(existing.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(await ToSummaryAsync(existing, cancellationToken));
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
            ParentCompanyId = null
        };

        _db.Companies.Add(company);
        await Jobsy.Infrastructure.Services.WmlSalaryTableService.EnsureForCompanyAsync(_db, company.Id, cancellationToken);
        await EnsureActorMembershipAsync(company.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMine), MapSummary(company, 0, 0));
    }

    /// <summary>
    /// Toggle whether the bedrijfsmanager manages tokens for a vestiging (checkbox opt-in).
    /// </summary>
    [HttpPut("{companyId:guid}/token-management")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<CompanySummaryDto>> UpdateTokenManagement(
        Guid companyId,
        [FromBody] UpdateTokenManagementRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
        }

        if (company.ParentCompanyId is null)
        {
            return BadRequest(new
            {
                message = "Tokenbeheer-optie geldt alleen voor vestigingen, niet voor de organisatiopot."
            });
        }

        company.TokensManagedByEnterprise = request.TokensManagedByEnterprise;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(await ToSummaryAsync(company, cancellationToken));
    }

    /// <summary>
    /// Enable/disable CSV Batch Import for an organisation (parent company).
    /// </summary>
    [HttpPut("{companyId:guid}/csv-batch-import")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<CompanySummaryDto>> UpdateCsvBatchImport(
        Guid companyId,
        [FromBody] UpdateCsvBatchImportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
        }

        if (company.ParentCompanyId is not null)
        {
            return BadRequest(new
            {
                message = "CSV Batch Import schakel je in op organisatieniveau (niet op een vestiging)."
            });
        }

        company.CsvBatchImportEnabled = request.CsvBatchImportEnabled;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(await ToSummaryAsync(company, cancellationToken));
    }

    /// <summary>
    /// Company-level "Voorkeur voor contact" (defaults for vacancies unless overridden).
    /// </summary>
    [HttpPut("{companyId:guid}/contact-preference")]
    [Authorize(Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin},{JobsyRoles.Intermediary}")]
    public async Task<ActionResult<CompanySummaryDto>> UpdateContactPreference(
        Guid companyId,
        [FromBody] UpdateContactPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound(new { message = "Bedrijf niet gevonden." });
        }

        var validationError = EmployerContactPreferenceRules.Validate(
            request.DirectContactEnabled,
            request.ContactPreferMail,
            request.ContactPreferPhone,
            request.ContactPreferWhatsApp,
            request.ContactEmail,
            request.ContactPhone,
            request.ContactWhatsApp);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        company.DirectContactEnabled = request.DirectContactEnabled;
        company.ContactPreferMail = request.DirectContactEnabled && request.ContactPreferMail;
        company.ContactPreferPhone = request.DirectContactEnabled && request.ContactPreferPhone;
        company.ContactPreferWhatsApp = request.DirectContactEnabled && request.ContactPreferWhatsApp;
        company.ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim();
        company.ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();
        company.ContactWhatsApp = string.IsNullOrWhiteSpace(request.ContactWhatsApp) ? null : request.ContactWhatsApp.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(await ToSummaryAsync(company, cancellationToken));
    }

    private async Task<CompanySummaryDto> ToSummaryAsync(Company company, CancellationToken cancellationToken)
    {
        var balance = await _db.TokenTransactions.AsNoTracking()
            .Where(t => t.CompanyId == company.Id)
            .SumAsync(t => t.Amount, cancellationToken);
        var activeVacancies = await _db.Vacancies.AsNoTracking()
            .CountAsync(v => v.CompanyId == company.Id && v.Status == VacancyStatus.Active, cancellationToken);

        return MapSummary(company, balance, activeVacancies);
    }

    private static CompanySummaryDto MapSummary(Company company, decimal balance, int activeVacancies) =>
        new(
            company.Id,
            company.Name,
            company.Address,
            company.KvkNumber,
            balance,
            activeVacancies,
            company.ParentCompanyId,
            company.TokensManagedByEnterprise,
            company.CsvBatchImportEnabled,
            company.DirectContactEnabled,
            company.ContactPreferMail,
            company.ContactPreferPhone,
            company.ContactPreferWhatsApp,
            company.ContactEmail,
            company.ContactPhone,
            company.ContactWhatsApp,
            company.KvkEstablishmentId);

    private async Task EnsureActorMembershipAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return;
        }

        var alreadyMember = await _db.UserCompanies.AnyAsync(
            uc => uc.UserId == actor.Id && uc.CompanyId == companyId, cancellationToken);
        if (!alreadyMember)
        {
            _db.UserCompanies.Add(new UserCompany { UserId = actor.Id, CompanyId = companyId });
        }
    }
}
