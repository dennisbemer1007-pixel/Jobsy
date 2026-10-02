using System.Text.Json.Serialization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/site")]
public class SiteController : ControllerBase
{
    private readonly IVacancyDiscoveryIndex _discovery;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly ILegalIdentity _legalIdentity;
    private readonly IPublicCompanyQuery _publicCompanies;
    private readonly IPlatformFeatureService _features;

    public SiteController(
        IVacancyDiscoveryIndex discovery,
        IPlatformCompanySettingsService companySettings,
        ILegalIdentity legalIdentity,
        IPublicCompanyQuery publicCompanies,
        IPlatformFeatureService features)
    {
        _discovery = discovery;
        _companySettings = companySettings;
        _legalIdentity = legalIdentity;
        _publicCompanies = publicCompanies;
        _features = features;
    }

    /// <summary>
    /// Maintenance state for the Web host's 503 middleware (errors 05). Anonymous and allowed
    /// through <see cref="Jobsy.Api.Security.MaintenanceApiMiddleware"/>, otherwise the Web host
    /// could never learn the switch was flipped back off. The admin note is never returned.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<SiteStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var snap = await _features.GetAsync(cancellationToken);
        return Ok(new SiteStatusDto(
            snap.MaintenanceEnabled,
            snap.MaintenanceEnabled ? snap.MaintenanceExpectedEndUtc : null));
    }

    /// <summary>
    /// Public header branding (company name + slogan). No addresses, KvK, IBAN or other PII.
    /// </summary>
    [HttpGet("branding")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<SiteBrandingDto>> GetBranding(CancellationToken cancellationToken)
    {
        var snap = await _companySettings.GetAsync(cancellationToken);
        return Ok(new SiteBrandingDto(snap.CompanyName, snap.Slogan));
    }

    /// <summary>Lobsy legal identity for privacy/footer pages. Empty fields omitted.</summary>
    [HttpGet("legal")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<LegalIdentityDto>> GetLegal(CancellationToken cancellationToken)
    {
        var snap = await _legalIdentity.GetAsync(cancellationToken);
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(LegalIdentityDto.From(snap));
    }

    /// <summary>
    /// Public vacancy and employer paths for sitemap.xml. No descriptions or PII.
    /// </summary>
    [HttpGet("crawl-index")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<SiteCrawlIndexDto>> GetCrawlIndex(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var records = await _discovery.GetActiveAsync(cancellationToken);
        var vacancies = records
            .Where(r => VacancyVisibilityRules.IsPubliclyVisible(r, today))
            .Take(5_000)
            .Select(r => new SiteCrawlVacancyDto(r.Id, r.StartDate, r.EndDate))
            .ToList();

        var companyPaths = await _publicCompanies.GetSitemapCompanyPathsAsync(cancellationToken);
        return Ok(new SiteCrawlIndexDto(vacancies, companyPaths));
    }
}

public sealed record SiteBrandingDto(string CompanyName, string Slogan);

public sealed record SiteStatusDto(bool Maintenance, DateTime? ExpectedEndUtc);

public sealed record SiteCrawlIndexDto(
    IReadOnlyList<SiteCrawlVacancyDto> Vacancies,
    IReadOnlyList<string> CompanyPaths);

public sealed record SiteCrawlVacancyDto(Guid Id, DateOnly StartDate, DateOnly EndDate);

public sealed record LegalIdentityDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TradeName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Street,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PostalCode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? City,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Country,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? KvkNumber,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? VatNumber,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PrivacyEmail,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SupportEmail,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SchoolsEmail)
{
    public static LegalIdentityDto From(LegalIdentitySnapshot snap) => new(
        NullIfEmpty(snap.Name),
        NullIfEmpty(snap.TradeName),
        NullIfEmpty(snap.Street),
        NullIfEmpty(snap.PostalCode),
        NullIfEmpty(snap.City),
        NullIfEmpty(snap.Country),
        NullIfEmpty(snap.KvkNumber),
        NullIfEmpty(snap.VatNumber),
        NullIfEmpty(snap.PrivacyEmail),
        NullIfEmpty(snap.SupportEmail),
        NullIfEmpty(snap.SchoolsEmail));

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
