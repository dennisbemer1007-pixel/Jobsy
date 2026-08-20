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

    public SiteController(IAboutPageSettingsService aboutPage, IVacancyDiscoveryIndex discovery)
    {
        _aboutPage = aboutPage;
        _discovery = discovery;
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

public sealed record SiteCrawlIndexDto(
    IReadOnlyList<SiteCrawlVacancyDto> Vacancies,
    IReadOnlyList<string> CompanyPaths);

public sealed record SiteCrawlVacancyDto(Guid Id, DateOnly StartDate, DateOnly EndDate);
