using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Api.Controllers;

/// <summary>
/// Mollie payment webhooks (application/x-www-form-urlencoded with id=tr_...).
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class MollieWebhooksController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ILogger<MollieWebhooksController> _logger;

    public MollieWebhooksController(IPaymentService payments, ILogger<MollieWebhooksController> logger)
    {
        _payments = payments;
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
            var status = await _payments.GetPaymentStatusAsync(id.Trim(), cancellationToken);
            _logger.LogInformation(
                "Mollie webhook {PaymentId}: status={Status}, paid={Paid}",
                status.PaymentId, status.Status, status.IsPaid);
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
