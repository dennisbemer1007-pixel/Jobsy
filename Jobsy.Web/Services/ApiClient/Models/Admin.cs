using Jobsy.Core.Enums;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

public sealed class MasterdataOptionItem
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnCandidate { get; set; } = true;
    public bool ShowOnVacancy { get; set; } = true;
}

public sealed class MasterdataOptionForm
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? ShowOnCandidate { get; set; }
    public bool? ShowOnVacancy { get; set; }
}

public sealed class ExclusivityEducationItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ExclusivitySettingItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SchoolDomain { get; set; }
    public string? StudentNumberPattern { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOpenOption { get; set; }
    public int SortOrder { get; set; }
    public List<ExclusivityEducationItem> Educations { get; set; } = [];
}

public sealed class ExclusivitySettingForm
{
    public string Name { get; set; } = string.Empty;
    public string? SchoolDomain { get; set; }
    public string? StudentNumberPattern { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOpenOption { get; set; }
    public int SortOrder { get; set; }
    public List<string>? Educations { get; set; }
}

public sealed class IntegrationHealthItem
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime CheckedAtUtc { get; set; }
    public bool? LastPingOk { get; set; }
}

public sealed class KvkUsageItem
{
    public int ZoekenToday { get; set; }
    public int BasisprofielToday { get; set; }
    public int VestigingenToday { get; set; }
    public int ProfileCallsThisMonth { get; set; }
    public int MonthlyProfileBudget { get; set; }
    public bool BudgetWarning { get; set; }
    public string TodaySummary { get; set; } = string.Empty;
}

public sealed class SendTestMailResultItem
{
    public bool Ok { get; set; }
    public bool SentViaSmtp { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class EmailTemplateItem
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool HasMascot { get; set; }
    public bool Parked { get; set; }
    public List<string> Languages { get; set; } = [];
    public bool RequiresEmployers { get; set; }
}

public sealed class EmailCatalogTestOptionsItem
{
    public List<string> TestRecipientAllowList { get; set; } = [];
    public int TestDailyCap { get; set; } = 100;
}

public sealed class EmailTemplatePreviewItem
{
    public string Key { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Preheader { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Dir { get; set; } = "ltr";
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class EmailCatalogSendResultItem
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public bool DeliveredViaProvider { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class EmailCatalogSendAllAcceptedItem
{
    public Guid RunId { get; set; }
    public int Total { get; set; }
}

public sealed class EmailCatalogSendAllStatusItem
{
    public Guid RunId { get; set; }
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public bool Done { get; set; }
    public string? Error { get; set; }
}

public sealed class AiProviderStatusItem
{
    public string Provider { get; set; } = "OpenAI";
    public string DisplayName { get; set; } = "OpenAI";
    public bool ReadOnly { get; set; } = true;
    public bool FellBackToOpenAi { get; set; }
}

public sealed class IntegrationCredentialItem
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public string? ApiKeyMasked { get; set; }
    public bool HasClientSecret { get; set; }
    public string? ClientSecretMasked { get; set; }
    public string? ClientId { get; set; }
    public string? TenantId { get; set; }
    public string? Model { get; set; }
    public string? BaseUrl { get; set; }
    public string? FromAddress { get; set; }
    public bool SupportsApiKey { get; set; }
    public bool SupportsModel { get; set; }
    public bool SupportsOAuth { get; set; }
    public bool SupportsTenantId { get; set; }
    public bool SupportsBaseUrl { get; set; }
    public bool SupportsFromAddress { get; set; }
    public bool? LastPingOk { get; set; }
    public string? LastPingMessage { get; set; }
    public DateTime? LastPingAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IgnoresEnvironmentCredentials { get; set; }
    public bool UsesEnvironmentCredentials { get; set; }
}

public sealed class IntegrationCredentialSaveForm
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }
    public string? BaseUrl { get; set; }
    public string? FromAddress { get; set; }
    public bool ClearApiKey { get; set; }
    public bool ClearClientSecret { get; set; }
    public bool UseEnvironmentCredentials { get; set; }
}

public sealed class PlatformFeatureItem
{
    public bool VacancyContentModerationEnabled { get; set; } = true;
    public bool AuthenticatorEnabled { get; set; }
    public string PublicWebBaseUrl { get; set; } = "http://localhost:5201";
    public DateTime? UpdatedAtUtc { get; set; }
    public int InactiveCompanyDays { get; set; } = 120;
    public int SessionInactivityTimeoutMinutes { get; set; } = 30;
    public DateOnly? FreePublishUntil { get; set; }
    /// <summary>When true with null FreePublishUntil, admin turned the launch promo off.</summary>
    public bool ClearFreePublishUntil { get; set; }
    public bool SupportAccessNotifyAdmins { get; set; }
    public bool SupportAccessNotifySubject { get; set; }
    public bool CandidateInsightsEnabled { get; set; } = true;
    public int CandidateInsightsUnlockDays { get; set; } = 90;
    public bool CandidateInsightsUnlockPerBranch { get; set; }
    // moves into PlatformSettingsCatalog group "Scholen"
    public bool SchoolsEnabled { get; set; }
    public bool SchoolPerCodeResultsEnabled { get; set; } = true;
    public int SchoolRetentionCutoffMonth { get; set; } = 7;
    public int SchoolRetentionCutoffDay { get; set; } = 31;
    public bool AmbassadorsEnabled { get; set; }
    public bool EmployersEnabled { get; set; }
    public bool CandidatePassportEnabled { get; set; } = true;
    public bool PassportPartnersEnabled { get; set; }
    public bool PassportPdfV2Enabled { get; set; }
    public bool PhoneVerificationEnabled { get; set; }
}

/// <summary>Partial PUT body for platform features (null = keep).</summary>
public sealed class PlatformFeaturePatch
{
    public bool? VacancyContentModerationEnabled { get; set; }
    public bool? AuthenticatorEnabled { get; set; }
    public string? PublicWebBaseUrl { get; set; }
    public int? InactiveCompanyDays { get; set; }
    public int? SessionInactivityTimeoutMinutes { get; set; }
    public DateOnly? FreePublishUntil { get; set; }
    public bool ClearFreePublishUntil { get; set; }
    public bool? SupportAccessNotifyAdmins { get; set; }
    public bool? SupportAccessNotifySubject { get; set; }
    public bool? CandidateInsightsEnabled { get; set; }
    public int? CandidateInsightsUnlockDays { get; set; }
    public bool? CandidateInsightsUnlockPerBranch { get; set; }
    public bool? SchoolsEnabled { get; set; }
    public bool? SchoolPerCodeResultsEnabled { get; set; }
    public int? SchoolRetentionCutoffMonth { get; set; }
    public int? SchoolRetentionCutoffDay { get; set; }
    public bool? AmbassadorsEnabled { get; set; }
    public bool? EmployersEnabled { get; set; }
    public bool? CandidatePassportEnabled { get; set; }
    public bool? PassportPartnersEnabled { get; set; }
    public bool? PassportPdfV2Enabled { get; set; }
    public bool? PhoneVerificationEnabled { get; set; }
    public string? Reason { get; set; }
}

/// <summary>Maintenance switch as the admin sees it (errors 05). <c>Note</c> is admin-only.</summary>
public sealed class MaintenanceStateItem
{
    public bool Enabled { get; set; }
    public DateTime? ExpectedEndUtc { get; set; }
    public string? Note { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class MaintenanceUpdateForm
{
    public bool Enabled { get; set; }
    public DateTime? ExpectedEndUtc { get; set; }
    public string? Note { get; set; }
}

public sealed class AdminTodoItemView
{
    public string Key { get; set; } = "";
    public string Severity { get; set; } = "info";
    public string TitleKey { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Area { get; set; } = "";
    public DateTime SinceUtc { get; set; }
    public string ActionLabelKey { get; set; } = "";
    public string Href { get; set; } = "";
    public int Count { get; set; } = 1;
}

public sealed class AdminTodoResponseItem
{
    public List<AdminTodoItemView> Items { get; set; } = [];
    public Dictionary<string, int> CountsByNavKey { get; set; } = new(StringComparer.Ordinal);
}

public sealed class AdminFinanceSummaryItem
{
    public string Period { get; set; } = "week";
    public int RevenueInclVatCents { get; set; }
    public int RevenueExVatCents { get; set; }
    public int PreviousRevenueInclVatCents { get; set; }
    public int TokensSold { get; set; }
    public int OpenAtMollieCents { get; set; }
    public int OpenAtMollieCount { get; set; }
    public int? OldestOpenMollieDays { get; set; }
    public int VatBufferPendingCents { get; set; }
    public int OpenPayoutsCents { get; set; }
    public int OpenPayoutsCount { get; set; }
    public List<AdminFinanceOpenPayoutPreviewItem> OpenPayoutPreviews { get; set; } = [];
}

public sealed class AdminFinanceOpenPayoutPreviewItem
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string MaskedPayeeName { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public decimal TotalInclVat { get; set; }
    public string MaskedIban { get; set; } = "—";
}

public sealed class SalesParkedBalanceApiItem
{
    public Guid UserId { get; set; }
    public string MaskedDisplayName { get; set; } = "";
    public decimal OpenBalanceExVat { get; set; }
    public DateTime? LastLineAtUtc { get; set; }
    public bool EmployersEnabled { get; set; }
    public bool CandidatePassportEnabled { get; set; } = true;
}

public sealed class PlatformCompanyItem
{
    public string CompanyName { get; set; } = "Lobsy";
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string Slogan { get; set; } = "Dichtbij genoeg om het pantser te laten vallen";
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "NL";
    public string? PostalStreet { get; set; }
    public string? PostalPostalCode { get; set; }
    public string? PostalCity { get; set; }
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? SupportEmail { get; set; }
    public string? PrivacyEmail { get; set; }
    public string? VatBufferIban { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SiteBrandingItem
{
    public string CompanyName { get; set; } = "Lobsy";
    public string Slogan { get; set; } = "Dichtbij genoeg om het pantser te laten vallen";
}

public sealed class MarketingFlyerItem
{
    public string Headline { get; set; } = string.Empty;
    public string Subheadline { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public string BulletPoints { get; set; } = string.Empty;
    public string PromoFreeText { get; set; } = string.Empty;
    public string PromoDiscountText { get; set; } = string.Empty;
    public string CtaTitle { get; set; } = string.Empty;
    public string CtaBody { get; set; } = string.Empty;
    public string QrCaption { get; set; } = string.Empty;
    public string QrPath { get; set; } = "/register";
    public string FooterNote { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class RegionHostItem
{
    public Guid Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Slogan { get; set; }
    public string? AddressLabel { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed record RegionHostUpsertPayload(
    string Hostname,
    string DisplayName,
    string? Slogan,
    string? AddressLabel,
    double? Latitude,
    double? Longitude,
    string? BackgroundImageUrl,
    bool IsActive = true);

public sealed class AdminSearchHitDto
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Sublabel { get; set; }
    public string Href { get; set; } = "";
}

public sealed class AdminSearchResultDto
{
    public List<AdminSearchHitDto> Users { get; set; } = [];
    public List<AdminSearchHitDto> Organisations { get; set; } = [];
    public List<AdminSearchHitDto> Vacancies { get; set; } = [];
    public List<AdminSearchHitDto> Invoices { get; set; } = [];
    public List<AdminSearchHitDto> Correlations { get; set; } = [];
}

/// <summary>One row of the admin tab "Meldingen" (public-pages 06). The e-mail arrives masked.</summary>
public sealed class AdminContentReportItem
{
    public Guid Id { get; set; }
    public string TargetType { get; set; } = "Vacancy";
    public Guid TargetId { get; set; }
    public string? TargetKvk { get; set; }
    public string? TargetLabel { get; set; }
    public string Reason { get; set; } = "Other";
    public string? Details { get; set; }
    public string? ReporterEmailMasked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Status { get; set; } = "Open";
    public string? DecisionReason { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public int TargetReportCount { get; set; }
}
