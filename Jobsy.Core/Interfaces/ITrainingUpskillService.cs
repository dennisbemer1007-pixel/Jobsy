using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ITrainingUpskillService
{
    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingOfferCardDto>> RecommendAsync(
        Guid userId,
        string? jobTitle,
        IReadOnlyList<string>? searchKeys,
        string campaign,
        CancellationToken cancellationToken = default);

    /// <summary>Passport "Groei verder" slots — empty when no curated free match (D5).</summary>
    Task<IReadOnlyList<PassportCourseCardDto>> RecommendPassportAsync(
        Guid userId,
        string? searchBlob,
        IReadOnlyList<string>? searchKeys,
        CancellationToken cancellationToken = default);

    Task<TrainingTrackedLinkDto> TrackAsync(
        Guid userId,
        Guid offerId,
        string? campaign,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingProviderAdminDto>> ListProvidersAdminAsync(CancellationToken cancellationToken = default);

    Task<TrainingProviderAdminDto> UpsertProviderAsync(
        TrainingProviderUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteProviderAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TrainingOfferAdminDto> UpsertOfferAsync(
        TrainingOfferUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteOfferAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TrainingConversionDto> RecordConversionAsync(
        TrainingConversionRequest request,
        CancellationToken cancellationToken = default);

    Task<string> ExportCsvAsync(int year, int month, Guid? providerId, CancellationToken cancellationToken = default);

    Task ForgetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record TrainingOfferCardDto(
    Guid OfferId,
    string Title,
    string ProviderName,
    string Kind,
    string Network,
    string Region,
    string CtaLabel,
    string Advice);

public sealed record PassportCourseCardDto(
    Guid OfferId,
    string Title,
    string ProviderName,
    string Type,
    int? DurationValue,
    string? DurationUnit,
    string Delivery,
    string? Location,
    bool IsFree,
    bool IsPartner,
    string Rel);

public sealed record TrainingTrackedLinkDto(Guid ClickId, string Url, string CandidateHash);

public sealed record TrainingProviderAdminDto(
    Guid Id,
    string Name,
    string Kind,
    string Network,
    string BaseUrl,
    string FieldsCsv,
    string Region,
    decimal? CplEuro,
    decimal? CpaEuro,
    decimal? IntakeFeeEuro,
    decimal? StartFeeEuro,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<TrainingOfferAdminDto> Offers);

public sealed record TrainingOfferAdminDto(
    Guid Id,
    Guid ProviderId,
    string Title,
    string FieldsCsv,
    string KeysCsv,
    string? ExternalPath,
    bool IsActive,
    int SortOrder,
    string Type,
    int? DurationValue,
    string? DurationUnit,
    string Delivery,
    string? Location,
    bool IsFree,
    bool IsPartner,
    string? AffiliateCode,
    bool ShowInPassport);

public sealed record TrainingProviderUpsertRequest(
    Guid? Id,
    string Name,
    TrainingProviderKind Kind,
    TrainingNetwork Network,
    string BaseUrl,
    string? FieldsCsv,
    string? Region,
    decimal? CplEuro,
    decimal? CpaEuro,
    decimal? IntakeFeeEuro,
    decimal? StartFeeEuro,
    bool IsActive,
    int SortOrder);

public sealed record TrainingOfferUpsertRequest(
    Guid? Id,
    Guid ProviderId,
    string Title,
    string? FieldsCsv,
    string? KeysCsv,
    string? ExternalPath,
    bool IsActive,
    int SortOrder,
    TrainingOfferType Type = TrainingOfferType.Cursus,
    int? DurationValue = null,
    TrainingDurationUnit? DurationUnit = null,
    TrainingDeliveryMode Delivery = TrainingDeliveryMode.Online,
    string? Location = null,
    bool IsFree = false,
    bool IsPartner = false,
    string? AffiliateCode = null,
    bool ShowInPassport = false);

public sealed record TrainingConversionRequest(
    Guid? ClickId,
    string? CandidateHash,
    string? Email,
    TrainingConversionKind Kind);

public sealed record TrainingConversionDto(
    Guid Id,
    Guid ClickId,
    string Kind,
    DateTime RecordedAtUtc,
    string Source);
