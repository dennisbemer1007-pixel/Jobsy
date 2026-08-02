namespace Jobsy.Core.Interfaces;

public interface ISalesManagerDashboardService
{
    Task<SalesManagerDashboardDto?> GetDashboardAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesManagerListItemDto>> ListSalesManagersAsync(
        CancellationToken cancellationToken = default);
}

public sealed record SalesManagerDashboardDto(
    Guid UserId,
    string Email,
    string FullName,
    string? TrackingCode,
    bool IsOnboardingComplete,
    decimal BalanceExVat,
    decimal BalanceInclVat,
    decimal UninvoicedExVat,
    decimal OutstandingIssuedExVat,
    IReadOnlyList<ReferredSupplierDto> Suppliers,
    IReadOnlyList<CommissionEntryDto> RecentLedger,
    IReadOnlyList<SelfBillingInvoiceDto> Invoices,
    bool CanRecruitSalesManagers = true,
    Guid? ReferredBySalesManagerUserId = null);

public sealed record ReferredSupplierDto(
    Guid CompanyId,
    string Name,
    string KvkNumber,
    int? FirstYearSupplierSlot,
    DateTime? FirstYearStartedAt,
    bool HasPaidOnboarding);

public sealed record CommissionEntryDto(
    Guid Id,
    string Kind,
    decimal AmountExVat,
    decimal VatAmount,
    string? Note,
    Guid? CompanyId,
    string? CompanyName,
    DateTime CreatedAt,
    Guid? InvoiceId);

public sealed record SelfBillingInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    decimal SubtotalExVat,
    decimal VatAmount,
    decimal TotalInclVat,
    string Status,
    DateTime CreatedAt,
    DateTime? IssuedAt,
    DateTime? PaidAt);

public sealed record SalesManagerListItemDto(
    Guid UserId,
    string Email,
    string FullName,
    string? TrackingCode,
    bool IsOnboardingComplete,
    decimal BalanceExVat,
    int SupplierCount,
    bool CanRecruitSalesManagers = true,
    Guid? ReferredBySalesManagerUserId = null);
