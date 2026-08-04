namespace Jobsy.Api.Models;

public record UpsertVacancyCategoryRequest(
    string Name,
    string ColorHex,
    decimal PublishCostTokens = 1m,
    bool HighlightAvailable = true,
    decimal HighlightCostTokens = 2m,
    bool PushBomAvailable = true,
    decimal? PushBomCostTokens = null,
    bool IsAlwaysFree = false,
    string PlacementKind = "Regular",
    IReadOnlyList<string>? ExtraFields = null,
    int? SortOrder = null,
    bool? IsActive = null,
    bool? ShowInMapFilter = true,
    bool? ShowInLegend = true);
