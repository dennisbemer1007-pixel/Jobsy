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
    int SortOrder);

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
    int SortOrder);

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
