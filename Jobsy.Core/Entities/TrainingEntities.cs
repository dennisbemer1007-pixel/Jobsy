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
