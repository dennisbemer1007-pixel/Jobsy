using Jobsy.Api.Models;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
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
    private readonly IRoutingService _routing;

    public PublicCompaniesController(
        JobsyDbContext db,
        IRoutingService routing)
    {
        _db = db;
        _routing = routing;
    }

    /// <summary>
    /// Bedrijven-hub discover: companies with at least one hub-qualifying active vacancy,
    /// filtered/sorted by candidate travel time (same hyper-local logic as banenkaart).
    /// </summary>
    [HttpGet("discover")]
    public async Task<ActionResult<IEnumerable<CompanyHubListItemDto>>> Discover(
        [FromQuery] double? originLat,
        [FromQuery] double? originLng,
        [FromQuery] string transport = TransportLabels.Bike,
        [FromQuery] int maxMinutes = 30,
        [FromQuery] double? radiusKm = null,
        [FromQuery] string[]? workType = null,
        [FromQuery] Guid[]? categoryId = null,
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        maxMinutes = Math.Clamp(maxMinutes, 5, 90);
        var mode = TransportLabels.Parse(transport);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        double? reachKm = null;
        if (originLat is not null && originLng is not null)
        {
            reachKm = TravelReach.MaxCrowFliesKm(mode, maxMinutes, radiusKm);
        }

        var vacancies = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .Where(v => v.Status == VacancyStatus.Active
                        && v.StartDate <= today
                        && v.EndDate >= today)
            .ToListAsync(cancellationToken);

        vacancies = vacancies
            .Where(v => VacancyVisibilityRules.QualifiesForCompanyHub(v, today))
            .Where(v => VacancyCategoryDefaults.MatchesSelectedCategories(
                v.CategoryId,
                v.SuitableFor65Plus,
                categoryId?.Where(id => id != Guid.Empty).ToHashSet()))
            .Where(v => WorkTypeLabels.MatchesFilter(v.WorkTypes, v.WorkTypeLabels, workType))
            .Where(v => string.IsNullOrWhiteSpace(q)
                        || VacancyTextSearch.Matches(v, q)
                        || (v.Company.Name?.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase) == true)
                        || (v.Company.HubAboutText?.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase) == true))
            .ToList();

        if (originLat is double oLat && originLng is double oLng && reachKm is double reach)
        {
            var origin = new GeoPoint(oLat, oLng);
            vacancies = vacancies
                .Where(v => v.Company.Location is not null
                            && GeoDistance.IsWithinKm(origin, v.Company.Location, reach))
                .ToList();
        }

        var byCompany = vacancies
            .GroupBy(v => v.CompanyId)
            .Select(g => new
            {
                Company = g.First().Company,
                VacancyCount = g.Count(),
                SampleImage = g.OrderByDescending(v => v.IsHighlighted)
                    .ThenBy(v => v.Title)
                    .Select(v => v.ImageUrl)
                    .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u))
            })
            .ToList();

        List<CompanyHubListItemDto> results;
        if (originLat is null || originLng is null)
        {
            results = byCompany
                .Select(x => MapHubItem(x.Company, x.VacancyCount, x.SampleImage, null, null, now))
                .OrderByDescending(x => x.IsHighlighted)
                .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        else
        {
            var lat = originLat.Value;
            var lng = originLng.Value;
            var routed = await Task.WhenAll(byCompany.Select(async x =>
            {
                var loc = x.Company.Location;
                if (loc is null)
                {
                    return (Item: (CompanyHubListItemDto?)null, Minutes: int.MaxValue);
                }

                var route = await _routing.GetRouteAsync(
                    lat, lng, loc.Latitude, loc.Longitude, mode, cancellationToken);
                var travelMinutes = (int)Math.Ceiling(route.DurationSeconds / 60.0);
                var distanceKm = route.DistanceMeters / 1000.0;
                if (radiusKm is > 0 && distanceKm > radiusKm.Value)
                {
                    return (Item: (CompanyHubListItemDto?)null, Minutes: int.MaxValue);
                }

                if (travelMinutes > maxMinutes)
                {
                    return (Item: (CompanyHubListItemDto?)null, Minutes: int.MaxValue);
                }

                var item = MapHubItem(
                    x.Company,
                    x.VacancyCount,
                    x.SampleImage,
                    travelMinutes,
                    Math.Round(distanceKm, 2),
                    now);
                return (Item: item, Minutes: travelMinutes);
            }));

            results = routed
                .Where(r => r.Item is not null)
                .OrderByDescending(r => r.Item!.IsHighlighted)
                .ThenBy(r => r.Minutes)
                .ThenBy(r => r.Item!.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(r => r.Item!)
                .ToList();
        }

        return Ok(results);
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var visibleIds = await GetHubVisibleCompanyIdsAsync(
            companies.Select(c => c.Id).ToList(), today, cancellationToken);
        // Org pages stay reachable via QR even without hub eligibility; vacancies list may be empty.
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
            .OrderBy(c => visibleIds.Contains(c.Id) ? 0 : 1)
            .ThenBy(c => c.ParentCompanyId is null ? 0 : 1)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .First();

        var displayName = StripBranchSuffix(primary.Name);
        return Ok(MapPage(
            kvk,
            Vestigingsnummer: null,
            displayName,
            primary,
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

        return Ok(MapPage(
            kvk,
            vestiging,
            company.Name,
            company,
            [company.Id],
            Branches: null));
    }

    private async Task<HashSet<Guid>> GetHubVisibleCompanyIdsAsync(
        IReadOnlyList<Guid> companyIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (companyIds.Count == 0)
        {
            return [];
        }

        var vacancies = await _db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId)
                        && v.Status == VacancyStatus.Active
                        && v.StartDate <= today
                        && v.EndDate >= today)
            .Select(v => new { v.CompanyId, v.StartDate, v.EndDate, v.Status })
            .ToListAsync(cancellationToken);

        return vacancies
            .Where(v =>
            {
                var remaining = v.EndDate.DayNumber - today.DayNumber;
                return remaining >= 0 && remaining <= VacancyVisibilityRules.CompanyHubMaxRemainingDays;
            })
            .Select(v => v.CompanyId)
            .ToHashSet();
    }

    private static PublicCompanyPageDto MapPage(
        string kvk,
        string? Vestigingsnummer,
        string name,
        Core.Entities.Company company,
        IReadOnlyList<Guid> companyIds,
        IReadOnlyList<PublicCompanyBranchDto>? Branches)
        => new(
            kvk,
            Vestigingsnummer,
            name,
            company.Address,
            company.LogoUrl,
            company.Location?.Latitude ?? 0,
            company.Location?.Longitude ?? 0,
            companyIds,
            Branches,
            company.HubAboutText,
            company.HubCultureText,
            company.HubVideoUrl,
            company.HubHighlightedUntil is DateTime until && until > DateTime.UtcNow);

    private static CompanyHubListItemDto MapHubItem(
        Core.Entities.Company company,
        int vacancyCount,
        string? sampleImage,
        int? travelMinutes,
        double? distanceKm,
        DateTime now)
    {
        var kvk = company.KvkNumber;
        var publicPath = CompanyPublicPaths.TryBuildPath(kvk, company.KvkEstablishmentId)
                         ?? $"/vestiging/{company.Id:D}";
        var highlighted = company.HubHighlightedUntil is DateTime until && until > now;
        return new CompanyHubListItemDto(
            company.Id,
            company.Name,
            company.Address,
            company.LogoUrl,
            company.Location?.Latitude ?? 0,
            company.Location?.Longitude ?? 0,
            vacancyCount,
            travelMinutes,
            distanceKm,
            highlighted,
            publicPath,
            sampleImage,
            CompanyPublicPaths.TryParseVestigingsnummer(company.KvkEstablishmentId, kvk),
            kvk);
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
    IReadOnlyList<PublicCompanyBranchDto>? Branches = null,
    string? AboutText = null,
    string? CultureText = null,
    string? VideoUrl = null,
    bool IsHighlighted = false);

public sealed record CompanyHubListItemDto(
    Guid CompanyId,
    string Name,
    string Address,
    string? LogoUrl,
    double Latitude,
    double Longitude,
    int ActiveVacancyCount,
    int? TravelMinutes,
    double? DistanceKm,
    bool IsHighlighted,
    string PublicPath,
    string? ImageUrl,
    string? Vestigingsnummer,
    string KvkNumber);
