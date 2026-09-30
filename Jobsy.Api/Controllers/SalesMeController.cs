using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Sales;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>Self-service portal APIs for the signed-in sales beneficiary (SalesManager).</summary>
[ApiController]
[Route("api/sales/me")]
[Authorize(Roles = JobsyRoles.SalesManager)]
public sealed class SalesMeController : ControllerBase
{
    private readonly ISalesBeneficiaryService _beneficiary;
    private readonly ISalesDashboardReadService _dashboard;
    private readonly ISalesEmployerPortalReadService _employers;
    private readonly ISalesLinkToolkitService _linkToolkit;
    private readonly ISalesMaterialsPdfService _materials;

    public SalesMeController(
        ISalesBeneficiaryService beneficiary,
        ISalesDashboardReadService dashboard,
        ISalesEmployerPortalReadService employers,
        ISalesLinkToolkitService linkToolkit,
        ISalesMaterialsPdfService materials)
    {
        _beneficiary = beneficiary;
        _dashboard = dashboard;
        _employers = employers;
        _linkToolkit = linkToolkit;
        _materials = materials;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<SalesDashboardDto>> GetDashboard(
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _dashboard.GetAsync(me.UserId, period ?? "year", cancellationToken: cancellationToken);
        return Ok(dto);
    }

    [HttpGet("employers")]
    public async Task<ActionResult<SalesEmployerPageDto>> ListEmployers(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        SalesEmployerStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<SalesEmployerStatus>(status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var dto = await _employers.ListAsync(
            me.UserId,
            q,
            statusFilter,
            year,
            page,
            cancellationToken: cancellationToken);
        return Ok(dto);
    }

    [HttpGet("employers/{companyId:guid}")]
    public async Task<ActionResult<SalesEmployerDetailDto>> GetEmployer(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _employers.GetDetailAsync(me.UserId, companyId, cancellationToken: cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpGet("link")]
    public async Task<ActionResult<SalesLinkToolkitDto>> GetLinkToolkit(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var dto = await _linkToolkit.GetAsync(me.UserId, cancellationToken);
        if (dto is null)
        {
            return NotFound(new { message = "Rond eerst onboarding af om je link te gebruiken." });
        }

        return Ok(dto);
    }

    [HttpGet("materials/qr.png")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadQrPng(CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var toolkit = await _linkToolkit.GetAsync(me.UserId, cancellationToken);
        if (toolkit is null)
        {
            return NotFound(new { message = "Rond eerst onboarding af om je QR te downloaden." });
        }

        var bytes = SalesQr.PngForSize(toolkit.QrUrl, 1024);
        return File(bytes, "image/png", $"lobsy-qr-{toolkit.TrackingCode}.png");
    }

    [HttpGet("materials/{kind}.pdf")]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadMaterial(string kind, CancellationToken cancellationToken)
    {
        var me = await _beneficiary.GetOrThrowAsync(User, cancellationToken);
        var toolkit = await _linkToolkit.GetAsync(me.UserId, cancellationToken);
        if (toolkit is null)
        {
            return NotFound(new { message = "Rond eerst onboarding af om materiaal te downloaden." });
        }

        var code = toolkit.TrackingCode;
        try
        {
            var (bytes, fileName) = kind.Trim().ToLowerInvariant() switch
            {
                "flyer" => (
                    await _materials.FlyerA4Async(code, cancellationToken),
                    $"lobsy-flyer-{code}.pdf"),
                "visitekaartje" or "visitekaartjes" or "cards" => (
                    await _materials.BusinessCardsAsync(code, cancellationToken),
                    $"lobsy-visitekaartje-{code}.pdf"),
                "prijskaart" or "prices" => (
                    await _materials.PriceCardAsync(code, cancellationToken),
                    $"lobsy-prijskaart-{code}.pdf"),
                "presentatie" or "presentation" => (
                    await _materials.PresentationAsync(
                        code,
                        toolkit.DisplayName,
                        toolkit.CompanyName,
                        toolkit.AccountEmail,
                        cancellationToken),
                    $"lobsy-presentatie-{code}.pdf"),
                _ => throw new ArgumentException("Onbekend materiaal. Gebruik flyer, visitekaartje, prijskaart of presentatie.")
            };

            return File(bytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
