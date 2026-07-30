using Jobsy.Core.Interfaces;
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
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class MollieWebhooksController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ITokenPurchaseFulfillmentService _fulfillment;
    private readonly JobsyDbContext _db;
    private readonly ILogger<MollieWebhooksController> _logger;

    public MollieWebhooksController(
        IPaymentService payments,
        ITokenPurchaseFulfillmentService fulfillment,
        JobsyDbContext db,
        ILogger<MollieWebhooksController> logger)
    {
        _payments = payments;
        _fulfillment = fulfillment;
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

        try
        {
            var paymentId = id.Trim();
            var status = await _payments.GetPaymentStatusAsync(paymentId, cancellationToken);
            _logger.LogInformation(
                "Mollie webhook {PaymentId}: status={Status}, paid={Paid}",
                status.PaymentId, status.Status, status.IsPaid);

            if (status.IsPaid)
            {
                var checkoutId = await _db.TokenPurchaseCheckouts.AsNoTracking()
                    .Where(c => c.PaymentId == paymentId)
                    .Select(c => (Guid?)c.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (checkoutId is Guid cid)
                {
                    var result = await _fulfillment.TryFulfillPaidCheckoutAsync(
                        cid,
                        actorUserId: null,
                        allowDevStubMarkPaid: false,
                        cancellationToken);
                    if (result is not null)
                    {
                        _logger.LogInformation(
                            "Mollie webhook fulfilled checkout {CheckoutId} → invoice {InvoiceNumber}",
                            result.CheckoutId, result.InvoiceNumber);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Always 200 so Mollie does not retry aggressively on our app bugs;
            // status is re-checked on redirect return / complete.
            _logger.LogWarning(ex, "Mollie webhook processing failed for {PaymentId}", id);
        }

        return Ok();
    }
}
