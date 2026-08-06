using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Entities;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;

    /// <summary>
    /// Unique establishment key: {kvkNumber}_{vestigingsnummer}.
    /// </summary>
    public string? KvkEstablishmentId { get; set; }

    /// <summary>
    /// Verified = KVK matched at registration; Pending = API was down (retry job);
    /// Failed = retries exhausted / rejected.
    /// </summary>
    public KvkVerificationStatus KvkVerificationStatus { get; set; } = KvkVerificationStatus.Verified;

    public DateTime? KvkVerifiedAtUtc { get; set; }
    public DateTime? KvkLastVerificationAttemptAtUtc { get; set; }
    public int KvkVerificationAttempts { get; set; }

    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public GeoPoint Location { get; set; } = null!;
    public CompanyType Type { get; set; } = CompanyType.Employer;

    public Guid? ParentCompanyId { get; set; }
    public Company? ParentCompany { get; set; }
    public ICollection<Company> ChildCompanies { get; set; } = new List<Company>();

    /// <summary>
    /// True after the one-time welcome token was granted on successful registration activation.
    /// Balance itself lives in <see cref="TokenTransactions"/> (ledger sum).
    /// </summary>
    public bool HasReceivedWelcomeToken { get; set; }

    /// <summary>
    /// When true, the enterprise manager (bedrijfsmanager) manages tokens for this vestiging:
    /// purchases go into the organisation pot and the EM issues tokens to this branch.
    /// When false, the vestiging manages its own token purchases.
    /// </summary>
    public bool TokensManagedByEnterprise { get; set; }

    /// <summary>
    /// When true for an organisation (parent company), the CSV Batch Import nav and screen are available.
    /// </summary>
    public bool CsvBatchImportEnabled { get; set; }

    /// <summary>
    /// Preferred Mollie payment method for token top-ups (<c>ideal</c> or <c>creditcard</c>).
    /// Used as the default at checkout; managers can override per purchase.
    /// </summary>
    public string? PreferredPaymentMethod { get; set; }

    /// <summary>
    /// Hard stop: when set, a "We missen je" re-engagement e-mail was already sent once for this account.
    /// Never auto-send again unless an admin clears this field.
    /// </summary>
    public DateTime? ReengagementEmailSentAtUtc { get; set; }

    /// <summary>Last successful CSV vacancy import (any row) for activity tracking.</summary>
    public DateTime? LastCsvImportAtUtc { get; set; }

    /// <summary>
    /// When true, candidates may be offered direct contact (mail/phone/WhatsApp) after a successful application.
    /// </summary>
    public bool DirectContactEnabled { get; set; }

    public bool ContactPreferMail { get; set; }
    public bool ContactPreferPhone { get; set; }
    public bool ContactPreferWhatsApp { get; set; }

    /// <summary>Contact e-mail shown after apply when Mail is preferred.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Phone number for tel: links after apply.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>WhatsApp number (digits); falls back to <see cref="ContactPhone"/> when empty.</summary>
    public string? ContactWhatsApp { get; set; }

    /// <summary>Salesmanager who referred this supplier (via tracking code).</summary>
    public Guid? ReferredBySalesManagerUserId { get; set; }
    public User? ReferredBySalesManagerUser { get; set; }

    /// <summary>Ambassadeur who referred this supplier (entrepreneur flyer / AM- tracking code).</summary>
    public Guid? ReferredByAmbassadeurUserId { get; set; }
    public User? ReferredByAmbassadeurUser { get; set; }

    /// <summary>Partner affiliate (Bedrijfsmanager or Intermediair) who referred this supplier.</summary>
    public Guid? ReferredByPartnerUserId { get; set; }
    public User? ReferredByPartnerUser { get; set; }

    /// <summary>
    /// Ambassadeur commission rate (0–1) frozen when the Ambassadeur referral became active.
    /// </summary>
    public decimal? CommissionAmbassadeurRateSnapshot { get; set; }

    /// <summary>
    /// Indirect (upline) salesmanager snapshotted at activation — not re-resolved live later.
    /// </summary>
    public Guid? CommissionIndirectSalesManagerUserId { get; set; }
    public User? CommissionIndirectSalesManagerUser { get; set; }

    /// <summary>Direct commission rate frozen when the referral became active (e.g. 0.15).</summary>
    public decimal? CommissionDirectRateSnapshot { get; set; }

    /// <summary>Indirect commission rate frozen when the referral became active (e.g. 0.03).</summary>
    public decimal? CommissionIndirectRateSnapshot { get; set; }

    /// <summary>Commission window length in days frozen at activation.</summary>
    public int? CommissionDurationDaysSnapshot { get; set; }

    /// <summary>When commission terms (SM, upline, rates, duration) were snapshotted.</summary>
    public DateTime? CommissionTermsSnapshottedAtUtc { get; set; }

    /// <summary>Platform-wide founder slot 1–10 when eligible; null otherwise.</summary>
    public int? FirstYearSupplierSlot { get; set; }

    /// <summary>Start of the first-year partnership window (for token commission year 1/2).</summary>
    public DateTime? FirstYearStartedAt { get; set; }

    /// <summary>
    /// When true, the next published vacancy receives one free start-highlight
    /// (salesmanager tracking referral). Cleared after use — not a flat discount.
    /// </summary>
    public bool PendingStartHighlightBonus { get; set; }

    public ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
    public ICollection<TokenTransaction> TokenTransactions { get; set; } = new List<TokenTransaction>();
    public ICollection<User> PrimaryUsers { get; set; } = new List<User>();
    public ICollection<UserCompany> UserMemberships { get; set; } = new List<UserCompany>();
    public ICollection<RegionCompany> RegionMemberships { get; set; } = new List<RegionCompany>();
    public ICollection<CompanySalaryTable> SalaryTables { get; set; } = new List<CompanySalaryTable>();
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
