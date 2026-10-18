using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface ITrainingPartnerService
{
    Task EnsureDemoPartnersAsync(CancellationToken cancellationToken = default);

    Task ImportDueProvidersAsync(CancellationToken cancellationToken = default);

    Task ImportProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingPartnerOfferDto>> ListForCareerStepAsync(
        Guid userId,
        string? stepTitle,
        IReadOnlyList<string>? gapKeys,
        CancellationToken cancellationToken = default);

    Task<TrainingLearnGrowPageDto> GetLearnGrowPageAsync(
        Guid userId,
        TrainingLearnGrowQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingCompetencyGrowthDto>> GetCompetencyGrowthAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TrainingProgressSummaryDto> GetProgressAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task ToggleSavedAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default);

    Task SetInProgressAsync(
        Guid userId,
        Guid offerId,
        int progressCurrent,
        int progressTotal,
        CancellationToken cancellationToken = default);

    Task RequestCertificateVerificationAsync(
        Guid userId,
        Guid offerId,
        string certificateTitle,
        CancellationToken cancellationToken = default);

    Task VerifyEnrollmentFromPartnerAsync(
        string partnerExternalId,
        string candidateHash,
        string certificateTitle,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingPartnerAdminDto>> ListPartnersAdminAsync(CancellationToken cancellationToken = default);

    Task<TrainingPartnerAdminDto> UpsertPartnerAdminAsync(
        TrainingPartnerAdminUpsert request,
        CancellationToken cancellationToken = default);
}

public sealed record TrainingLearnGrowQuery(
    string? Search,
    string? Subject,
    bool FreeOnly,
    string? Format,
    string? Duration);

public sealed record TrainingPartnerOfferDto(
    Guid OfferId,
    string Title,
    string ProviderName,
    string Type,
    int? DurationValue,
    string? DurationUnit,
    string Delivery,
    string? Location,
    bool IsFree,
    decimal? PriceEuro,
    string? PriceLabel,
    string Reason,
    bool Saved,
    TrainingOfferSection Section,
    IReadOnlyList<string> LearnBullets,
    DateOnly? StartsOn);

public sealed record TrainingLearnGrowPageDto(
    IReadOnlyList<TrainingPartnerOfferDto> NextStep,
    IReadOnlyList<TrainingPartnerOfferDto> Growth,
    IReadOnlyList<TrainingPartnerOfferDto> General,
    int TotalCount,
    string? NextStepSubtitle,
    string? GrowthSubtitle);

public sealed record TrainingCompetencyGrowthDto(
    string Category,
    int Score,
    string BandKey,
    string MeaningKey,
    bool ShowOffers,
    IReadOnlyList<TrainingPartnerOfferDto> Offers);

public sealed record TrainingProgressSummaryDto(
    int InProgress,
    int Completed,
    int Saved,
    IReadOnlyList<TrainingProgressItemDto> Items);

public sealed record TrainingProgressItemDto(
    Guid OfferId,
    string Title,
    TrainingEnrollmentStatus Status,
    int ProgressCurrent,
    int ProgressTotal,
    bool Verified);

public sealed record TrainingPartnerAdminDto(
    Guid Id,
    string Name,
    TrainingProviderKind Kind,
    TrainingPartnerFeedKind FeedKind,
    string? FeedUrl,
    string? FeedMappingJson,
    string? FeedApiSecretName,
    int ImportIntervalHours,
    DateTime? LastImportedAtUtc,
    bool IsActive,
    bool IsDemoPartner,
    int OfferCount);

public sealed record TrainingPartnerAdminUpsert(
    Guid? Id,
    string Name,
    TrainingProviderKind Kind,
    TrainingPartnerFeedKind FeedKind,
    string? FeedUrl,
    string? FeedMappingJson,
    string? FeedApiSecretName,
    int ImportIntervalHours,
    bool IsActive,
    bool IsDemoPartner);
