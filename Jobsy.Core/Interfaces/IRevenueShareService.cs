using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IRevenueShareService
{
    /// <summary>
    /// Applies 15%/5%/80% revenue share after a referred company token purchase is credited.
    /// Idempotent on <paramref name="tokenCheckoutId"/>.
    /// </summary>
    Task ApplyTokenPurchaseShareAsync(
        Guid tokenCheckoutId,
        Guid companyId,
        Guid? purchaseTokenTransactionId,
        int packSize,
        decimal purchaseAmountEuro,
        Guid? salesManagerUserId,
        DateTime? firstYearStartedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RevenueShareLog>> ListForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
