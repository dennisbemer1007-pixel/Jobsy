using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/site")]
public class SiteController : ControllerBase
{
    private readonly IAboutPageSettingsService _aboutPage;

    public SiteController(IAboutPageSettingsService aboutPage)
    {
        _aboutPage = aboutPage;
    }

    /// <summary>Public “Wie zijn wij” page content.</summary>
    [HttpGet("about")]
    [AllowAnonymous]
    public async Task<ActionResult<AboutPageDto>> GetAbout(CancellationToken cancellationToken)
    {
        var snap = await _aboutPage.GetAsync(cancellationToken);
        return Ok(ToDto(snap));
    }

    internal static AboutPageDto ToDto(AboutPageSnapshot snap) =>
        new(snap.Title, snap.Lead, snap.BodyHtml, snap.UpdatedAtUtc);
}

public sealed record AboutPageDto(
    string Title,
    string Lead,
    string BodyHtml,
    DateTime? UpdatedAtUtc);
