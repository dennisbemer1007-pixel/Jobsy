using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/site")]
public class SiteController : ControllerBase
{
    private readonly IAboutPageSettingsService _aboutPage;
    private readonly IVacancyDiscoveryIndex _discovery;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IPlatformFeatureService _features;

    public SiteController(
        IAboutPageSettingsService aboutPage,
        IVacancyDiscoveryIndex discovery,
        IPlatformCompanySettingsService companySettings,
        IPlatformFeatureService features)
    {
        _aboutPage = aboutPage;
        _discovery = discovery;
        _companySettings = companySettings;
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

    /// <summary>Public “Wie zijn wij” page content.</summary>
    [HttpGet("about")]
    [AllowAnonymous]
    public async Task<ActionResult<AboutPageDto>> GetAbout(CancellationToken cancellationToken)
    {
        var snap = await _aboutPage.GetAsync(cancellationToken);
        return Ok(ToDto(snap));
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

        var companyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        {
            var kvkPath = CompanyPublicPaths.TryBuildKvkPath(record.KvkNumber);
            if (kvkPath is not null)
            {
                companyPaths.Add(kvkPath);
            }

            var vestigingPath = CompanyPublicPaths.TryBuildPath(record.KvkNumber, record.Vestigingsnummer);
            if (vestigingPath is not null)
            {
                companyPaths.Add(vestigingPath);
            }
        }

        return Ok(new SiteCrawlIndexDto(vacancies, companyPaths.OrderBy(p => p, StringComparer.Ordinal).ToList()));
    }

    internal static AboutPageDto ToDto(AboutPageSnapshot snap) =>
        new(snap.Title, snap.Lead, snap.BodyHtml, snap.UpdatedAtUtc);
}

public sealed record AboutPageDto(
    string Title,
    string Lead,
    string BodyHtml,
    DateTime? UpdatedAtUtc);

public sealed record SiteBrandingDto(string CompanyName, string Slogan);

public sealed record SiteStatusDto(bool Maintenance, DateTime? ExpectedEndUtc);

public sealed record SiteCrawlIndexDto(
    IReadOnlyList<SiteCrawlVacancyDto> Vacancies,
    IReadOnlyList<string> CompanyPaths);

public sealed record SiteCrawlVacancyDto(Guid Id, DateOnly StartDate, DateOnly EndDate);
