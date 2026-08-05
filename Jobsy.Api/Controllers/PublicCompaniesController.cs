using Jobsy.Api.Models;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

/// <summary>
/// Anonymous employer/vestiging pages keyed by KVK (+ optional vestigingsnummer).
/// Returns public identity only — no contact PII.
/// </summary>
[ApiController]
[Route("api/public/companies")]
[AllowAnonymous]
[EnableRateLimiting("public-write")]
public sealed class PublicCompaniesController : ControllerBase
{
    private readonly JobsyDbContext _db;

    public PublicCompaniesController(JobsyDbContext db)
    {
        _db = db;
    }

    /// <summary>All registered vestigingen under a KVK (ondernemer-pagina).</summary>
    [HttpGet("{kvkNumber}")]
    public async Task<ActionResult<PublicCompanyPageDto>> GetByKvk(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var kvk = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (kvk is null)
        {
            return BadRequest(new { message = "Ongeldig KVK-nummer." });
        }

        var companies = await _db.Companies.AsNoTracking()
            .Where(c => c.KvkNumber == kvk)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        if (companies.Count == 0)
        {
            return NotFound(new { message = "Ondernemer niet gevonden." });
        }

        var branches = companies
            .Select(c =>
            {
                var vestiging = CompanyPublicPaths.TryParseVestigingsnummer(c.KvkEstablishmentId, kvk);
                return new PublicCompanyBranchDto(
                    c.Id,
                    c.Name,
                    c.Address,
                    c.LogoUrl,
                    c.Location?.Latitude ?? 0,
                    c.Location?.Longitude ?? 0,
                    vestiging,
                    CompanyPublicPaths.TryBuildPath(kvk, c.KvkEstablishmentId));
            })
            .ToList();

        var primary = companies
            .OrderBy(c => c.ParentCompanyId is null ? 0 : 1)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .First();

        var displayName = StripBranchSuffix(primary.Name);
        return Ok(new PublicCompanyPageDto(
            kvk,
            Vestigingsnummer: null,
            displayName,
            primary.Address,
            primary.LogoUrl,
            primary.Location?.Latitude ?? 0,
            primary.Location?.Longitude ?? 0,
            companies.Select(c => c.Id).ToList(),
            branches));
    }

    /// <summary>Single vestiging page.</summary>
    [HttpGet("{kvkNumber}/{vestigingsnummer}")]
    public async Task<ActionResult<PublicCompanyPageDto>> GetByVestiging(
        string kvkNumber,
        string vestigingsnummer,
        CancellationToken cancellationToken)
    {
        var kvk = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (kvk is null)
        {
            return BadRequest(new { message = "Ongeldig KVK-nummer." });
        }

        if (!CompanyPublicPaths.IsValidVestigingRouteSegment(vestigingsnummer))
        {
            return BadRequest(new { message = "Ongeldig vestigingsnummer." });
        }

        var establishmentId = CompanyPublicPaths.BuildEstablishmentId(kvk, vestigingsnummer.Trim());
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.KvkEstablishmentId == establishmentId
                     || (c.KvkNumber == kvk && c.KvkEstablishmentId == vestigingsnummer.Trim()),
                cancellationToken);

        if (company is null)
        {
            // Soft match: same KVK and establishment suffix ignoring formatting.
            var all = await _db.Companies.AsNoTracking()
                .Where(c => c.KvkNumber == kvk)
                .ToListAsync(cancellationToken);
            company = all.FirstOrDefault(c =>
                string.Equals(
                    CompanyPublicPaths.TryParseVestigingsnummer(c.KvkEstablishmentId, kvk),
                    vestigingsnummer.Trim(),
                    StringComparison.Ordinal));
        }

        if (company is null)
        {
            return NotFound(new { message = "Vestiging niet gevonden." });
        }

        var vestiging = CompanyPublicPaths.TryParseVestigingsnummer(company.KvkEstablishmentId, kvk)
                        ?? vestigingsnummer.Trim();

        return Ok(new PublicCompanyPageDto(
            kvk,
            vestiging,
            company.Name,
            company.Address,
            company.LogoUrl,
            company.Location?.Latitude ?? 0,
            company.Location?.Longitude ?? 0,
            [company.Id],
            Branches: null));
    }

    private static string StripBranchSuffix(string name)
    {
        var parts = name.Split(['—', '-'], 2, StringSplitOptions.TrimEntries);
        return parts[0];
    }
}

public sealed record PublicCompanyBranchDto(
    Guid CompanyId,
    string Name,
    string Address,
    string? LogoUrl,
    double Latitude,
    double Longitude,
    string? Vestigingsnummer,
    string? PublicPath);

public sealed record PublicCompanyPageDto(
    string KvkNumber,
    string? Vestigingsnummer,
    string Name,
    string Address,
    string? LogoUrl,
    double Latitude,
    double Longitude,
    IReadOnlyList<Guid> CompanyIds,
    IReadOnlyList<PublicCompanyBranchDto>? Branches = null);
