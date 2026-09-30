using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Api.Controllers;

/// <summary>
/// Mollie payment webhooks (application/x-www-form-urlencoded with id=tr_...).
/// On paid token checkouts: fulfill credit + invoice + BTW-buffer queue.
/// Refunds/chargebacks book negative commission corrections (D3).
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class MollieWebhooksController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ITokenPurchaseFulfillmentService _fulfillment;
    private readonly ISalesCorrectionService _corrections;
    private readonly JobsyDbContext _db;
    private readonly ILogger<MollieWebhooksController> _logger;

    public MollieWebhooksController(
        IPaymentService payments,
        ITokenPurchaseFulfillmentService fulfillment,
        ISalesCorrectionService corrections,
        JobsyDbContext db,
        ILogger<MollieWebhooksController> logger)
    {
        _payments = payments;
        _fulfillment = fulfillment;
        _corrections = corrections;
        _db = db;
        _logger = logger;
    }

    [HttpPost("mollie")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> MolliePayment(
        [FromForm] string? id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var paymentId = id.Trim();
        try
        {
            // Poll Mollie (works for iDEAL and creditcard); marks session Paid then fulfills immediately.
            var status = await _payments.GetPaymentStatusAsync(paymentId, cancellationToken);
            _logger.LogInformation(
                "Mollie webhook {PaymentId}: status={Status}, paid={Paid}, method={Method}, refunded={Refunded}, chargeback={Chargeback}",
                status.PaymentId, status.Status, status.IsPaid, status.Method ?? "unknown",
                status.AmountRefundedEuro, status.AmountChargedBackEuro);

            var checkout = await _db.TokenPurchaseCheckouts.AsNoTracking()
                .Where(c => c.PaymentId == paymentId)
                .Select(c => new { c.Id, c.AmountEuro, c.TotalAmountCents, c.Status })
                .FirstOrDefaultAsync(cancellationToken);

            if (checkout is null)
            {
                // Unknown payment id (not a token checkout we track) — acknowledge.
                return Ok();
            }

            if (status.IsPaid)
            {
                var result = await _fulfillment.TryFulfillPaidCheckoutAsync(
                    checkout.Id,
                    actorUserId: null,
                    allowDevStubMarkPaid: false,
                    cancellationToken);
                if (result is not null)
                {
                    _logger.LogInformation(
                        "Mollie webhook fulfilled checkout {CheckoutId} method={Method} → invoice {InvoiceNumber} (tokens + commission settlement)",
                        result.CheckoutId, status.Method ?? "unknown", result.InvoiceNumber);
                }
            }
            else if (status.AmountRefundedEuro <= 0 && status.AmountChargedBackEuro <= 0)
            {
                // Definitive non-paid status without reversals — acknowledge so Mollie stops retrying.
                return Ok();
            }

            var amountPaid = status.AmountEuro > 0
                ? status.AmountEuro
                : checkout.AmountEuro > 0
                    ? checkout.AmountEuro
                    : checkout.TotalAmountCents / 100m;

            if (status.AmountRefundedEuro > 0 || status.AmountChargedBackEuro > 0)
            {
                await _corrections.ApplyPaymentReversalsAsync(
                    checkout.Id,
                    amountPaid,
                    status.AmountRefundedEuro,
                    status.AmountChargedBackEuro,
                    cancellationToken);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            // Transient / app failures: non-2xx so Mollie retries; also persist for ops reprocess.
            _logger.LogError(ex, "Mollie webhook processing failed for {PaymentId}", paymentId);
            await TryWritePlatformLogAsync(paymentId, ex, cancellationToken);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private async Task TryWritePlatformLogAsync(
        string paymentId,
        Exception ex,
        CancellationToken cancellationToken)
    {
        try
        {
            _db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Error,
                Category = "MollieWebhook",
                Message = $"Webhook processing failed for payment {paymentId}",
                DetailsJson = JsonSerializer.Serialize(new
                {
                    paymentId,
                    error = ex.GetType().Name,
                    // Message only — no stack / PII in platform logs.
                    message = ex.Message.Length > 400 ? ex.Message[..400] : ex.Message
                }),
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Failed to persist Mollie webhook PlatformLog for {PaymentId}", paymentId);
        }
    }
}
