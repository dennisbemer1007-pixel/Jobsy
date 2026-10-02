using System.Text.RegularExpressions;
using Jobsy.Api.Admin;
using Jobsy.Api.Models;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/sales-commercial")]
[RequiresFeature(PlatformFeature.Employers)]
public partial class SalesCommercialController : ControllerBase
{
    private static readonly Regex TrackingCodePattern = TrackingCodeRegex();

    private readonly ISalesCommercialService _sales;
    private readonly IPartnerFlyerPdfService _flyerPdf;
    private readonly ISalesAttributionResolver _attribution;
    private readonly ISalesLinkClickService _clicks;

    public SalesCommercialController(
        ISalesCommercialService sales,
        IPartnerFlyerPdfService flyerPdf,
        ISalesAttributionResolver attribution,
        ISalesLinkClickService clicks)
    {
        _sales = sales;
        _flyerPdf = flyerPdf;
        _attribution = attribution;
        _clicks = clicks;
    }

    /// <summary>Public partner catalog (rates + packages) for the sales landing page.</summary>
    [HttpGet("catalog")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<PartnerSalesCatalogDto>> GetCatalog(CancellationToken cancellationToken)
        => Ok(await _sales.GetPublicCatalogAsync(cancellationToken));

    /// <summary>
    /// Validate a referral code, optionally count a funnel click (no IP/UA stored), return cookie days.
    /// </summary>
    [HttpPost("referral/visit")]
    [AllowAnonymous]
    [AdminAuditExempt("Public referral click tracking; no admin write")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<object>> RecordReferralVisit(
        [FromBody] SalesReferralVisitRequest request,
        CancellationToken cancellationToken)
    {
        var active = await _attribution.ResolveActiveReferralAsync(request.Code, cancellationToken);
        var settings = await _sales.GetAdminAsync(cancellationToken);
        var cookieDays = settings.AttributionCookieDays > 0 ? settings.AttributionCookieDays : 30;

        if (active is null)
        {
            return Ok(new
            {
                Active = false,
                CookieDays = cookieDays,
                Code = (string?)null,
                Kind = (string?)null
            });
        }

        if (request.CountClick)
        {
            var channel = Enum.TryParse<SalesLinkChannel>(request.Channel, true, out var parsed)
                ? parsed
                : SalesTrackingCodes.ChannelFromQuery(request.Channel);
            await _clicks.RecordClickAsync(active.BeneficiaryUserId, channel, cancellationToken: cancellationToken);
        }

        return Ok(new
        {
            Active = true,
            CookieDays = cookieDays,
            active.Code,
            Kind = active.Kind.ToString()
        });
    }

    /// <summary>Printable A4 flyer PDF. Generic (no code) is public; personal code only when active.</summary>
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
            return BadRequest(new { message = "Ongeldige salescode. Gebruik het formaat SM-, BM- of IM-XXXXXX." });
        }

        if (normalized is not null)
        {
            var active = await _attribution.ResolveActiveReferralAsync(normalized, cancellationToken);
            if (active is null)
            {
                return NotFound(new { message = "Deze code kennen we niet." });
            }
        }

        var bytes = await _flyerPdf.RenderAsync(normalized, cancellationToken);
        // Fixed download name — never embed untrusted query text in Content-Disposition.
        var fileName = normalized is null
            ? "lobsy-partner-flyer.pdf"
            : $"lobsy-flyer-{normalized}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("admin")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SalesCommercialAdminDto>> GetAdmin(CancellationToken cancellationToken)
        => Ok(await _sales.GetAdminAsync(cancellationToken));

    [HttpPut("admin/settings")]
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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
                request.DirectCommissionRate,
                request.IndirectCommissionRate,
                request.CommissionDurationDays,
                request.PartnerCommissionRate,
                request.Year2DirectCommissionRate,
                request.Year3DirectCommissionRate,
                request.ReferredYear1DirectCommissionRate,
                request.CommissionHoldDays,
                request.PayoutMinimumEuro,
                request.IbanChangeHoldDays,
                request.AttributionCookieDays,
                cancellationToken);
            return Ok(new
            {
                settings.Id,
                settings.BaseTokenValueEuro,
                settings.HighlightCarouselTokens,
                settings.HighlightPulseTokens,
                settings.HighlightCarouselDays,
                settings.StartHighlightBonusTokens,
                settings.DirectCommissionRate,
                settings.IndirectCommissionRate,
                settings.CommissionDurationDays,
                settings.PartnerCommissionRate,
                settings.Year2DirectCommissionRate,
                settings.Year3DirectCommissionRate,
                settings.ReferredYear1DirectCommissionRate,
                settings.CommissionHoldDays,
                settings.PayoutMinimumEuro,
                settings.IbanChangeHoldDays,
                settings.AttributionCookieDays,
                settings.UpdatedAtUtc
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("admin/vacancy-type-costs")]
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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
    [AdminAudit(AdminAuditKeys.SettingsPricingUpdate, TargetType = AdminAuditKeys.TargetTypes.Setting)]
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
    [AdminAudit(AdminAuditKeys.SettingsPricingDelete, TargetType = AdminAuditKeys.TargetTypes.Setting, TargetRouteKey = "id")]
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

    [GeneratedRegex(@"^(SM|BM|IM)-[A-Z0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex TrackingCodeRegex();
}
