using Jobsy.Core.Enums;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

public sealed class SalesManagerInviteResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TemporaryPassword { get; set; }
    public bool CreatedNewUser { get; set; }
}

public sealed class SalesManagerListItem
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public bool IsOnboardingComplete { get; set; }
    public decimal BalanceExVat { get; set; }
    public int SupplierCount { get; set; }
    public bool CanRecruitSalesManagers { get; set; } = true;
    public Guid? ReferredBySalesManagerUserId { get; set; }
}

public sealed class SalesManagerDashboard
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public bool IsOnboardingComplete { get; set; }
    public decimal BalanceExVat { get; set; }
    public decimal BalanceInclVat { get; set; }
    public decimal UninvoicedExVat { get; set; }
    public decimal OutstandingIssuedExVat { get; set; }
    public List<ReferredSupplierItem> Suppliers { get; set; } = [];
    public List<CommissionEntryItem> RecentLedger { get; set; } = [];
    public List<SelfBillingInvoiceItem> Invoices { get; set; } = [];
    public bool CanRecruitSalesManagers { get; set; } = true;
    public Guid? ReferredBySalesManagerUserId { get; set; }
}

public sealed class AmbassadeurInviteResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool CreatedNew { get; set; }
}

public sealed class AmbassadeurListItem
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public bool IsOnboardingComplete { get; set; }
    public int RegisteredCandidates { get; set; }
    public decimal CurrentCommissionPercentage { get; set; }
    public decimal BalanceExVat { get; set; }
}

public sealed class AmbassadeurSettingsDto
{
    public int CandidateThreshold { get; set; }
    public decimal PercentPerThreshold { get; set; }
    public decimal MaxCommissionPercentage { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class AmbassadeurDashboard
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TrackingCode { get; set; }
    public bool IsOnboardingComplete { get; set; }
    public int RegisteredCandidates { get; set; }
    public int CandidateApplications { get; set; }
    public decimal BaseCommissionPercentage { get; set; }
    public decimal CurrentCommissionPercentage { get; set; }
    public decimal MaxCommissionPercentage { get; set; }
    public decimal? CommissionPercentageOverride { get; set; }
    public int CandidateThreshold { get; set; }
    public decimal PercentPerThreshold { get; set; }
    public int CandidatesUntilNextTier { get; set; }
    public decimal BalanceExVat { get; set; }
    public decimal BalanceInclVat { get; set; }
    public decimal UninvoicedExVat { get; set; }
    public decimal OutstandingIssuedExVat { get; set; }
    public List<ReferredCandidateItem> RecentCandidates { get; set; } = [];
    public List<ReferredSupplierItem> Suppliers { get; set; } = [];
    public List<CommissionEntryItem> RecentLedger { get; set; } = [];
    public List<SelfBillingInvoiceItem> Invoices { get; set; } = [];
}

public sealed class ReferredCandidateItem
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime? RegisteredAt { get; set; }
    public int ApplicationCount { get; set; }
}

public sealed class AmbassadeurProfile
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Iban { get; set; }
    public string? TrackingCode { get; set; }
    public decimal BaseCommissionPercentage { get; set; }
    public decimal CurrentCommissionPercentage { get; set; }
    public decimal MaxCommissionPercentage { get; set; }
    public decimal? CommissionPercentageOverride { get; set; }
    public DateTime? AgreementSignedAt { get; set; }
    public string? AgreementVersion { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }
    public bool IsOnboardingComplete { get; set; }
}

public sealed class AmbassadeurProfileForm
{
    public string CompanyName { get; set; } = "";
    public string KvkNumber { get; set; } = "";
    public string VatNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "NL";
    public string? Iban { get; set; }
}

public sealed class SalesManagerApplicationItem
{
    public Guid Id { get; set; }
    public Guid ReferrerSalesManagerUserId { get; set; }
    public string ReferrerFullName { get; set; } = string.Empty;
    public string ReferrerEmail { get; set; } = string.Empty;
    public string ReferrerTrackingCode { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string CandidateFullName { get; set; } = string.Empty;
    public string Motivation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ProvisionedUserId { get; set; }
    public string? RejectionReason { get; set; }
    public string? TemporaryPassword { get; set; }
}

public sealed class ReferredSupplierItem
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;
    public int? FirstYearSupplierSlot { get; set; }
    public DateTime? FirstYearStartedAt { get; set; }
    public bool HasPaidOnboarding { get; set; }
}

public sealed class CommissionEntryItem
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public decimal AmountExVat { get; set; }
    public decimal VatAmount { get; set; }
    public string? Note { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? InvoiceId { get; set; }
}

public sealed class SelfBillingInvoiceItem
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal SubtotalExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public sealed class SalesManagerPayoutPreview
{
    public decimal AvailableExVat { get; set; }
    public decimal AmountExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal AmountInclVat { get; set; }
    public string? Iban { get; set; }
    public string MaskedIban { get; set; } = "—";
    public bool CanPayout { get; set; }
    public string? BlockReason { get; set; }
}

public sealed class SalesManagerPayoutCheckoutResult
{
    public string PaymentId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public decimal AmountEuro { get; set; }
    public string MaskedIban { get; set; } = string.Empty;
    public bool IsStub { get; set; }
}

public sealed class SalesManagerPayoutCompleteResult
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalInclVat { get; set; }
    public string MaskedIban { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class SalesManagerProfile
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Iban { get; set; }
    public string? TrackingCode { get; set; }
    public DateTime? AgreementSignedAt { get; set; }
    public string? AgreementVersion { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }
    public bool IsOnboardingComplete { get; set; }
}

public sealed class SalesManagerProfileForm
{
    public string CompanyName { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Country { get; set; } = "NL";
    public string? Iban { get; set; }
}

public sealed class OnboardingCheckoutResult
{
    public string PaymentId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public decimal AmountEuro { get; set; }
    public bool IsStub { get; set; }
}

public sealed class OnboardingCompleteResult
{
    public Guid CompanyId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool CommissionCredited { get; set; }
    public int? FirstYearSupplierSlot { get; set; }
}

public sealed class SalesManagerCostFinanceItem
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SalesManagerUserId { get; set; }
    public string SalesManagerCompanyName { get; set; } = string.Empty;
    public decimal SubtotalExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public string VatTreatment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? VatDeclarationStatusLabel { get; set; }
}

public sealed class PartnerSalesCatalog
{
    public decimal BaseTokenValueEuro { get; set; }
    public decimal HighlightCarouselTokens { get; set; }
    public decimal HighlightPulseTokens { get; set; }
    public int HighlightCarouselDays { get; set; }
    public decimal StartHighlightBonusTokens { get; set; }
    public List<VacancyTypeCostItem> VacancyTypeCosts { get; set; } = [];
    public List<SalesPackageItem> Packages { get; set; } = [];
}

public sealed class SalesCommercialAdminModel
{
    public Guid SettingsId { get; set; }
    public decimal BaseTokenValueEuro { get; set; }
    public decimal HighlightCarouselTokens { get; set; }
    public decimal HighlightPulseTokens { get; set; }
    public int HighlightCarouselDays { get; set; }
    public decimal StartHighlightBonusTokens { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<VacancyTypeCostItem> VacancyTypeCosts { get; set; } = [];
    public List<SalesPackageItem> Packages { get; set; } = [];
    public decimal DirectCommissionRate { get; set; } = 0.25m;
    public decimal IndirectCommissionRate { get; set; } = 0.05m;
    public int CommissionDurationDays { get; set; } = 1095;
    public decimal PartnerCommissionRate { get; set; } = 0.05m;
    public decimal Year2DirectCommissionRate { get; set; } = 0.10m;
    public decimal Year3DirectCommissionRate { get; set; } = 0.05m;
    public decimal ReferredYear1DirectCommissionRate { get; set; } = 0.20m;
    public int CommissionHoldDays { get; set; } = 14;
    public decimal PayoutMinimumEuro { get; set; } = 50m;
    public int IbanChangeHoldDays { get; set; } = 3;
    public int AttributionCookieDays { get; set; } = 30;
}

public sealed class PartnerAffiliateMeModel
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TrackingCode { get; set; } = string.Empty;
    public decimal ReferralTokensEarned { get; set; }
    public int ReferredCompanyCount { get; set; }
    public int PendingReferralCount { get; set; }
    public int RewardedReferralCount { get; set; }
    public List<PartnerAffiliateReferralRowModel> Referrals { get; set; } = [];
}

public sealed class PartnerAffiliateReferralRowModel
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime? ReferredAtUtc { get; set; }
    public DateTime? RewardedAtUtc { get; set; }
    public bool WelcomeTokenAvailable { get; set; }
}

public sealed class PartnerAffiliateToolkitModel
{
    public string TrackingCode { get; set; } = string.Empty;
    public string PartnerPageUrl { get; set; } = string.Empty;
    public string RegisterUrl { get; set; } = string.Empty;
    public string FlyerUrl { get; set; } = string.Empty;
}

public sealed class VacancyTypeCostItem
{
    public string Kind { get; set; } = "Regular";
    public string Label { get; set; } = string.Empty;
    public decimal CostTokens { get; set; }
    public decimal PriceEuro { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SalesPackageItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Category { get; set; } = "Standard";
    public int TokenAmount { get; set; }
    public decimal PriceEuro { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class BranchFlyerRouteDto
{
    public string RedirectPath { get; set; } = "/";
}

public sealed class PublicCompanyPage
{
    public string KvkNumber { get; set; } = string.Empty;
    public string? Vestigingsnummer { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<Guid> CompanyIds { get; set; } = [];
    public List<PublicCompanyBranch>? Branches { get; set; }
}

public sealed class PublicCompanyBranch
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Vestigingsnummer { get; set; }
    public string? PublicPath { get; set; }
}
