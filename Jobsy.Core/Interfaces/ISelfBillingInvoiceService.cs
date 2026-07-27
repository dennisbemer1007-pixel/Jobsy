using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ISelfBillingInvoiceService
{
    Task<SelfBillingInvoice> CreateFromUninvoicedBalanceAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default);

    Task<SelfBillingInvoice> MarkPaidAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SelfBillingInvoice>> ListForSalesManagerAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default);

    Task<SelfBillingInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
