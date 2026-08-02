using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IRevenueShareService
{
    /// <summary>
    /// Applies automated settlement after a referred company's Mollie token purchase is credited:
    /// ambassador tokens + direct SM commission (default 15%, within 1-year window) +
    /// optional indirect/upline SM (default 3%) + platform remainder.
    /// <paramref name="purchaseAmountExVatEuro"/> is the ex-BTW purchase base.
    /// Idempotent on <paramref name="tokenCheckoutId"/>.
    /// </summary>
    Task ApplyTokenPurchaseShareAsync(
        Guid tokenCheckoutId,
        Guid companyId,
        Guid? purchaseTokenTransactionId,
        int packSize,
        decimal purchaseAmountExVatEuro,
        Guid? salesManagerUserId,
        DateTime? firstYearStartedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RevenueShareLog>> ListForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
