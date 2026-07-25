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
[Route("api/admin")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public class AdminController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IKvkService _kvk;
    private readonly IVacancyProductService _products;
    private readonly IUserLookupService _users;

    public AdminController(
        JobsyDbContext db,
        IKvkService kvk,
        IVacancyProductService products,
        IUserLookupService users)
    {
        _db = db;
        _kvk = kvk;
        _products = products;
        _users = users;
    }

    [HttpGet("companies")]
    public async Task<ActionResult<IEnumerable<AdminCompanyDetailDto>>> GetCompanies(CancellationToken cancellationToken)
    {
        var companies = await _db.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new AdminCompanyDetailDto(
                c.Id,
                c.Name,
                c.KvkNumber,
                c.Address,
                c.LogoUrl,
                c.Type.ToString(),
                c.ParentCompanyId,
                _db.Users.Count(u => u.IsActive && (
                    u.CompanyId == c.Id
                    || u.CompanyMemberships.Any(m => m.CompanyId == c.Id))),
                c.Vacancies.Count(v => v.Status == VacancyStatus.Active),
                c.Vacancies.Count(),
                c.Vacancies.Sum(v => v.Applications.Count),
                c.TokenTransactions.Sum(t => (decimal?)t.Amount) ?? 0m))
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }

    [HttpPost("companies/from-kvk")]
    public async Task<ActionResult<AdminCompanyDetailDto>> RegisterCompanyFromKvk(
        [FromBody] RegisterAdminCompanyRequest request,
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

        if (request.ParentCompanyId is Guid parentId)
        {
            var parent = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
            if (parent is null)
            {
                return NotFound(new { message = "Parent-bedrijf niet gevonden." });
            }

            if (!string.Equals(parent.KvkNumber, match.KvkNumber, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "KVK-nummer moet overeenkomen met het parent-bedrijf." });
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
            Type = request.Type,
            ParentCompanyId = request.ParentCompanyId
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCompanies), new AdminCompanyDetailDto(
            company.Id,
            company.Name,
            company.KvkNumber,
            company.Address,
            company.LogoUrl,
            company.Type.ToString(),
            company.ParentCompanyId,
            0,
            0,
            0,
            0,
            0));
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserDetailDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new AdminUserDetailDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.ToString(),
                u.CompanyId,
                u.Company != null ? u.Company.Name : null,
                u.Company != null ? u.Company.Type.ToString() : null,
                u.IsEarlyAdapter,
                u.IsActive,
                u.CompanyMemberships.Select(m => m.CompanyId).ToList()))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("vacancies")]
    public async Task<ActionResult<IEnumerable<AdminVacancyDetailDto>>> GetVacancies(CancellationToken cancellationToken)
    {
        var vacancies = await _db.Vacancies
            .AsNoTracking()
            .OrderByDescending(v => v.StartDate)
            .Select(v => new AdminVacancyDetailDto(
                v.Id,
                v.Title,
                v.Status.ToString(),
                v.CompanyId,
                v.Company.Name,
                v.Company.Type.ToString(),
                v.IsHighlighted,
                v.ExtensionCount,
                v.StartDate,
                v.EndDate,
                v.Clicks.Count,
                v.Shares.Count,
                v.Applications.Count,
                v.Likes.Count,
                v.ExtensionCount > 0))
            .ToListAsync(cancellationToken);

        return Ok(vacancies);
    }

    [HttpPost("vacancies/{id:guid}/extend")]
    public async Task<ActionResult<VacancyProductActionResultDto>> ExtendVacancy(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await _products.ExtendAsync(vacancy, actor?.Id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProductResult(result));
    }

    [HttpPost("vacancies/{id:guid}/inactive")]
    public async Task<ActionResult<VacancyProductActionResultDto>> DeactivateVacancy(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var result = await _products.DeactivateAsync(vacancy, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(ToProductResult(result));
    }

    private static VacancyProductActionResultDto ToProductResult(VacancyProductOutcome result)
    {
        var v = result.Vacancy;
        return new VacancyProductActionResultDto(
            new VacancyListItemDto(
                v.Id,
                v.Title,
                v.Description,
                v.HourlyWage,
                v.StartDate,
                v.EndDate,
                v.Status.ToString(),
                v.CompanyId,
                v.Company?.Name ?? "",
                v.Company?.Address ?? "",
                v.Company?.LogoUrl,
                v.ImageUrl,
                v.Location.Latitude,
                v.Location.Longitude,
                TransportLabels.Expand(v.RequiredTransport),
                true,
                null,
                null,
                v.IsHighlighted,
                v.ExtensionCount,
                v.VideoUrl,
                v.SalaryTableId,
                null,
                null,
                WorkTypeLabels.Expand(v.WorkTypes)),
            result.PendingApproval,
            result.ErrorMessage,
            result.PushBomRecipientCount);
    }
}
