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
    public bool ExposeRegistrationActivationLinks { get; set; }
    public string PublicWebBaseUrl { get; set; } = "http://localhost:5201";
    public DateTime? UpdatedAtUtc { get; set; }
    public int InactiveCompanyDays { get; set; } = 120;
    public int SessionInactivityTimeoutMinutes { get; set; } = 30;
    public DateOnly? FreePublishUntil { get; set; }
    /// <summary>When true with null FreePublishUntil, admin turned the launch promo off.</summary>
    public bool ClearFreePublishUntil { get; set; }
    public bool SupportAccessNotifyAdmins { get; set; }
    public bool SupportAccessNotifySubject { get; set; }
    public bool AmbassadorsEnabled { get; set; }
}

public sealed class SalesParkedBalanceApiItem
{
    public Guid UserId { get; set; }
    public string MaskedDisplayName { get; set; } = "";
    public decimal OpenBalanceExVat { get; set; }
    public DateTime? LastLineAtUtc { get; set; }
}

public sealed class PlatformCompanyItem
{
    public string CompanyName { get; set; } = "Lobsy";
    public string Slogan { get; set; } = "Dichtbij genoeg om het pantser te laten vallen";
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "NL";
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? VatBufferIban { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SiteBrandingItem
{
    public string CompanyName { get; set; } = "Lobsy";
    public string Slogan { get; set; } = "Dichtbij genoeg om het pantser te laten vallen";
}

public sealed class AboutPageItem
{
    public string Title { get; set; } = "Wie zijn wij";
    public string Lead { get; set; } = "Over Lobsy — en de mens achter de knop";
    public string BodyHtml { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
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
