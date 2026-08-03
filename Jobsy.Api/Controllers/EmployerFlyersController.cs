using Jobsy.Core.Authorization;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/employer-flyers")]
public sealed class EmployerFlyersController : ControllerBase
{
    private readonly IEmployerRaamflyerService _flyers;
    private readonly ICompanyAuthorizationService _companyAuth;

    public EmployerFlyersController(
        IEmployerRaamflyerService flyers,
        ICompanyAuthorizationService companyAuth)
    {
        _flyers = flyers;
        _companyAuth = companyAuth;
    }

    /// <summary>
    /// Public QR resolver: 1 open vacancy → detail path; otherwise map with company focus.
    /// Returns only the redirect path (no vacancy counts/IDs) to limit reconnaissance.
    /// </summary>
    [HttpGet("public/branches/{companyId:guid}/route")]
    [AllowAnonymous]
    [EnableRateLimiting("public-pdf")]
    public async Task<ActionResult<BranchFlyerRouteDto>> ResolvePublicRoute(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var target = await _flyers.ResolveBranchQrTargetAsync(companyId, cancellationToken);
            // Never expose vacancy IDs on this anonymous probe surface; the map deep-link
            // auto-opens the single vacancy (or cluster) after load.
            var redirectPath = $"/?company={companyId:D}";
            return Ok(new BranchFlyerRouteDto(redirectPath));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Vestiging niet gevonden." });
        }
    }

    [HttpGet("branch/{companyId:guid}.pdf")]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadBranchFlyer(
        Guid companyId,
        [FromQuery] string format = "A4",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(User, companyId, cancellationToken);
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }

        var pdfFormat = ParseFormat(format);
        try
        {
            var pdf = await _flyers.RenderBranchFlyerAsync(companyId, pdfFormat, cancellationToken);
            var size = pdfFormat == RaamflyerFormat.A3 ? "A3" : "A4";
            return File(pdf, "application/pdf", $"lobsy-raamflyer-{size}-{companyId:N}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Vestiging niet gevonden." });
        }
    }

    [HttpGet("overview.pdf")]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadOverviewFlyer(
        [FromQuery] string? title = null,
        [FromQuery] string format = "A4",
        [FromQuery] Guid[]? companyIds = null,
        CancellationToken cancellationToken = default)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        List<Guid> ids;
        if (companyIds is { Length: > 0 })
        {
            ids = companyIds.Distinct().ToList();
            if (accessible is not null && ids.Any(id => !accessible.Contains(id)))
            {
                return Forbid();
            }
        }
        else
        {
            if (accessible is null)
            {
                return BadRequest(new { message = "Selecteer vestigingen voor de overzichtsflyer." });
            }

            ids = accessible.ToList();
        }

        if (ids.Count == 0)
        {
            return BadRequest(new { message = "Geen vestigingen beschikbaar voor de overzichtsflyer." });
        }

        var pdfFormat = ParseFormat(format);
        var heading = string.IsNullOrWhiteSpace(title) ? "Onze vestigingen" : title.Trim();
        try
        {
            var pdf = await _flyers.RenderOverviewFlyerAsync(ids, heading, pdfFormat, cancellationToken);
            var size = pdfFormat == RaamflyerFormat.A3 ? "A3" : "A4";
            return File(pdf, "application/pdf", $"lobsy-overzicht-raamflyer-{size}.pdf");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static RaamflyerFormat ParseFormat(string? format)
        => string.Equals(format?.Trim(), "A3", StringComparison.OrdinalIgnoreCase)
            ? RaamflyerFormat.A3
            : RaamflyerFormat.A4;
}

public sealed record BranchFlyerRouteDto(string RedirectPath);
