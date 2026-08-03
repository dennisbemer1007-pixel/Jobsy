namespace Jobsy.Core.Interfaces;

public interface IAmbassadeurDashboardService
{
    Task<AmbassadeurDashboardDto?> GetDashboardAsync(
        Guid ambassadeurUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AmbassadeurListItemDto>> ListAmbassadeursAsync(
        CancellationToken cancellationToken = default);
}

public sealed record AmbassadeurDashboardDto(
    Guid UserId,
    string Email,
    string FullName,
    string? TrackingCode,
    bool IsOnboardingComplete,
    int RegisteredCandidates,
    int CandidateApplications,
    decimal BaseCommissionPercentage,
    decimal CurrentCommissionPercentage,
    decimal MaxCommissionPercentage,
    decimal? CommissionPercentageOverride,
    int CandidateThreshold,
    decimal PercentPerThreshold,
    int CandidatesUntilNextTier,
    decimal BalanceExVat,
    decimal BalanceInclVat,
    decimal UninvoicedExVat,
    decimal OutstandingIssuedExVat,
    IReadOnlyList<ReferredCandidateDto> RecentCandidates,
    IReadOnlyList<ReferredSupplierDto> Suppliers,
    IReadOnlyList<CommissionEntryDto> RecentLedger,
    IReadOnlyList<SelfBillingInvoiceDto> Invoices);

public sealed record ReferredCandidateDto(
    Guid UserId,
    string FullName,
    DateTime? RegisteredAt,
    int ApplicationCount);

public sealed record AmbassadeurListItemDto(
    Guid UserId,
    string Email,
    string FullName,
    string? TrackingCode,
    bool IsOnboardingComplete,
    int RegisteredCandidates,
    decimal CurrentCommissionPercentage,
    decimal BalanceExVat);
