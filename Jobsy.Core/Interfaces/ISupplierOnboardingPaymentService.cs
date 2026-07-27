using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ISupplierOnboardingPaymentService
{
    Task<SupplierOnboardingCheckoutResult> CreateCheckoutAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <param name="expectedCompanyId">When set, must match the checkout company (prevents IDOR).</param>
    Task<SupplierOnboardingCompleteResult> CompleteCheckoutAsync(
        string paymentId,
        Guid? actorUserId,
        Guid? expectedCompanyId = null,
        CancellationToken cancellationToken = default);
}

public sealed record SupplierOnboardingCheckoutResult(
    string PaymentId,
    string CheckoutUrl,
    decimal AmountEuro,
    bool IsStub);

public sealed record SupplierOnboardingCompleteResult(
    Guid CompanyId,
    string Status,
    bool CommissionCredited,
    int? FirstYearSupplierSlot);
