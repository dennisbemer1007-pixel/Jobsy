using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IVatDeclarationService
{
    Task<IReadOnlyList<VatOpenPeriodDto>> GetOpenPeriodsAsync(CancellationToken cancellationToken = default);

    Task<VatDeclarationPreviewDto> PreviewAsync(
        int year,
        int quarter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms the declaration: locks open token + paid SM invoices in the period,
    /// stores branded PDF, and labels lines as processed for this period.
    /// </summary>
    Task<VatDeclaration> GenerateAndConfirmAsync(
        int year,
        int quarter,
        Guid? actorUserId = null,
        string? actorName = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VatDeclarationListItemDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<VatDeclaration?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<byte[]> GetPdfAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesManagerCostFinanceRow>> GetSalesManagerCostsAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default);
}

public sealed record VatOpenPeriodDto(
    int Year,
    int Quarter,
    string PeriodLabel,
    int OpenTokenInvoiceCount,
    int OpenSalesManagerInvoiceCount,
    bool HasOpenItems);

public sealed record VatDeclarationPreviewDto(
    int Year,
    int Quarter,
    string PeriodLabel,
    int Rubriek1OmzetExVatCents,
    int Rubriek1VatCents,
    int TokenInvoiceCount,
    int GoodwillCount,
    int Rubriek5VoorbelastingCents,
    int Rubriek5CostExVatCents,
    int SalesManagerInvoiceCount,
    int AmountDueCents,
    bool AlreadyDeclared);

public sealed record VatDeclarationListItemDto(
    Guid Id,
    int Year,
    int Quarter,
    string PeriodLabel,
    string Status,
    int Rubriek1OmzetExVatCents,
    int Rubriek1VatCents,
    int Rubriek5VoorbelastingCents,
    int AmountDueCents,
    int TokenInvoiceCount,
    int GoodwillCount,
    int SalesManagerInvoiceCount,
    DateTime GeneratedAt,
    string? GeneratedByName,
    string PlatformCompanyName,
    bool HasPdf);

public sealed record SalesManagerCostFinanceRow(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid SalesManagerUserId,
    string SalesManagerCompanyName,
    decimal SubtotalExVat,
    decimal VatAmount,
    decimal TotalInclVat,
    string VatTreatment,
    string Status,
    DateTime? PaidAt,
    string? VatDeclarationStatusLabel);
