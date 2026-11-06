using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Public employer self-registration via KVK (pending activation or takeover).
/// </summary>
public class CompanyRegistration
{
    public Guid Id { get; set; }

    public string KvkNumber { get; set; } = string.Empty;
    public string KvkEstablishmentId { get; set; } = string.Empty;
    public string EstablishmentName { get; set; } = string.Empty;
    public string EstablishmentAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public RegistrationScope Scope { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }

    public string ActivationToken { get; set; } = string.Empty;

    /// <summary>SHA-256/HMAC hash of the 6-digit e-mail confirmation OTP (cleared after use/expiry).</summary>
    public string? EmailVerificationCode { get; set; }

    /// <summary>UTC expiry for the confirmation OTP (typically 10 minutes after submit).</summary>
    public DateTime? EmailVerificationExpiresAt { get; set; }

    /// <summary>Failed OTP guesses for the current confirmation code; lockout after max attempts.</summary>
    public int EmailVerificationFailedAttempts { get; set; }

    /// <summary>
    /// PBKDF2 hash of the password chosen at registration (cleared after activation/takeover approve).
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>Primary SBI code captured from KVK at submit (for audit / role assignment).</summary>
    public string? PrimarySbiCode { get; set; }

    /// <summary>True when KVK SBI starts with 78 (employment/recruitment agency).</summary>
    public bool IsIntermediarySbi { get; set; }

    /// <summary>
    /// Verified when KVK matched at submit; Pending when the user continued during API outage.
    /// </summary>
    public KvkVerificationStatus KvkVerificationStatus { get; set; } = KvkVerificationStatus.Verified;

    /// <summary>
    /// Set when the contact e-mail was confirmed (activation or takeover e-mail verification).
    /// Takeover approve requires this so a chosen password is never applied to an unverified address.
    /// </summary>
    public DateTime? ContactEmailVerifiedAt { get; set; }

    public CompanyRegistrationStatus Status { get; set; } = CompanyRegistrationStatus.PendingActivation;

    public Guid? CreatedUserId { get; set; }
    public User? CreatedUser { get; set; }
    public Guid? CreatedOrganizationCompanyId { get; set; }
    public Company? CreatedOrganizationCompany { get; set; }
    public Guid? CreatedBranchCompanyId { get; set; }
    public Company? CreatedBranchCompany { get; set; }

    public DateTime? ConsentAcceptedAt { get; set; }
    public string? ConsentVersion { get; set; }

    /// <summary>Optional salesmanager tracking code captured at submit.</summary>
    public string? SalesManagerTrackingCode { get; set; }

    /// <summary>Resolved salesmanager user id (preferred over raw tracking text).</summary>
    public Guid? SalesManagerUserId { get; set; }

    /// <summary>Optional partner affiliate tracking code (BM-/IM-) captured at submit.</summary>
    public string? PartnerTrackingCode { get; set; }

    /// <summary>How the sales attribution was resolved (typed vs cookie). Partner-only stays null.</summary>
    public SalesAttributionSource? SalesAttributionSource { get; set; }

    /// <summary>
    /// JSON array of KVK establishment ids the registrant selected (organization scope with untick).
    /// Null/empty → claim all free siblings (legacy).
    /// </summary>
    public string? SelectedEstablishmentIdsJson { get; set; }

    /// <summary>When the registrant confirmed they may represent the company.</summary>
    public DateTime? RepresentationConsentAtUtc { get; set; }

    /// <summary>Version string of the representation consent text.</summary>
    public string? RepresentationConsentVersion { get; set; }

    /// <summary>Preferred login: password | microsoft | google.</summary>
    public string? PreferredLoginProvider { get; set; }

    /// <summary>True when manual entry could not be geocoded (never pin at NL centre).</summary>
    public bool LocationUnknown { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }

    /// <summary>Day-7 unverified reminder e-mail sent (once).</summary>
    public DateTime? ReminderSentDay7AtUtc { get; set; }

    /// <summary>Day-21 unverified reminder e-mail sent (once).</summary>
    public DateTime? ReminderSentDay21AtUtc { get; set; }

    /// <summary>When registering via an external vacancy employer invite, links to the outbound mail row.</summary>
    public Guid? ExternalVacancyOutboundId { get; set; }

    public ICollection<EstablishmentTakeoverRequest> TakeoverRequests { get; set; } = new List<EstablishmentTakeoverRequest>();
}
