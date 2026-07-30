using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IVatBufferTransferService
{
    /// <summary>
    /// Queues a BTW-buffer transfer for the invoice VAT amount toward the configured Knab IBAN.
    /// Description/kenmerk is always the invoice number.
    /// </summary>
    Task<VatBufferTransfer> QueueForInvoiceAsync(
        TokenPurchaseInvoice invoice,
        CancellationToken cancellationToken = default);

    /// <summary>Processes pending transfer orders (logboek-trigger for bank execution).</summary>
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VatBufferTransfer>> ListAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default);
}
