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

    /// <summary>Credits token commission after token checkout Credited. Idempotent on checkout id.</summary>
    Task<CommissionLedgerEntry?> TryCreditTokenCommissionAsync(
        Guid salesManagerUserId,
        Guid companyId,
        Guid tokenCheckoutId,
        decimal purchaseAmountEuro,
        DateTime? firstYearStartedAt,
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
