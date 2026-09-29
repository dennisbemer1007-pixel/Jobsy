namespace Jobsy.Core.Admin;

/// <summary>Period finance roll-up for the admin dashboard KPI (and file 06).</summary>
public sealed record AdminFinanceSummary(
    string Period,
    int RevenueInclVatCents,
    int RevenueExVatCents,
    int PreviousRevenueInclVatCents,
    int TokensSold,
    int OpenAtMollieCents,
    int VatBufferPendingCents,
    int OpenPayoutsCents,
    int OpenPayoutsCount);

public interface IAdminFinanceSummaryService
{
    Task<AdminFinanceSummary> GetAsync(string period, CancellationToken cancellationToken = default);
}
