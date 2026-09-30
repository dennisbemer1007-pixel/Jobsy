using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Sales;

/// <summary>D12: no raw enum names in the UI — keys into UiStringsSales.</summary>
public static class SalesLabels
{
    public static string Key(CommissionEntryKind kind) => kind switch
    {
        CommissionEntryKind.TokenCommission => "Sales.Label.Kind.TokenCommission",
        CommissionEntryKind.FounderBonus => "Sales.Label.Kind.FounderBonus",
        CommissionEntryKind.Payout => "Sales.Label.Kind.Payout",
        CommissionEntryKind.Adjustment => "Sales.Label.Kind.Adjustment",
        CommissionEntryKind.IndirectTokenCommission => "Sales.Label.Kind.IndirectTokenCommission",
        CommissionEntryKind.RefundCorrection => "Sales.Label.Kind.RefundCorrection",
        CommissionEntryKind.ChargebackCorrection => "Sales.Label.Kind.ChargebackCorrection",
        _ => "Sales.Label.Kind.TokenCommission"
    };

    public static string Key(SalesPayoutRequestStatus status) => status switch
    {
        SalesPayoutRequestStatus.Requested => "Sales.Label.PayoutRequest.Requested",
        SalesPayoutRequestStatus.InRun => "Sales.Label.PayoutRequest.InRun",
        SalesPayoutRequestStatus.Approved => "Sales.Label.PayoutRequest.Approved",
        SalesPayoutRequestStatus.Rejected => "Sales.Label.PayoutRequest.Rejected",
        SalesPayoutRequestStatus.Paid => "Sales.Label.PayoutRequest.Paid",
        SalesPayoutRequestStatus.Cancelled => "Sales.Label.PayoutRequest.Cancelled",
        _ => "Sales.Label.PayoutRequest.Requested"
    };

    public static string Key(SalesPayoutRunStatus status) => status switch
    {
        SalesPayoutRunStatus.Draft => "Sales.Label.PayoutRun.Draft",
        SalesPayoutRunStatus.Approved => "Sales.Label.PayoutRun.Approved",
        SalesPayoutRunStatus.Exported => "Sales.Label.PayoutRun.Exported",
        SalesPayoutRunStatus.Paid => "Sales.Label.PayoutRun.Paid",
        SalesPayoutRunStatus.Closed => "Sales.Label.PayoutRun.Closed",
        _ => "Sales.Label.PayoutRun.Draft"
    };

    public static string Key(SelfBillingInvoiceStatus status) => status switch
    {
        SelfBillingInvoiceStatus.Draft => "Sales.Label.Invoice.Draft",
        SelfBillingInvoiceStatus.Issued => "Sales.Label.Invoice.Issued",
        SelfBillingInvoiceStatus.Paid => "Sales.Label.Invoice.Paid",
        SelfBillingInvoiceStatus.Cancelled => "Sales.Label.Invoice.Cancelled",
        _ => "Sales.Label.Invoice.Draft"
    };

    public static string Key(SalesManagerVatTreatment treatment) => treatment switch
    {
        SalesManagerVatTreatment.Standard21 => "Sales.Label.Vat.Standard21",
        SalesManagerVatTreatment.SmallBusinessScheme => "Sales.Label.Vat.KOR",
        SalesManagerVatTreatment.ReverseCharge => "Sales.Label.Vat.ReverseCharge",
        SalesManagerVatTreatment.Exempt => "Sales.Label.Vat.Exempt",
        _ => "Sales.Label.Vat.Standard21"
    };

    public static string Key(SalesAttributionSource source) => source switch
    {
        SalesAttributionSource.TypedCode => "Sales.Label.Attribution.TypedCode",
        SalesAttributionSource.LinkCookie => "Sales.Label.Attribution.LinkCookie",
        SalesAttributionSource.Admin => "Sales.Label.Attribution.Admin",
        SalesAttributionSource.Legacy => "Sales.Label.Attribution.Legacy",
        _ => "Sales.Label.Attribution.Legacy"
    };

    public static string Key(SalesManagerApplicationStatus status) => status switch
    {
        SalesManagerApplicationStatus.Pending => "Sales.Label.Application.Pending",
        SalesManagerApplicationStatus.Approved => "Sales.Label.Application.Approved",
        SalesManagerApplicationStatus.Rejected => "Sales.Label.Application.Rejected",
        SalesManagerApplicationStatus.Expired => "Sales.Label.Application.Expired",
        _ => "Sales.Label.Application.Pending"
    };

    /// <summary>Portal/admin status label; objections use Rejected + SubjectObjectedAtUtc.</summary>
    public static string ApplicationStatusKey(
        SalesManagerApplicationStatus status,
        DateTime? subjectObjectedAtUtc)
        => subjectObjectedAtUtc is not null
            ? "Sales.Label.Application.Objection"
            : Key(status);

    public static string Key(CommissionEntryState state) => state switch
    {
        CommissionEntryState.Pending => "Sales.Label.State.Pending",
        CommissionEntryState.Available => "Sales.Label.State.Available",
        CommissionEntryState.Requested => "Sales.Label.State.Requested",
        CommissionEntryState.Paid => "Sales.Label.State.Paid",
        CommissionEntryState.Settled => "Sales.Label.State.Settled",
        _ => "Sales.Label.State.Pending"
    };

    public static string Key(Contracts.Sales.SalesEmployerStatus status) => status switch
    {
        Contracts.Sales.SalesEmployerStatus.NoPurchase => "Sales.Label.EmployerStatus.NoPurchase",
        Contracts.Sales.SalesEmployerStatus.Active => "Sales.Label.EmployerStatus.Active",
        Contracts.Sales.SalesEmployerStatus.Quiet => "Sales.Label.EmployerStatus.Quiet",
        Contracts.Sales.SalesEmployerStatus.Ended => "Sales.Label.EmployerStatus.Ended",
        _ => "Sales.Label.EmployerStatus.NoPurchase"
    };

    public static string CommissionStateKey(string state) => state switch
    {
        "Pending" => "Sales.Label.State.Pending",
        "Available" => "Sales.Label.State.Available",
        "Requested" => "Sales.Label.State.Requested",
        "Paid" => "Sales.Label.State.Paid",
        "Settled" => "Sales.Label.State.Settled",
        _ => "Sales.Label.State.Pending"
    };

    public static IEnumerable<Type> LabeledEnumTypes() =>
    [
        typeof(CommissionEntryKind),
        typeof(CommissionEntryState),
        typeof(SalesPayoutRequestStatus),
        typeof(SalesPayoutRunStatus),
        typeof(SelfBillingInvoiceStatus),
        typeof(SalesManagerVatTreatment),
        typeof(SalesAttributionSource),
        typeof(SalesManagerApplicationStatus),
        typeof(Contracts.Sales.SalesEmployerStatus)
    ];
}
