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
    private readonly ICompanyApiKeyService _apiKeys;

    public AdminController(
        JobsyDbContext db,
        IKvkService kvk,
        IVacancyProductService products,
        IUserLookupService users,
        ICompanyApiKeyService apiKeys)
    {
        _db = db;
        _kvk = kvk;
        _products = products;
        _users = users;
        _apiKeys = apiKeys;
    }

    [HttpGet("companies")]
    public async Task<ActionResult<IEnumerable<AdminCompanyDetailDto>>> GetCompanies(CancellationToken cancellationToken)
    {
        var companies = await _db.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.KvkNumber,
                c.Address,
                c.LogoUrl,
                Type = c.Type.ToString(),
                c.ParentCompanyId,
                c.ReferredBySalesManagerUserId,
                SalesManagerName = c.ReferredBySalesManagerUser != null
                    ? c.ReferredBySalesManagerUser.FullName
                    : null
            })
            .ToListAsync(cancellationToken);

        var companyIds = companies.Select(c => c.Id).ToList();
        if (companyIds.Count == 0)
        {
            return Ok(Array.Empty<AdminCompanyDetailDto>());
        }

        // Users counted via primary CompanyId OR membership (same semantics as before).
        var activeUserIds = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var activeUserSet = activeUserIds.ToHashSet();

        var membershipPairs = await _db.UserCompanies.AsNoTracking()
            .Where(m => companyIds.Contains(m.CompanyId))
            .Select(m => new { m.CompanyId, m.UserId })
            .ToListAsync(cancellationToken);
        membershipPairs = membershipPairs
            .Where(m => activeUserSet.Contains(m.UserId))
            .ToList();

        var primaryPairs = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.CompanyId != null && companyIds.Contains(u.CompanyId.Value))
            .Select(u => new { CompanyId = u.CompanyId!.Value, UserId = u.Id })
            .ToListAsync(cancellationToken);
        var userCounts = primaryPairs.Concat(membershipPairs)
            .GroupBy(x => x.CompanyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().Count());

        var vacancyStats = await _db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId))
            .GroupBy(v => v.CompanyId)
            .Select(g => new
            {
                CompanyId = g.Key,
                Active = g.Count(v => v.Status == VacancyStatus.Active),
                Total = g.Count()
            })
            .ToDictionaryAsync(x => x.CompanyId, cancellationToken);

        var companyByVacancy = await _db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId))
            .Select(v => new { v.Id, v.CompanyId })
            .ToListAsync(cancellationToken);
        var vacancyCompanyMap = companyByVacancy.ToDictionary(v => v.Id, v => v.CompanyId);

        var applicationRows = await _db.Applications.AsNoTracking()
            .Select(a => a.VacancyId)
            .ToListAsync(cancellationToken);
        var applicationCounts = applicationRows
            .Where(vacancyCompanyMap.ContainsKey)
            .Select(vacancyId => vacancyCompanyMap[vacancyId])
            .GroupBy(companyId => companyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.CompanyId, x => x.Count);

        var tokenBalances = await _db.TokenTransactions.AsNoTracking()
            .Where(t => companyIds.Contains(t.CompanyId))
            .GroupBy(t => t.CompanyId)
            .Select(g => new { CompanyId = g.Key, Balance = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Balance, cancellationToken);

        return Ok(companies.Select(c =>
        {
            vacancyStats.TryGetValue(c.Id, out var vac);
            return new AdminCompanyDetailDto(
                c.Id,
                c.Name,
                c.KvkNumber,
                c.Address,
                c.LogoUrl,
                c.Type,
                c.ParentCompanyId,
                userCounts.GetValueOrDefault(c.Id),
                vac?.Active ?? 0,
                vac?.Total ?? 0,
                applicationCounts.GetValueOrDefault(c.Id),
                tokenBalances.GetValueOrDefault(c.Id),
                c.ReferredBySalesManagerUserId,
                c.SalesManagerName);
        }));
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
        await Jobsy.Infrastructure.Services.WmlSalaryTableService.EnsureForCompanyAsync(_db, company.Id, cancellationToken);
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
            .Select(v => new
            {
                v.Id,
                v.Title,
                Status = v.Status.ToString(),
                v.CompanyId,
                CompanyName = v.Company.Name,
                CompanyType = v.Company.Type.ToString(),
                v.IsHighlighted,
                v.ExtensionCount,
                v.StartDate,
                v.EndDate,
                CreatedVia = v.CreatedVia.ToString()
            })
            .ToListAsync(cancellationToken);

        var ids = vacancies.Select(v => v.Id).ToList();
        var impressions = await _db.VacancySearchImpressions.AsNoTracking()
            .Where(i => ids.Contains(i.VacancyId))
            .GroupBy(i => i.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var clicks = await _db.VacancyClicks.AsNoTracking()
            .Where(c => ids.Contains(c.VacancyId))
            .GroupBy(c => c.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var shares = await _db.VacancyShares.AsNoTracking()
            .Where(s => ids.Contains(s.VacancyId))
            .GroupBy(s => s.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var applications = await _db.Applications.AsNoTracking()
            .Where(a => ids.Contains(a.VacancyId))
            .GroupBy(a => a.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var likes = await _db.VacancyLikes.AsNoTracking()
            .Where(l => ids.Contains(l.VacancyId))
            .GroupBy(l => l.VacancyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return Ok(vacancies.Select(v => new AdminVacancyDetailDto(
            v.Id,
            v.Title,
            v.Status,
            v.CompanyId,
            v.CompanyName,
            v.CompanyType,
            v.IsHighlighted,
            v.ExtensionCount,
            v.StartDate,
            v.EndDate,
            impressions.GetValueOrDefault(v.Id),
            clicks.GetValueOrDefault(v.Id),
            shares.GetValueOrDefault(v.Id),
            applications.GetValueOrDefault(v.Id),
            likes.GetValueOrDefault(v.Id),
            v.ExtensionCount > 0,
            v.CreatedVia)));
    }

    [HttpGet("api-keys")]
    public async Task<ActionResult<IEnumerable<AdminApiKeyView>>> GetApiKeys(CancellationToken cancellationToken)
    {
        var items = await _apiKeys.ListAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("api-keys/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateApiKey(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _apiKeys.DeactivateAsync(id, cancellationToken);
        if (!ok)
        {
            return NotFound(new { message = "API-key niet gevonden." });
        }

        return Ok(new { message = "API-key gedeactiveerd." });
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
                VacancyHighlightRules.IsActive(v.IsHighlighted, v.HighlightedUntil, DateTime.UtcNow),
                v.HighlightedUntil,
                v.ExtensionCount,
                v.VideoUrl,
                v.SalaryTableId,
                null,
                null,
                WorkTypeLabels.ResolveLabels(v.WorkTypes, v.WorkTypeLabels),
                0,
                0,
                0,
                v.RequiredDrivingLicense,
                v.RequiredEducation,
                v.MinimumEmployers,
                v.FulfilledByApplicationId,
                v.CreatedVia.ToString()),
            result.PendingApproval,
            result.ErrorMessage,
            result.PushBomRecipientCount);
    }
}
