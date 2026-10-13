using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Sales;

/// <summary>
/// Admin monthly payout run: create, approve/reject lines, export, mark paid.
/// Money actions require an MFA-verified session at the controller.
/// </summary>
public interface ISalesPayoutRunService
{
    /// <summary>
    /// Creates the scheduled monthly run for <paramref name="runDate"/> when missing,
    /// moving Requested requests (created before now, non-parked beneficiaries) into it.
    /// Idempotent via the unique filtered index on RunDate where IsExtra = false.
    /// </summary>
    Task<SalesPayoutRun?> TryCreateScheduledRunAsync(
        DateOnly runDate,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<SalesPayoutRunDto> CreateExtraRunAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task RejectLineAsync(
        Guid adminUserId,
        Guid runId,
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<SalesPayoutRunDto> ApproveRunAsync(
        Guid adminUserId,
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<SalesPayoutExportResult> ExportAsync(
        Guid adminUserId,
        Guid runId,
        string format,
        CancellationToken cancellationToken = default);

    Task<SalesPayoutRunDto> MarkPaidAsync(
        Guid adminUserId,
        Guid runId,
        IReadOnlyList<Guid>? invoiceIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesPayoutRunListItemDto>> ListRunsAsync(
        CancellationToken cancellationToken = default);

    Task<SalesPayoutRunDetailDto> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called from <see cref="ISelfBillingInvoiceService.MarkPaidAsync"/> so every mark-paid
    /// path (old page, admin redesign 06.4, run bulk) closes the matching payout request.
    /// </summary>
    Task CloseRequestForPaidInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Payout provider seam (bank transfer now; Mollie later).
/// // Mollie payouts: implement ISalesPayoutProvider when available
/// </summary>
public interface ISalesPayoutProvider
{
    string Key { get; }
    bool SupportsAutomaticPayout { get; }
    bool IsConfigured { get; }
    string? NotConfiguredMessage { get; }

    Task<SalesPayoutExportFile> ExportAsync(
        SalesPayoutRun run,
        IReadOnlyList<SalesPayoutExportLine> lines,
        string format,
        CancellationToken cancellationToken = default);
}

public sealed record SalesPayoutExportLine(
    Guid RequestId,
    Guid InvoiceId,
    string InvoiceNumber,
    string CreditorName,
    string CreditorIban,
    string MaskedIban,
    decimal AmountInclVat,
    decimal AmountExVat);

public sealed record SalesPayoutExportFile(
    string FileName,
    string ContentType,
    byte[] Bytes);

public sealed record SalesPayoutExportResult(
    string FileName,
    string ContentType,
    byte[] Bytes,
    string Sha256Hex,
    bool AccessLogged);

public sealed record SalesPayoutRunListItemDto(
    Guid Id,
    DateOnly RunDate,
    bool IsExtra,
    string Status,
    int LineCount,
    decimal TotalInclVat,
    string? ApprovedByMaskedName,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? ExportedAtUtc,
    DateTime? PaidClosedAtUtc);

public sealed record SalesPayoutRunDetailDto(
    Guid Id,
    DateOnly RunDate,
    bool IsExtra,
    string Status,
    string ProviderKey,
    string? ExportFileSha256,
    bool ExportConfigured,
    string? ExportDisabledReason,
    string? ApprovedByMaskedName,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? ExportedAtUtc,
    IReadOnlyList<SalesPayoutRunLineDto> Lines,
    string NextStepKey);

public sealed record SalesPayoutRunLineDto(
    Guid RequestId,
    Guid BeneficiaryUserId,
    string BeneficiaryMaskedName,
    string Role,
    decimal AmountExVat,
    decimal VatAmount,
    decimal TotalInclVat,
    string VatTreatment,
    string MaskedIban,
    string Status,
    Guid? InvoiceId,
    string? InvoiceNumber,
    bool FlagNewIban,
    bool FlagNoConsent,
    bool FlagBalanceTooLow,
    bool FlagIncompleteProfile,
    string? FlagNote,
    string? RejectionReason);

public sealed record SalesPayoutRunDto(
    Guid Id,
    DateOnly RunDate,
    bool IsExtra,
    string Status,
    int LineCount,
    decimal TotalInclVat);

public sealed class SalesPayoutProviderOptions
{
    public const string SectionName = "Sales:Payout";

    public string Provider { get; set; } = "bank-transfer";
    public string? DebtorName { get; set; }
    public string? DebtorIban { get; set; }
    public string? DebtorBic { get; set; }
}
