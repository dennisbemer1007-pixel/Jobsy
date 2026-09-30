using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Separate self-billing consent (D5), distinct from the partner agreement.</summary>
public class SalesSelfBillingConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
    public string TextSha256 { get; set; } = string.Empty;
    public DateTime AcceptedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

public class SalesPayoutRequest
{
    public Guid Id { get; set; }

    /// <summary>beneficiary: SalesManager (or a parked Ambassadeur)</summary>
    public Guid BeneficiaryUserId { get; set; }
    public User BeneficiaryUser { get; set; } = null!;

    public decimal AmountExVat { get; set; }
    public SalesManagerVatTreatment VatTreatment { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public string MaskedIban { get; set; } = string.Empty;
    public SalesPayoutRequestStatus Status { get; set; } = SalesPayoutRequestStatus.Requested;
    public DateTime RequestedAtUtc { get; set; }
    public Guid? SalesPayoutRunId { get; set; }
    public SalesPayoutRun? SalesPayoutRun { get; set; }
    public Guid? SelfBillingInvoiceId { get; set; }
    public SelfBillingInvoice? SelfBillingInvoice { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}

public class SalesPayoutRun
{
    public Guid Id { get; set; }
    public DateOnly RunDate { get; set; }
    public bool IsExtra { get; set; }
    public SalesPayoutRunStatus Status { get; set; } = SalesPayoutRunStatus.Draft;
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ExportedAtUtc { get; set; }
    public string? ExportFileSha256 { get; set; }
    public string ProviderKey { get; set; } = "bank-transfer";

    public ICollection<SalesPayoutRequest> Requests { get; set; } = new List<SalesPayoutRequest>();
}

public class SalesAttributionChange
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid? FromUserId { get; set; }
    public Guid? ToUserId { get; set; }
    public SalesAttributionSource Source { get; set; } = SalesAttributionSource.Admin;
    public string Reason { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}

public class SalesLinkClickDaily
{
    public Guid BeneficiaryUserId { get; set; }
    public DateOnly Date { get; set; }
    public SalesLinkChannel Channel { get; set; }
    public int Count { get; set; }
}
