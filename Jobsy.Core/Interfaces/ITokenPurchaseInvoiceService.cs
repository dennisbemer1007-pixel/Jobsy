using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ITokenPurchaseInvoiceService
{
    Task<TokenPurchaseInvoice> CreateForCheckoutAsync(
        TokenPurchaseCheckout checkout,
        TokenTransaction purchaseTransaction,
        CancellationToken cancellationToken = default);

    Task<TokenPurchaseInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task<TokenPurchaseInvoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    Task<byte[]> RenderPdfAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
