using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface IRoleFitCheckService
{
    Task<RoleFitCheckStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<RoleFitCheckStateDto> EvaluateAsync(
        Guid userId,
        string? jobTitle,
        Guid? vacancyId = null,
        CancellationToken cancellationToken = default);
}

public sealed record RoleFitCheckStateDto(
    bool IsUnlocked,
    bool CompetenceQuickScanCompleted,
    bool CareerQuickScanCompleted,
    bool DeepAnalysisCompleted,
    decimal DeepAnalysisPriceEuro,
    string LockMessage,
    string DeepUpsellCopy,
    RoleFitCheckResultDto? LastResult);

public sealed record RoleFitCheckResultDto(
    string JobTitle,
    int MatchPercent,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> ActionSteps,
    IReadOnlyList<string> SearchKeys,
    string MapHref,
    bool FromDeepAnalysis,
    bool FromOpenAi,
    bool ShowDeepUpsell,
    IReadOnlyList<TrainingOfferCardDto> TrainingOffers,
    Guid? VacancyId = null,
    string? BarrierKind = null,
    int? CultureFitPercent = null,
    string? CultureFitBand = null,
    string? CultureFitLabel = null,
    string? CultureFitWhy = null,
    IReadOnlyList<RoleFitFormalItemDto>? FormalItems = null,
    bool ShowFormalBlock = false,
    bool ShowUpskill = false,
    bool AvailabilityOk = true);

public sealed record RoleFitFormalItemDto(string Key, string Label, bool Met, string Note);
