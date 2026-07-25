using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Production placeholder — real Mollie webhook verification must replace the Development stub.
/// </summary>
public sealed class DisabledPaymentService : IPaymentService
{
    private readonly ILogger<DisabledPaymentService> _logger;

    public DisabledPaymentService(ILogger<DisabledPaymentService> logger)
    {
        _logger = logger;
    }

    public Task<PaymentCheckoutResult> CreateTokenPurchaseCheckoutAsync(
        Guid companyId,
        int packSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError("Payment checkout blocked: Mollie stub is Development-only.");
        throw new InvalidOperationException(
            "Betalingen zijn niet geconfigureerd. Configureer een echte Mollie-integratie buiten Development.");
    }

    public Task<PaymentStatusResult> GetPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Betalingen zijn niet geconfigureerd buiten Development.");
    }
}
