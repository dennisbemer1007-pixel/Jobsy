using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/partner-affiliate")]
[Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary}")]
public class PartnerAffiliateController : ControllerBase
{
    private readonly IPartnerAffiliateService _partners;
    private readonly ISelfBillingInvoiceService _invoices;
    private readonly ISalesManagerPayoutService _payouts;
    private readonly IUserLookupService _users;

    public PartnerAffiliateController(
        IPartnerAffiliateService partners,
        ISelfBillingInvoiceService invoices,
        ISalesManagerPayoutService payouts,
        IUserLookupService users)
    {
        _partners = partners;
        _invoices = invoices;
        _payouts = payouts;
        _users = users;
    }

    [HttpGet("me")]
    public async Task<ActionResult<PartnerAffiliateMeDto>> GetMine(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _partners.GetMineAsync(user.Id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("token-log")]
    public async Task<ActionResult<IEnumerable<PartnerAffiliateTokenLogRowDto>>> GetTokenLog(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _partners.GetTokenLogAsync(user.Id, cancellationToken));
    }

    [HttpGet("toolkit")]
    public async Task<ActionResult<PartnerAffiliateToolkitDto>> GetToolkit(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _partners.GetToolkitAsync(user.Id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("me/billing")]
    public async Task<ActionResult<PartnerAffiliateBillingDto>> GetBilling(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _partners.GetBillingAsync(user.Id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("me/billing")]
    public async Task<ActionResult<PartnerAffiliateBillingDto>> UpdateBilling(
        [FromBody] PartnerAffiliateBillingUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _partners.UpdateBillingAsync(
            user.Id,
            new PartnerAffiliateBillingUpdate(
                request.CompanyName,
                request.KvkNumber,
                request.VatNumber,
                request.Address,
                request.PostalCode,
                request.City,
                request.Country,
                request.Iban,
                request.ClearIban),
            cancellationToken);
        return Ok(dto);
    }

    [HttpGet("me/invoices")]
    public async Task<ActionResult<IEnumerable<SelfBillingInvoiceDto>>> ListMyInvoices(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var invoices = await _invoices.ListForSalesManagerAsync(user.Id, cancellationToken);
        return Ok(invoices.Select(MapInvoice));
    }

    [HttpGet("me/invoices/{invoiceId:guid}/download")]
    public async Task<IActionResult> DownloadMyInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var pdf = await _payouts.RenderInvoicePdfAsync(invoiceId, user.Id, cancellationToken);
            var invoice = await _invoices.GetAsync(invoiceId, cancellationToken);
            var fileName = $"{invoice?.InvoiceNumber ?? invoiceId.ToString("N")}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("me/payouts/preview")]
    public async Task<ActionResult<SalesManagerPayoutPreviewDto>> GetPayoutPreview(
        [FromQuery] decimal? amountExVat,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _payouts.GetPreviewAsync(user.Id, amountExVat, cancellationToken));
    }

    [HttpPost("me/payouts/checkout")]
    public async Task<ActionResult<SalesManagerPayoutCheckoutResult>> CreatePayoutCheckout(
        [FromBody] CreatePartnerAffiliatePayoutCheckoutRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            if (request?.AmountExVat is null or <= 0)
            {
                return BadRequest(new { message = "Geef een bedrag excl. BTW op om uit te betalen." });
            }

            return Ok(await _payouts.CreateCheckoutAsync(user.Id, request.AmountExVat.Value, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/payouts/complete")]
    public async Task<ActionResult<SalesManagerPayoutCompleteResult>> CompletePayoutCheckout(
        [FromBody] CompletePartnerAffiliatePayoutRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _payouts.CompleteCheckoutAsync(request.PaymentId, user.Id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    private static SelfBillingInvoiceDto MapInvoice(Core.Entities.SelfBillingInvoice i) =>
        new(
            i.Id,
            i.InvoiceNumber,
            i.SubtotalExVat,
            i.VatAmount,
            i.TotalInclVat,
            i.Status.ToString(),
            i.CreatedAt,
            i.IssuedAt,
            i.PaidAt);
}

public record CreatePartnerAffiliatePayoutCheckoutRequest(decimal? AmountExVat);

public record CompletePartnerAffiliatePayoutRequest(string PaymentId);

public record PartnerAffiliateBillingUpdateRequest(
    string? CompanyName,
    string? KvkNumber,
    string? VatNumber,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? Iban,
    bool ClearIban = false);
