using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class TrainingProvider
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TrainingProviderKind Kind { get; set; }
    public TrainingNetwork Network { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    /// <summary>Comma-separated shortage fields (zorg, techniek, logistiek).</summary>
    public string FieldsCsv { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal? CplEuro { get; set; }
    public decimal? CpaEuro { get; set; }
    public decimal? IntakeFeeEuro { get; set; }
    public decimal? StartFeeEuro { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Partner feed adapter (JSON/CSV/API). None = manual admin offers only.</summary>
    public TrainingPartnerFeedKind FeedKind { get; set; }
    public string? FeedUrl { get; set; }
    /// <summary>JSON mapping / column map for CSV. Not shown in candidate UI.</summary>
    public string? FeedMappingJson { get; set; }
    /// <summary>Configuration secret name (API key), never stored in plain text in the repo.</summary>
    public string? FeedApiSecretName { get; set; }
    public int ImportIntervalHours { get; set; } = 24;
    public DateTime? LastImportedAtUtc { get; set; }
    /// <summary>Demo-only partner rows (acceptatie/Development).</summary>
    public bool IsDemoPartner { get; set; }

    public ICollection<TrainingOffer> Offers { get; set; } = new List<TrainingOffer>();
}

public class TrainingOffer
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public TrainingProvider Provider { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string FieldsCsv { get; set; } = string.Empty;
    public string KeysCsv { get; set; } = string.Empty;
    public string? ExternalPath { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Passport curation: existing rows stay false until an admin opts in.</summary>
    public TrainingOfferType Type { get; set; } = TrainingOfferType.Cursus;
    public int? DurationValue { get; set; }
    public TrainingDurationUnit? DurationUnit { get; set; }
    public TrainingDeliveryMode Delivery { get; set; } = TrainingDeliveryMode.Online;
    public string? Location { get; set; }
    public bool IsFree { get; set; }
    public bool IsPartner { get; set; }
    public string? AffiliateCode { get; set; }
    public bool ShowInPassport { get; set; }

    /// <summary>Stable id from partner feed for dedupe (unique per provider).</summary>
    public string? PartnerExternalId { get; set; }
    public decimal? PriceEuro { get; set; }
    /// <summary>When set, shown instead of formatted price (e.g. "Lesgeld per jaar").</summary>
    public string? PriceLabel { get; set; }
    public TrainingOfferSection Section { get; set; } = TrainingOfferSection.General;
    /// <summary>CompetencyTestCatalog category keys, comma-separated.</summary>
    public string CompetenciesCsv { get; set; } = string.Empty;
    /// <summary>Career step keys/titles this offer supports.</summary>
    public string CareerStepsCsv { get; set; } = string.Empty;
    /// <summary>Short NL relevance line (no scores).</summary>
    public string? ReasonNl { get; set; }
    public string? ReasonEn { get; set; }
    public string? LearnBulletsJson { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateTime? ImportedAtUtc { get; set; }
}

public class TrainingEnrollment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OfferId { get; set; }
    public TrainingOffer Offer { get; set; } = null!;
    public TrainingEnrollmentStatus Status { get; set; }
    public int ProgressCurrent { get; set; }
    public int ProgressTotal { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public TrainingCertificateVerificationSource? VerificationSource { get; set; }
    public string? CertificateTitle { get; set; }
}

public class TrainingClick
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public TrainingOffer Offer { get; set; } = null!;
    public Guid? UserId { get; set; }
    public string CandidateHash { get; set; } = string.Empty;
    public string EmailHash { get; set; } = string.Empty;
    public string Campaign { get; set; } = string.Empty;
    public DateTime ClickedAtUtc { get; set; } = DateTime.UtcNow;
    public string OutboundUrl { get; set; } = string.Empty;

    public ICollection<TrainingConversion> Conversions { get; set; } = new List<TrainingConversion>();
}

public class TrainingConversion
{
    public Guid Id { get; set; }
    public Guid ClickId { get; set; }
    public TrainingClick Click { get; set; } = null!;
    public TrainingConversionKind Kind { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "manual";
}
