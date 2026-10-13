using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Sales;

public interface ISalesPayoutRequestService
{
    Task<SalesPayoutPreviewDto> PreviewAsync(
        Guid beneficiaryUserId,
        bool mfaSatisfied,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a payout request for the full available amount and links every available ledger line.
    /// Idempotent: returns the existing open request on double-submit.
    /// </summary>
    Task<SalesPayoutRequestDto> RequestAsync(
        Guid beneficiaryUserId,
        bool mfaSatisfied,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid beneficiaryUserId,
        Guid requestId,
        CancellationToken cancellationToken = default);
}

public interface ISalesWalletPortalService
{
    Task<SalesWalletOverviewDto> GetOverviewAsync(
        Guid beneficiaryUserId,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);

    Task<SalesWalletEntriesPageDto> ListEntriesAsync(
        Guid beneficiaryUserId,
        int? periodYear,
        string? kind,
        string? state,
        int page = 1,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesPayoutListItemDto>> ListPayoutsAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesInvoiceListItemDto>> ListInvoicesAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default);

    Task<byte[]> RenderJaaroverzichtPdfAsync(
        Guid beneficiaryUserId,
        int year,
        CancellationToken cancellationToken = default);
}

public sealed record SalesPayoutBlockerDto(
    string Code,
    string MessageKey,
    string? Href,
    string? MessageArg = null);

public sealed class SalesInvoicePreviewDto
{
    public string InvoiceNumberPlaceholder { get; init; } = "wordt toegekend bij goedkeuring";
    public string SupplierCompanyName { get; init; } = "";
    public string SupplierKvk { get; init; } = "";
    public string? SupplierVat { get; init; }
    public string SupplierAddress { get; init; } = "";
    public string CustomerName { get; init; } = "Lobsy B.V.";
    public DateOnly? ConsentDate { get; init; }
    public string? ConsentVersion { get; init; }
    public int LineCount { get; init; }
    public string PeriodLabel { get; init; } = "";
    public decimal SubtotalExVat { get; init; }
    public decimal VatAmount { get; init; }
    public decimal TotalInclVat { get; init; }
    public string VatTreatment { get; init; } = nameof(SalesManagerVatTreatment.Standard21);
    public bool IsKor { get; init; }
}

public sealed class SalesPayoutPreviewDto
{
    public decimal AvailableExVat { get; init; }
    public decimal AmountExVat { get; init; }
    public decimal VatAmount { get; init; }
    public decimal TotalInclVat { get; init; }
    public string VatTreatment { get; init; } = nameof(SalesManagerVatTreatment.Standard21);
    public bool IsKor { get; init; }
    public string MaskedIban { get; init; } = "—";
    public string? HolderName { get; init; }
    public DateOnly ExpectedRunDate { get; init; }
    public DateOnly? IbanHoldUntil { get; init; }
    public decimal PayoutMinimumEuro { get; init; } = 50m;
    public int CommissionHoldDays { get; init; } = 14;
    public bool CanRequest { get; init; }
    public IReadOnlyList<SalesPayoutBlockerDto> Blockers { get; init; } = [];
    public SalesInvoicePreviewDto? InvoicePreview { get; init; }
    public Guid? OpenRequestId { get; init; }
}

public sealed class SalesPayoutRequestDto
{
    public Guid Id { get; init; }
    public decimal AmountExVat { get; init; }
    public decimal VatAmount { get; init; }
    public decimal TotalInclVat { get; init; }
    public string VatTreatment { get; init; } = "";
    public string MaskedIban { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime RequestedAtUtc { get; init; }
    public Guid? SelfBillingInvoiceId { get; init; }
    public string? InvoiceNumber { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? DecidedAtUtc { get; init; }
    public DateTime? PaidAtUtc { get; init; }
}

public sealed class SalesWalletOverviewDto
{
    public decimal Available { get; init; }
    public decimal Pending { get; init; }
    public decimal Requested { get; init; }
    public decimal PaidThisYear { get; init; }
    public int InvoiceCountThisYear { get; init; }
    public int LocalYear { get; init; }
    public DateOnly NextRunDate { get; init; }
    public decimal PayoutMinimumEuro { get; init; }
    public int CommissionHoldDays { get; init; }
    public string VatTreatment { get; init; } = "";
    public bool IsKor { get; init; }
    public decimal VatOnAvailable { get; init; }
    public string? MaskedIban { get; init; }
    public string? HolderName { get; init; }
    public bool CanRequestPayout { get; init; }
    public IReadOnlyList<SalesPayoutBlockerDto> Blockers { get; init; } = [];
    public IReadOnlyList<SalesInvoiceListItemDto> LatestInvoices { get; init; } = [];
    public Guid? OpenRequestId { get; init; }
}

public sealed class SalesWalletEntryDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public string Kind { get; init; } = "";
    public string KindLabelKey { get; init; } = "";
    public string Title { get; init; } = "";
    public string? SubLine { get; init; }
    public string State { get; init; } = "";
    public string StateLabelKey { get; init; } = "";
    public DateOnly? AvailableOn { get; init; }
    public decimal AmountExVat { get; init; }
}

public sealed class SalesWalletEntriesPageDto
{
    public IReadOnlyList<SalesWalletEntryDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; } = 25;
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class SalesPayoutListItemDto
{
    public Guid Id { get; init; }
    public bool IsLegacyCheckout { get; init; }
    public decimal AmountExVat { get; init; }
    public decimal TotalInclVat { get; init; }
    public string MaskedIban { get; init; } = "";
    public string Status { get; init; } = "";
    public string StatusLabelKey { get; init; } = "";
    public DateTime RequestedAtUtc { get; init; }
    public Guid? InvoiceId { get; init; }
    public string? InvoiceNumber { get; init; }
    public string? RejectionReason { get; init; }
    public IReadOnlyList<string> StepperSteps { get; init; } = [];
    public int ActiveStepIndex { get; init; }
}

public sealed class SalesInvoiceListItemDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public DateTime CreatedAtUtc { get; init; }
    public decimal TotalInclVat { get; init; }
    public decimal SubtotalExVat { get; init; }
    public string Status { get; init; } = "";
    public string StatusLabelKey { get; init; } = "";
}
