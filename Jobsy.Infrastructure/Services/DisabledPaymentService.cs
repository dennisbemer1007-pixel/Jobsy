using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Legacy placeholder kept for reference. Prefer <see cref="MolliePaymentService"/>.
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
        _logger.LogError("Payment checkout blocked: no Mollie payment service registered.");
        throw new InvalidOperationException(
            "Betalingen zijn niet geconfigureerd. Sla een Mollie API-key op onder Admin → Integraties.");
    }

    public Task<PaymentStatusResult> GetPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Betalingen zijn niet geconfigureerd. Sla een Mollie API-key op onder Admin → Integraties.");
    }
}
