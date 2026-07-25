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
[Route("api/regions")]
[Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Admin}")]
public class RegionsController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;

    public RegionsController(JobsyDbContext db, ICompanyAuthorizationService companyAuth)
    {
        _db = db;
        _companyAuth = companyAuth;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegionDto>>> List(CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.Regions
            .AsNoTracking()
            .Include(r => r.OrganizationCompany)
            .Include(r => r.Companies).ThenInclude(rc => rc.Company)
            .AsQueryable();

        if (accessible is not null)
        {
            query = query.Where(r => accessible.Contains(r.OrganizationCompanyId));
        }

        var regions = await query.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        return Ok(regions.Select(Map));
    }

    [HttpPost]
    public async Task<ActionResult<RegionDto>> Create(
        [FromBody] CreateRegionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Naam is verplicht." });
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is not null
            && !accessible.Contains(request.OrganizationCompanyId)
            && !_companyAuth.IsAdmin(User))
        {
            return Forbid();
        }

        var org = await _db.Companies.FirstOrDefaultAsync(
            c => c.Id == request.OrganizationCompanyId, cancellationToken);
        if (org is null)
        {
            return NotFound(new { message = "Organisatie niet gevonden." });
        }

        var region = new Region
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            OrganizationCompanyId = request.OrganizationCompanyId
        };
        _db.Regions.Add(region);

        var companyIds = await FilterAccessibleCompanyIdsAsync(request.CompanyIds, cancellationToken);
        foreach (var companyId in companyIds)
        {
            _db.RegionCompanies.Add(new RegionCompany { RegionId = region.Id, CompanyId = companyId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        var loaded = await LoadAsync(region.Id, cancellationToken);
        if (loaded is null)
        {
            return CreatedAtAction(nameof(List), null);
        }

        return CreatedAtAction(nameof(List), Map(loaded));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RegionDto>> Update(
        Guid id,
        [FromBody] UpdateRegionRequest request,
        CancellationToken cancellationToken)
    {
        var region = await _db.Regions
            .Include(r => r.Companies)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (region is null)
        {
            return NotFound();
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is not null && !accessible.Contains(region.OrganizationCompanyId) && !_companyAuth.IsAdmin(User))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Naam is verplicht." });
        }

        region.Name = request.Name.Trim();
        _db.RegionCompanies.RemoveRange(region.Companies);

        var companyIds = await FilterAccessibleCompanyIdsAsync(request.CompanyIds, cancellationToken);
        foreach (var companyId in companyIds)
        {
            _db.RegionCompanies.Add(new RegionCompany { RegionId = region.Id, CompanyId = companyId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        var updated = await LoadAsync(region.Id, cancellationToken);
        return updated is null ? NotFound() : Ok(Map(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var region = await _db.Regions
            .Include(r => r.Companies)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (region is null)
        {
            return NotFound();
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is not null && !accessible.Contains(region.OrganizationCompanyId) && !_companyAuth.IsAdmin(User))
        {
            return Forbid();
        }

        _db.RegionCompanies.RemoveRange(region.Companies);
        _db.Regions.Remove(region);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<List<Guid>> FilterAccessibleCompanyIdsAsync(
        Guid[]? companyIds,
        CancellationToken ct)
    {
        if (companyIds is null || companyIds.Length == 0)
        {
            return [];
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, ct);
        var distinct = companyIds.Distinct().ToList();
        if (accessible is null || _companyAuth.IsAdmin(User))
        {
            return await _db.Companies.Where(c => distinct.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
        }

        return distinct.Where(accessible.Contains).ToList();
    }

    private async Task<Region?> LoadAsync(Guid id, CancellationToken ct)
        => await _db.Regions
            .AsNoTracking()
            .Include(r => r.OrganizationCompany)
            .Include(r => r.Companies).ThenInclude(rc => rc.Company)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    private static RegionDto Map(Region r) => new(
        r.Id,
        r.Name,
        r.OrganizationCompanyId,
        r.OrganizationCompany.Name,
        r.Companies.Select(c => new RegionCompanyItemDto(c.CompanyId, c.Company.Name)).OrderBy(c => c.CompanyName).ToList());
}
