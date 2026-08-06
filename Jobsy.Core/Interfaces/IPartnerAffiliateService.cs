using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IPartnerAffiliateService
{
    Task<PartnerAffiliateProfile> EnsureProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PartnerAffiliateProfile?> ResolveByTrackingCodeAsync(
        string? trackingCode,
        CancellationToken cancellationToken = default);

    Task<bool> ApplyReferralAsync(
        Company company,
        string? trackingCode,
        CancellationToken cancellationToken = default);

    Task<CommissionLedgerEntry?> TryCreditTokenCommissionAsync(
        Guid partnerUserId,
        Guid companyId,
        Guid tokenCheckoutId,
        decimal purchaseAmountExVatEuro,
        CancellationToken cancellationToken = default);

    Task<PartnerAffiliateMeDto?> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartnerAffiliateTokenLogRowDto>> GetTokenLogAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PartnerAffiliateToolkitDto?> GetToolkitAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PartnerAffiliateBillingDto?> GetBillingAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PartnerAffiliateBillingDto> UpdateBillingAsync(
        Guid userId,
        PartnerAffiliateBillingUpdate update,
        CancellationToken cancellationToken = default);
}

public sealed record PartnerAffiliateMeDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string TrackingCode,
    decimal CommissionRate,
    decimal BalanceExVat,
    decimal BalanceInclVat,
    int ReferredCompanyCount,
    IReadOnlyList<PartnerAffiliateLedgerSummaryDto> RecentLedger);

public sealed record PartnerAffiliateLedgerSummaryDto(
    Guid Id,
    string Kind,
    decimal AmountExVat,
    decimal VatAmount,
    string? Note,
    Guid? CompanyId,
    string? CompanyName,
    DateTime CreatedAt,
    Guid? InvoiceId);

public sealed record PartnerAffiliateTokenLogRowDto(
    Guid LedgerEntryId,
    Guid? CompanyId,
    string? CompanyName,
    DateTime DateUtc,
    int TokensBought,
    decimal EarningsExVat,
    decimal PayoutExVat,
    string Kind,
    string? Note);

public sealed record PartnerAffiliateToolkitDto(
    string TrackingCode,
    decimal CommissionRate,
    string PartnerPageUrl,
    string RegisterUrl,
    string FlyerUrl);

public sealed record PartnerAffiliateBillingDto(
    string? CompanyName,
    string? KvkNumber,
    string? VatNumber,
    string? Address,
    string? PostalCode,
    string? City,
    string Country,
    string MaskedIban,
    bool HasIban);

public sealed record PartnerAffiliateBillingUpdate(
    string? CompanyName,
    string? KvkNumber,
    string? VatNumber,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? Iban,
    bool ClearIban = false);
