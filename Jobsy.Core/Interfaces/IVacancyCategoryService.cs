using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record VacancyCategoryDto(
    Guid Id,
    string Slug,
    string Name,
    string ColorHex,
    decimal PublishCostTokens,
    bool HighlightAvailable,
    decimal HighlightCostTokens,
    bool PushBomAvailable,
    decimal? PushBomCostTokens,
    bool IsAlwaysFree,
    string PlacementKind,
    IReadOnlyList<string> ExtraFields,
    IReadOnlyList<VacancyCategoryFieldDto> ExtraFieldDefinitions,
    int SortOrder,
    bool IsActive,
    bool ShowInMapFilter,
    bool ShowInLegend);

public sealed record VacancyCategoryFieldDto(
    string Key,
    string Label,
    string InputType,
    IReadOnlyList<string>? Options);

public sealed record VacancyCategoryPricing(
    Guid CategoryId,
    string Name,
    string ColorHex,
    decimal PublishCostTokens,
    bool HighlightAvailable,
    decimal HighlightCostTokens,
    bool PushBomAvailable,
    decimal? PushBomCostTokens,
    bool UseTierPushBomPricing,
    bool IsAlwaysFree,
    VacancyKind PlacementKind);

public interface IVacancyCategoryService
{
    Task<IReadOnlyList<VacancyCategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VacancyCategoryDto>> GetAllAdminAsync(CancellationToken cancellationToken = default);

    Task<VacancyCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VacancyCategory?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default);

    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

    Task<VacancyCategoryDto> CreateAsync(
        string name,
        string colorHex,
        decimal publishCostTokens,
        bool highlightAvailable,
        decimal highlightCostTokens,
        bool pushBomAvailable,
        decimal? pushBomCostTokens,
        bool isAlwaysFree,
        VacancyKind placementKind,
        IEnumerable<string>? extraFields,
        int? sortOrder,
        bool showInMapFilter,
        bool showInLegend,
        CancellationToken cancellationToken = default);

    Task<VacancyCategoryDto?> UpdateAsync(
        Guid id,
        string name,
        string colorHex,
        decimal publishCostTokens,
        bool highlightAvailable,
        decimal highlightCostTokens,
        bool pushBomAvailable,
        decimal? pushBomCostTokens,
        bool isAlwaysFree,
        VacancyKind placementKind,
        IEnumerable<string>? extraFields,
        int? sortOrder,
        bool? isActive,
        bool showInMapFilter,
        bool showInLegend,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VacancyCategoryPricing> ResolvePricingAsync(
        Guid? categoryId,
        VacancyKind fallbackKind,
        CancellationToken cancellationToken = default);

    Task BackfillVacancyCategoriesAsync(CancellationToken cancellationToken = default);
}
