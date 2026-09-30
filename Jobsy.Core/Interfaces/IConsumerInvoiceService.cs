using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IConsumerInvoiceService
{
    Task<ConsumerPurchaseInvoice> CreateForDeepCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken = default);

    Task<ConsumerPurchaseInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsumerPurchaseInvoice>> ListAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> RenderPdfAsync(
        Guid invoiceId,
        string? culture = null,
        CancellationToken cancellationToken = default);
}

public sealed record ConsumerPurchaseFinanceDto(
    Guid InvoiceId,
    string InvoiceNumber,
    DateTime IssuedAt,
    int TotalAmountCents,
    decimal TotalAmountEuro,
    string Kind,
    string TestName,
    string? PaymentMethod,
    string? VatDeclarationStatusLabel,
    bool IsStub);
