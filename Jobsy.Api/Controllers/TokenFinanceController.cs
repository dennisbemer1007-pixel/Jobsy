using System.Text;
using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/tokens")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class TokenFinanceController : ControllerBase
{
    private readonly ITokenFinanceQueryService _finance;
    private readonly ITokenPurchaseInvoiceService _invoices;
    private readonly IVatBufferTransferService _vatBuffer;

    public TokenFinanceController(
        ITokenFinanceQueryService finance,
        ITokenPurchaseInvoiceService invoices,
        IVatBufferTransferService vatBuffer)
    {
        _finance = finance;
        _invoices = invoices;
        _vatBuffer = vatBuffer;
    }

    [HttpGet("finance/purchases")]
    public async Task<ActionResult<IEnumerable<TokenPurchaseFinanceDto>>> GetPurchases(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await _finance.GetPurchasesAsync(year, quarter, cancellationToken);
        return Ok(rows.Select(ToDto));
    }

    [HttpGet("finance/goodwill")]
    public async Task<ActionResult<IEnumerable<TokenGoodwillFinanceDto>>> GetGoodwill(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await _finance.GetGoodwillAsync(year, quarter, cancellationToken);
        return Ok(rows.Select(r => new TokenGoodwillFinanceDto(
            r.TransactionId,
            r.CompanyId,
            r.CompanyName,
            r.TokenAmount,
            0,
            0,
            0,
            r.Reason,
            r.IssuedByUserId,
            r.IssuedByName,
            r.CreatedAt)));
    }

    [HttpGet("finance/vat-transfers")]
    public async Task<ActionResult<IEnumerable<VatBufferTransferDto>>> GetVatTransfers(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await _vatBuffer.ListAsync(year, quarter, cancellationToken);
        return Ok(rows.Select(t => new VatBufferTransferDto(
            t.Id,
            t.TokenPurchaseInvoiceId,
            t.InvoiceNumber,
            MaskIban(t.DestinationIban),
            t.AmountCents,
            TokenVatPricing.FromCents(t.AmountCents),
            t.Status.ToString(),
            t.CreatedAt,
            t.ProcessedAt,
            t.Note)));
    }

    [HttpGet("finance/purchases/export")]
    public async Task<IActionResult> ExportPurchases(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var csv = await _finance.ExportPurchasesCsvAsync(year, quarter, cancellationToken);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var name = $"token-aankopen-{(year?.ToString() ?? "all")}-Q{(quarter?.ToString() ?? "all")}.csv";
        return File(bytes, "text/csv; charset=utf-8", name);
    }

    [HttpGet("finance/goodwill/export")]
    public async Task<IActionResult> ExportGoodwill(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var csv = await _finance.ExportGoodwillCsvAsync(year, quarter, cancellationToken);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var name = $"token-goodwill-{(year?.ToString() ?? "all")}-Q{(quarter?.ToString() ?? "all")}.csv";
        return File(bytes, "text/csv; charset=utf-8", name);
    }

    [HttpGet("invoices/{invoiceId:guid}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoices.GetAsync(invoiceId, cancellationToken);
            if (invoice is null)
            {
                return NotFound(new { message = "Factuur niet gevonden." });
            }

            var pdf = await _invoices.RenderPdfAsync(invoiceId, cancellationToken);
            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Factuur niet gevonden." });
        }
    }

    private static TokenPurchaseFinanceDto ToDto(TokenPurchaseFinanceRow r) =>
        new(
            r.InvoiceId,
            r.InvoiceNumber,
            r.CheckoutId,
            r.MolliePaymentId,
            r.CompanyId,
            r.CompanyName,
            r.PackSize,
            r.AmountExVatCents,
            r.VatAmountCents,
            r.TotalAmountCents,
            TokenVatPricing.FromCents(r.AmountExVatCents),
            TokenVatPricing.FromCents(r.VatAmountCents),
            TokenVatPricing.FromCents(r.TotalAmountCents),
            r.IssuedAt,
            r.InvoicePdfPath,
            r.VatDeclarationStatusLabel);

    private static string MaskIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban) || iban.Length < 8)
        {
            return "—";
        }

        return $"{iban[..4]}••••{iban[^4..]}";
    }
}

public sealed record TokenPurchaseFinanceDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CheckoutId,
    string MolliePaymentId,
    Guid CompanyId,
    string CompanyName,
    int PackSize,
    int AmountExVatCents,
    int VatAmountCents,
    int TotalAmountCents,
    decimal AmountExVatEuro,
    decimal VatAmountEuro,
    decimal TotalAmountEuro,
    DateTime IssuedAt,
    string InvoicePdfUrl,
    string? VatDeclarationStatusLabel = null);

public sealed record TokenGoodwillFinanceDto(
    Guid TransactionId,
    Guid CompanyId,
    string CompanyName,
    decimal TokenAmount,
    int AmountExVatCents,
    int VatAmountCents,
    int TotalAmountCents,
    string Reason,
    Guid? IssuedByUserId,
    string? IssuedByName,
    DateTime CreatedAt);

public sealed record VatBufferTransferDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    string DestinationIbanMasked,
    int AmountCents,
    decimal AmountEuro,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? Note);
