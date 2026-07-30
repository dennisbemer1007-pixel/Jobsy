using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ITokenFinanceQueryService
{
    Task<IReadOnlyList<TokenPurchaseFinanceRow>> GetPurchasesAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TokenGoodwillFinanceRow>> GetGoodwillAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default);

    Task<string> ExportPurchasesCsvAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default);

    Task<string> ExportGoodwillCsvAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default);
}

public sealed record TokenPurchaseFinanceRow(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CheckoutId,
    string MolliePaymentId,
    Guid CompanyId,
    string CompanyName,
    int PackSize,
    int AmountExVatCents,
    int VatAmountCents,
    int TotalAmountCents,
    DateTime IssuedAt,
    string InvoicePdfPath,
    string? VatDeclarationStatusLabel = null);

public sealed record TokenGoodwillFinanceRow(
    Guid TransactionId,
    Guid CompanyId,
    string CompanyName,
    decimal TokenAmount,
    string Reason,
    Guid? IssuedByUserId,
    string? IssuedByName,
    DateTime CreatedAt);
