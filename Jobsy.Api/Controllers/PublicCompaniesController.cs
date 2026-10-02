using Jobsy.Api.Models;
using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Media;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>
/// Anonymous employer/vestiging pages keyed by KVK (+ optional vestigingsnummer).
/// Returns public identity only — city, no contact PII, no GUIDs/coordinates.
/// </summary>
[ApiController]
[Route("api/public/companies")]
[AllowAnonymous]
[EnableRateLimiting("public-read")]
[RequiresFeature(PlatformFeature.Employers)]
public sealed class PublicCompaniesController : ControllerBase
{
    private readonly IPublicCompanyQuery _query;
    private readonly IVacancyDiscoveryIndex _discovery;

    public PublicCompaniesController(IPublicCompanyQuery query, IVacancyDiscoveryIndex discovery)
    {
        _query = query;
        _discovery = discovery;
    }

    [HttpGet("{kvkNumber}")]
    public async Task<ActionResult<PublicCompanyPageDto>> GetByKvk(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var kvk = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (kvk is null)
        {
            return BadRequest(new { code = "not_found" });
        }

        var companies = await _query.GetByKvkAsync(kvk, cancellationToken);
        if (companies.Count == 0 || companies.All(c => c.ParentCompanyId is not null))
        {
            return NotFound(new { code = "not_found" });
        }

        var branches = companies
            .Where(c => c.PublicVacancyCount > 0)
            .Select(c =>
            {
                var vestiging = CompanyPublicPaths.TryParseVestigingsnummer(c.KvkEstablishmentId, kvk);
                return new PublicCompanyBranchDto(
                    c.Name,
                    c.City,
                    vestiging,
                    CompanyPublicPaths.TryBuildPath(kvk, c.KvkEstablishmentId),
                    c.PublicVacancyCount);
            })
            .ToList();

        var primary = companies
            .OrderBy(c => c.ParentCompanyId is null ? 0 : 1)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .First();

        return Ok(new PublicCompanyPageDto(
            kvk,
            Vestigingsnummer: null,
            StripBranchSuffix(primary.Name),
            primary.City,
            primary.LogoUrl,
            branches));
    }

    [HttpGet("{kvkNumber}/{vestigingsnummer}")]
    public async Task<ActionResult<PublicCompanyPageDto>> GetByVestiging(
        string kvkNumber,
        string vestigingsnummer,
        CancellationToken cancellationToken)
    {
        var kvk = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (kvk is null || !CompanyPublicPaths.IsValidVestigingRouteSegment(vestigingsnummer))
        {
            return BadRequest(new { code = "not_found" });
        }

        var company = await _query.GetVestigingAsync(kvk, vestigingsnummer, cancellationToken);
        if (company is null)
        {
            return NotFound(new { code = "not_found" });
        }

        var vestiging = CompanyPublicPaths.TryParseVestigingsnummer(company.KvkEstablishmentId, kvk)
                        ?? vestigingsnummer.Trim();

        return Ok(new PublicCompanyPageDto(
            kvk,
            vestiging,
            company.Name,
            company.City,
            company.LogoUrl,
            Branches: null));
    }

    [HttpGet("{kvkNumber}/vacancies")]
    public async Task<ActionResult<IReadOnlyList<VacancyListItemDto>>> GetVacanciesByKvk(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var kvk = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (kvk is null)
        {
            return BadRequest(new { code = "not_found" });
        }

        var companies = await _query.GetByKvkAsync(kvk, cancellationToken);
        if (companies.Count == 0)
        {
            return NotFound(new { code = "not_found" });
        }

        var ids = companies.Select(c => c.Id).ToHashSet();
        return Ok(await MapVacanciesAsync(kvk, ids, vestiging: null, cancellationToken));
    }

    [HttpGet("{kvkNumber}/{vestigingsnummer}/vacancies")]
    public async Task<ActionResult<IReadOnlyList<VacancyListItemDto>>> GetVacanciesByVestiging(
        string kvkNumber,
        string vestigingsnummer,
        CancellationToken cancellationToken)
    {
        var kvk = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (kvk is null || !CompanyPublicPaths.IsValidVestigingRouteSegment(vestigingsnummer))
        {
            return BadRequest(new { code = "not_found" });
        }

        var company = await _query.GetVestigingAsync(kvk, vestigingsnummer, cancellationToken);
        if (company is null)
        {
            return NotFound(new { code = "not_found" });
        }

        return Ok(await MapVacanciesAsync(
            kvk,
            new HashSet<Guid> { company.Id },
            vestigingsnummer.Trim(),
            cancellationToken));
    }

    private async Task<IReadOnlyList<VacancyListItemDto>> MapVacanciesAsync(
        string kvk,
        HashSet<Guid> companyIds,
        string? vestiging,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var records = await _discovery.GetActiveAsync(cancellationToken);
        var filtered = records
            .Where(r => VacancyVisibilityRules.IsPubliclyVisible(r, today)
                        && companyIds.Contains(r.CompanyId)
                        && string.Equals(r.KvkNumber, kvk, StringComparison.Ordinal)
                        && (vestiging is null
                            || string.Equals(r.Vestigingsnummer, vestiging, StringComparison.Ordinal)))
            .Take(100)
            .Select(MapRecord)
            .ToList();
        return filtered;
    }

    private static VacancyListItemDto MapRecord(VacancyDiscoveryRecord r)
    {
        var workType = r.WorkTypeLabelList.FirstOrDefault();
        return new VacancyListItemDto(
            r.Id,
            r.Title,
            Description: null,
            HourlyWage: null,
            r.StartDate,
            r.EndDate,
            r.Status.ToString(),
            r.CompanyId,
            r.CompanyName,
            r.CompanyAddress,
            VacancyImageUrls.Normalize(r.CompanyLogoUrl),
            VacancyImageUrls.ForCard(r.ImageUrl, r.CompanyLogoUrl, r.Id, workType),
            r.Latitude,
            r.Longitude,
            r.RequiredTransportLabels,
            WageVisible: false,
            WorkTypes: r.WorkTypeLabelList,
            KvkNumber: r.KvkNumber,
            Vestigingsnummer: r.Vestigingsnummer,
            IsHighlighted: VacancyHighlightRules.IsActive(r.IsHighlighted, r.HighlightedUntil, DateTime.UtcNow),
            Kind: r.Kind.ToString(),
            OfferedByLabel: r.OfferedByLabel);
    }

    private static string StripBranchSuffix(string name)
    {
        var parts = name.Split(['—', '-'], 2, StringSplitOptions.TrimEntries);
        return parts[0];
    }
}

public sealed record PublicCompanyBranchDto(
    string Name,
    string? City,
    string? Vestigingsnummer,
    string? Path,
    int VacancyCount);

public sealed record PublicCompanyPageDto(
    string Kvk,
    string? Vestigingsnummer,
    string Name,
    string? City,
    string? LogoUrl,
    IReadOnlyList<PublicCompanyBranchDto>? Branches = null);
