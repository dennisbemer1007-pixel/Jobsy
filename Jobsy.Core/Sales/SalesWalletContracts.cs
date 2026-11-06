using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Sales;

public sealed record SalesWalletBalances(
    decimal Available,
    decimal Pending,
    decimal Requested,
    decimal PaidThisYear,
    decimal EarnedThisYear);

public interface ISalesWalletReadService
{
    Task<decimal> GetAvailableAsync(Guid beneficiaryUserId, CancellationToken cancellationToken = default);

    Task<SalesWalletBalances> GetBalancesAsync(
        Guid beneficiaryUserId,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);

    CommissionEntryState DeriveState(
        CommissionLedgerEntry entry,
        DateTime utcNow,
        SelfBillingInvoiceStatus? invoiceStatus,
        SalesPayoutRequestStatus? requestStatus);
}

public sealed record SalesEmployerUnitDto(
    Guid RootCompanyId,
    string DisplayName,
    string? Place,
    DateTime? AttributedAtUtc,
    DateTime? CommissionStartsAtUtc,
    int BranchCount,
    decimal OwnCommissionExVat);

public interface ISalesEmployerReadService
{
    /// <summary>One row per organisation root attributed to the beneficiary.</summary>
    Task<IReadOnlyList<SalesEmployerUnitDto>> ListUnitsAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>Shared place extraction for privacy-safe employer DTOs (D4).</summary>
public static class SalesPlace
{
    public static string? FromAddress(string? address, CompanyLegalForm? legalForm)
    {
        if (legalForm is null or CompanyLegalForm.Eenmanszaak)
        {
            return null;
        }

        return Jobsy.Core.Contracts.LobsyCvModelFactory.ExtractCity(address);
    }
}

public interface ISalesCorrectionService
{
    /// <summary>
    /// Books refund/chargeback corrections for every beneficiary line of a checkout.
    /// Idempotent via SourceRefundKey; only books the delta vs already booked corrections.
    /// </summary>
    Task ApplyPaymentReversalsAsync(
        Guid checkoutId,
        decimal amountPaidEuro,
        decimal amountRefundedEuro,
        decimal amountChargedBackEuro,
        CancellationToken cancellationToken = default);

    /// <summary>Manual admin Adjustment line (±). Caller enforces admin + MFA.</summary>
    Task<CommissionLedgerEntry> BookManualCorrectionAsync(
        Guid beneficiaryUserId,
        Guid? companyId,
        decimal amountExVat,
        string reason,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}
