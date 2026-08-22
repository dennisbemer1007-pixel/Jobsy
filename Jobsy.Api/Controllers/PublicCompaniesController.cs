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
[EnableRateLimiting("public-read")]
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

        var companies = await QueryPublicRows(_db, kvk)
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
                    c.Latitude,
                    c.Longitude,
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
            primary.Latitude,
            primary.Longitude,
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
        var company = await QueryPublicRows(_db, kvk)
            .FirstOrDefaultAsync(
                c => c.KvkEstablishmentId == establishmentId
                     || c.KvkEstablishmentId == vestigingsnummer.Trim(),
                cancellationToken);

        if (company is null)
        {
            // Soft match: same KVK and establishment suffix ignoring formatting.
            var all = await QueryPublicRows(_db, kvk).ToListAsync(cancellationToken);
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
            company.Latitude,
            company.Longitude,
            [company.Id],
            Branches: null));
    }

    private static IQueryable<CompanyPublicRow> QueryPublicRows(JobsyDbContext db, string kvk)
        => db.Companies.AsNoTracking()
            .Where(c => c.KvkNumber == kvk)
            .Select(c => new CompanyPublicRow(
                c.Id,
                c.Name,
                c.Address,
                c.LogoUrl,
                c.KvkEstablishmentId,
                c.ParentCompanyId,
                c.Location == null ? 0 : c.Location.Latitude,
                c.Location == null ? 0 : c.Location.Longitude));

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

internal sealed record CompanyPublicRow(
    Guid Id,
    string Name,
    string Address,
    string? LogoUrl,
    string? KvkEstablishmentId,
    Guid? ParentCompanyId,
    double Latitude,
    double Longitude);
