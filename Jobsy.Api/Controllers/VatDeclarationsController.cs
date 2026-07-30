using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/vat")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class VatDeclarationsController : ControllerBase
{
    private readonly IVatDeclarationService _declarations;
    private readonly IUserLookupService _users;

    public VatDeclarationsController(IVatDeclarationService declarations, IUserLookupService users)
    {
        _declarations = declarations;
        _users = users;
    }

    [HttpGet("open-periods")]
    public async Task<ActionResult<IEnumerable<VatOpenPeriodDto>>> GetOpenPeriods(
        CancellationToken cancellationToken)
        => Ok(await _declarations.GetOpenPeriodsAsync(cancellationToken));

    [HttpGet("preview")]
    public async Task<ActionResult<VatDeclarationPreviewDto>> Preview(
        [FromQuery] int year,
        [FromQuery] int quarter,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _declarations.PreviewAsync(year, quarter, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("generate")]
    public async Task<ActionResult<VatDeclarationListItemDto>> Generate(
        [FromBody] GenerateVatDeclarationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
            var d = await _declarations.GenerateAndConfirmAsync(
                request.Year,
                request.Quarter,
                actor?.Id,
                actor?.FullName,
                cancellationToken);

            return Ok(new VatDeclarationListItemDto(
                d.Id,
                d.Year,
                d.Quarter,
                d.PeriodLabel,
                d.Status.ToString(),
                d.Rubriek1OmzetExVatCents,
                d.Rubriek1VatCents,
                d.Rubriek5VoorbelastingCents,
                d.AmountDueCents,
                d.TokenInvoiceCount,
                d.GoodwillCount,
                d.SalesManagerInvoiceCount,
                d.GeneratedAt,
                d.GeneratedByName,
                d.PlatformCompanyName,
                d.PdfBytes is { Length: > 0 }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("declarations")]
    public async Task<ActionResult<IEnumerable<VatDeclarationListItemDto>>> List(
        CancellationToken cancellationToken)
        => Ok(await _declarations.ListAsync(cancellationToken));

    [HttpGet("declarations/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var d = await _declarations.GetAsync(id, cancellationToken);
            if (d is null)
            {
                return NotFound(new { message = "BTW-aangifte niet gevonden." });
            }

            var pdf = await _declarations.GetPdfAsync(id, cancellationToken);
            return File(pdf, "application/pdf", d.PdfFileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "BTW-aangifte niet gevonden." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sales-manager-costs")]
    public async Task<ActionResult<IEnumerable<SalesManagerCostFinanceDto>>> GetSalesManagerCosts(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await _declarations.GetSalesManagerCostsAsync(year, quarter, cancellationToken);
        return Ok(rows.Select(r => new SalesManagerCostFinanceDto(
            r.InvoiceId,
            r.InvoiceNumber,
            r.SalesManagerUserId,
            r.SalesManagerCompanyName,
            r.SubtotalExVat,
            r.VatAmount,
            r.TotalInclVat,
            r.VatTreatment,
            r.Status,
            r.PaidAt,
            r.VatDeclarationStatusLabel)));
    }
}

public sealed record GenerateVatDeclarationRequest(int Year, int Quarter);

public sealed record SalesManagerCostFinanceDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid SalesManagerUserId,
    string SalesManagerCompanyName,
    decimal SubtotalExVat,
    decimal VatAmount,
    decimal TotalInclVat,
    string VatTreatment,
    string Status,
    DateTime? PaidAt,
    string? VatDeclarationStatusLabel);
