using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ISelfBillingInvoiceService
{
    /// <param name="maxAmountExVat">
    /// Optional ex-VAT amount to invoice. When null, invoices the full uninvoiced balance.
    /// Oldest ledger entries are taken first; the last entry may be split.
    /// </param>
    Task<SelfBillingInvoice> CreateFromUninvoicedBalanceAsync(
        Guid salesManagerUserId,
        decimal? maxAmountExVat = null,
        CancellationToken cancellationToken = default);

    Task<SelfBillingInvoice> MarkPaidAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SelfBillingInvoice>> ListForSalesManagerAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default);

    Task<SelfBillingInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
