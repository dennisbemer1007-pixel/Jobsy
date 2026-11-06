using Jobsy.Core.Enums;

namespace Jobsy.Web.Models;

public sealed class TrainingPartnerOfferModel
{
    public Guid OfferId { get; set; }
    public string Title { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public string Type { get; set; } = "";
    public int? DurationValue { get; set; }
    public string? DurationUnit { get; set; }
    public string Delivery { get; set; } = "";
    public string? Location { get; set; }
    public bool IsFree { get; set; }
    public decimal? PriceEuro { get; set; }
    public string? PriceLabel { get; set; }
    public string Reason { get; set; } = "";
    public bool Saved { get; set; }
    public TrainingOfferSection Section { get; set; }
    public List<string> LearnBullets { get; set; } = [];
    public DateOnly? StartsOn { get; set; }

    public bool IsPartnerLink => !IsFree;
}

public sealed class TrainingLearnGrowPageModel
{
    public List<TrainingPartnerOfferModel> NextStep { get; set; } = [];
    public List<TrainingPartnerOfferModel> Growth { get; set; } = [];
    public List<TrainingPartnerOfferModel> General { get; set; } = [];
    public int TotalCount { get; set; }
    public string? NextStepSubtitle { get; set; }
    public string? GrowthSubtitle { get; set; }
}

public sealed class TrainingCompetencyGrowthModel
{
    public string Category { get; set; } = "";
    public int Score { get; set; }
    public string BandKey { get; set; } = "";
    public string MeaningKey { get; set; } = "";
    public bool ShowOffers { get; set; }
    public List<TrainingPartnerOfferModel> Offers { get; set; } = [];
}

public sealed class TrainingProgressSummaryModel
{
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int Saved { get; set; }
    public List<TrainingProgressItemModel> Items { get; set; } = [];
}

public sealed class TrainingProgressItemModel
{
    public Guid OfferId { get; set; }
    public string Title { get; set; } = "";
    public TrainingEnrollmentStatus Status { get; set; }
    public int ProgressCurrent { get; set; }
    public int ProgressTotal { get; set; }
    public bool Verified { get; set; }
}

public sealed class TrainingPartnerAdminModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public TrainingProviderKind Kind { get; set; }
    public TrainingPartnerFeedKind FeedKind { get; set; }
    public string? FeedUrl { get; set; }
    public string? FeedMappingJson { get; set; }
    public string? FeedApiSecretName { get; set; }
    public int ImportIntervalHours { get; set; }
    public DateTime? LastImportedAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsDemoPartner { get; set; }
    public int OfferCount { get; set; }
}

public sealed class TrainingPartnerAdminUpsertModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = "";
    public TrainingProviderKind Kind { get; set; }
    public TrainingPartnerFeedKind FeedKind { get; set; }
    public string? FeedUrl { get; set; }
    public string? FeedMappingJson { get; set; }
    public string? FeedApiSecretName { get; set; }
    public int ImportIntervalHours { get; set; } = 24;
    public bool IsActive { get; set; } = true;
    public bool IsDemoPartner { get; set; }
}
