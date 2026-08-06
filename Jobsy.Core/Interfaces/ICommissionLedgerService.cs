using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ICommissionLedgerService
{
    Task<decimal> GetBalanceExVatAsync(Guid salesManagerUserId, CancellationToken cancellationToken = default);

    Task<decimal> GetUninvoicedBalanceExVatAsync(Guid salesManagerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommissionLedgerEntry>> ListEntriesAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Credits founder bonus after €2500 onboarding is Credited. Idempotent on paymentId.</summary>
    Task<CommissionLedgerEntry?> TryCreditFounderBonusAsync(
        Guid salesManagerUserId,
        Guid companyId,
        string paymentId,
        int? firstYearSlot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Credits direct token commission after token checkout Credited.
    /// Idempotent on (checkout id, salesmanager, kind).
    /// </summary>
    Task<CommissionLedgerEntry?> TryCreditTokenCommissionAsync(
        Guid salesManagerUserId,
        Guid companyId,
        Guid tokenCheckoutId,
        decimal purchaseAmountEuro,
        DateTime? firstYearStartedAt,
        decimal? directRate = null,
        int? durationDays = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Credits passive indirect commission to the referring salesmanager.
    /// Idempotent on (checkout id, salesmanager, kind).
    /// </summary>
    Task<CommissionLedgerEntry?> TryCreditIndirectTokenCommissionAsync(
        Guid referringSalesManagerUserId,
        Guid companyId,
        Guid tokenCheckoutId,
        decimal purchaseAmountEuro,
        DateTime? firstYearStartedAt,
        decimal? indirectRate = null,
        int? durationDays = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Credits Ambassadeur token commission (ledger beneficiary = Ambassadeur user id).
    /// Idempotent on (checkout id, ambassadeur, TokenCommission).
    /// </summary>
    Task<CommissionLedgerEntry?> TryCreditAmbassadeurTokenCommissionAsync(
        Guid ambassadeurUserId,
        Guid companyId,
        Guid tokenCheckoutId,
        decimal purchaseAmountEuro,
        DateTime? firstYearStartedAt,
        decimal rate,
        int? durationDays = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Credits Bedrijfsmanager/Intermediair partner commission on token purchases.
    /// Idempotent on (checkout id, partner user, TokenCommission).
    /// </summary>
    Task<CommissionLedgerEntry?> TryCreditPartnerTokenCommissionAsync(
        Guid partnerUserId,
        Guid companyId,
        Guid tokenCheckoutId,
        decimal purchaseAmountEuro,
        decimal rate,
        CancellationToken cancellationToken = default);

    Task AttachEntriesToInvoiceAsync(
        Guid invoiceId,
        IReadOnlyList<Guid> entryIds,
        CancellationToken cancellationToken = default);

    Task<CommissionLedgerEntry> RecordPayoutAsync(
        Guid salesManagerUserId,
        Guid invoiceId,
        decimal amountExVat,
        decimal vatAmount,
        CancellationToken cancellationToken = default);
}
