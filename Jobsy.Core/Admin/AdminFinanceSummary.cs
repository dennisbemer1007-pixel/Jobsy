namespace Jobsy.Core.Admin;

/// <summary>Period finance roll-up for the admin dashboard KPI and Omzet &amp; transacties.</summary>
public sealed record AdminFinanceSummary(
    string Period,
    int RevenueInclVatCents,
    int RevenueExVatCents,
    int PreviousRevenueInclVatCents,
    int TokensSold,
    int OpenAtMollieCents,
    int OpenAtMollieCount,
    int? OldestOpenMollieDays,
    int VatBufferPendingCents,
    int OpenPayoutsCents,
    int OpenPayoutsCount,
    IReadOnlyList<AdminFinanceOpenPayoutPreview> OpenPayoutPreviews);

/// <summary>Open self-billing invoice row for finance aside / uitbetalingen (amounts unchanged).</summary>
public sealed record AdminFinanceOpenPayoutPreview(
    Guid InvoiceId,
    string InvoiceNumber,
    string MaskedPayeeName,
    string RoleLabel,
    decimal TotalInclVat,
    string MaskedIban);

public interface IAdminFinanceSummaryService
{
    Task<AdminFinanceSummary> GetAsync(string period, CancellationToken cancellationToken = default);
}
