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

        // Project only scalar fields — never materialize Company.Location (PostGIS geometry),
        // which has caused 500s when Include()'ing full Company graphs.
        var query = _db.Regions.AsNoTracking().AsQueryable();
        if (accessible is not null)
        {
            query = query.Where(r => accessible.Contains(r.OrganizationCompanyId));
        }

        var rows = await query
            .OrderBy(r => r.Name)
            .Select(r => new RegionListRow(
                r.Id,
                r.Name,
                r.OrganizationCompanyId,
                r.OrganizationCompany != null ? r.OrganizationCompany.Name : "Onbekende organisatie",
                r.Companies
                    .Where(c => c.Company != null)
                    .Select(c => new RegionCompanyRow(c.CompanyId, c.Company!.Name))
                    .ToList()))
            .ToListAsync(cancellationToken);

        var result = rows
            .Select(r => new RegionDto(
                r.Id,
                r.Name,
                r.OrganizationCompanyId,
                string.IsNullOrWhiteSpace(r.OrganizationCompanyName)
                    ? "Onbekende organisatie"
                    : r.OrganizationCompanyName,
                r.Companies
                    .Select(c => new RegionCompanyItemDto(c.CompanyId, c.CompanyName))
                    .OrderBy(c => c.CompanyName)
                    .ToList()))
            .ToList();

        return Ok(result);
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

        var orgExists = await _db.Companies.AsNoTracking()
            .AnyAsync(c => c.Id == request.OrganizationCompanyId, cancellationToken);
        if (!orgExists)
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
        var loaded = await LoadDtoAsync(region.Id, cancellationToken);
        if (loaded is null)
        {
            return CreatedAtAction(nameof(List), null);
        }

        return CreatedAtAction(nameof(List), loaded);
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
        var updated = await LoadDtoAsync(region.Id, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
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
            return await _db.Companies.AsNoTracking()
                .Where(c => distinct.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(ct);
        }

        return distinct.Where(accessible.Contains).ToList();
    }

    private async Task<RegionDto?> LoadDtoAsync(Guid id, CancellationToken ct)
    {
        var row = await _db.Regions.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RegionListRow(
                r.Id,
                r.Name,
                r.OrganizationCompanyId,
                r.OrganizationCompany != null ? r.OrganizationCompany.Name : "Onbekende organisatie",
                r.Companies
                    .Where(c => c.Company != null)
                    .Select(c => new RegionCompanyRow(c.CompanyId, c.Company!.Name))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        if (row is null)
        {
            return null;
        }

        return new RegionDto(
            row.Id,
            row.Name,
            row.OrganizationCompanyId,
            string.IsNullOrWhiteSpace(row.OrganizationCompanyName)
                ? "Onbekende organisatie"
                : row.OrganizationCompanyName,
            row.Companies
                .Select(c => new RegionCompanyItemDto(c.CompanyId, c.CompanyName))
                .OrderBy(c => c.CompanyName)
                .ToList());
    }

    private sealed record RegionListRow(
        Guid Id,
        string Name,
        Guid OrganizationCompanyId,
        string OrganizationCompanyName,
        List<RegionCompanyRow> Companies);

    private sealed record RegionCompanyRow(Guid CompanyId, string CompanyName);
}
