using System.Text.RegularExpressions;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/sales-commercial")]
public partial class SalesCommercialController : ControllerBase
{
    private static readonly Regex TrackingCodePattern = TrackingCodeRegex();

    private readonly ISalesCommercialService _sales;
    private readonly IPartnerFlyerPdfService _flyerPdf;

    public SalesCommercialController(ISalesCommercialService sales, IPartnerFlyerPdfService flyerPdf)
    {
        _sales = sales;
        _flyerPdf = flyerPdf;
    }

    /// <summary>Public partner catalog (rates + packages) for the sales landing page.</summary>
    [HttpGet("catalog")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<PartnerSalesCatalogDto>> GetCatalog(CancellationToken cancellationToken)
        => Ok(await _sales.GetPublicCatalogAsync(cancellationToken));

    /// <summary>Printable A4 flyer PDF with optional salesmanager tracking code.</summary>
    [HttpGet("flyer.pdf")]
    [AllowAnonymous]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> GetFlyerPdf(
        [FromQuery] string? trackingCode,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeTrackingCode(trackingCode);
        if (trackingCode is not null && normalized is null)
        {
            return BadRequest(new { message = "Ongeldige salescode. Gebruik het formaat SM-XXXXXX." });
        }

        var bytes = await _flyerPdf.RenderAsync(normalized, cancellationToken);
        // Fixed download name — never embed untrusted query text in Content-Disposition.
        return File(bytes, "application/pdf", "lobsy-partner-flyer.pdf");
    }

    [HttpGet("admin")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SalesCommercialAdminDto>> GetAdmin(CancellationToken cancellationToken)
        => Ok(await _sales.GetAdminAsync(cancellationToken));

    [HttpPut("admin/settings")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<object>> UpdateSettings(
        [FromBody] UpdateSalesCommercialSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _sales.UpdateSettingsAsync(
                request.BaseTokenValueEuro,
                request.HighlightCarouselTokens,
                request.HighlightPulseTokens,
                request.HighlightCarouselDays,
                request.StartHighlightBonusTokens,
                cancellationToken);
            return Ok(new
            {
                settings.Id,
                settings.BaseTokenValueEuro,
                settings.HighlightCarouselTokens,
                settings.HighlightPulseTokens,
                settings.HighlightCarouselDays,
                settings.StartHighlightBonusTokens,
                settings.UpdatedAtUtc
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("admin/vacancy-type-costs")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<object>> UpdateVacancyTypeCost(
        [FromBody] UpdateVacancyTypeCostRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var row = await _sales.UpdateVacancyTypeCostAsync(
                request.Kind,
                request.CostTokens,
                request.IsActive,
                cancellationToken);
            return Ok(new
            {
                row.Id,
                Kind = row.Kind.ToString(),
                row.CostTokens,
                row.IsActive
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("admin/packages")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<object>> UpsertPackage(
        [FromBody] UpsertSalesPackageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var package = await _sales.UpsertPackageAsync(
                new SalesPackage
                {
                    Id = request.Id ?? Guid.Empty,
                    Name = request.Name,
                    Code = request.Code,
                    Category = request.Category,
                    TokenAmount = request.TokenAmount,
                    PriceEuro = request.PriceEuro,
                    Description = request.Description,
                    IsActive = request.IsActive,
                    SortOrder = request.SortOrder
                },
                cancellationToken);
            return Ok(new
            {
                package.Id,
                package.Name,
                package.Code,
                Category = package.Category.ToString(),
                package.TokenAmount,
                package.PriceEuro,
                package.Description,
                package.IsActive,
                package.SortOrder
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("admin/packages/{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<IActionResult> DeletePackage(Guid id, CancellationToken cancellationToken)
    {
        await _sales.DeletePackageAsync(id, cancellationToken);
        return NoContent();
    }

    private static string? NormalizeTrackingCode(string? trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var normalized = trackingCode.Trim().ToUpperInvariant();
        return TrackingCodePattern.IsMatch(normalized) ? normalized : null;
    }

    [GeneratedRegex(@"^SM-[A-Z0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex TrackingCodeRegex();
}
