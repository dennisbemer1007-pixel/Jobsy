using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Admin-managed vacancy category: color, token pricing, upgrades, and extra create-form fields.
/// Drives map filter, legend, and create dropdown automatically.
/// </summary>
public class VacancyCategory
{
    public Guid Id { get; set; }

    /// <summary>Stable slug used for seeding and lookups (e.g. "volunteer").</summary>
    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Hex color for map pins / filter / legend (e.g. #F54A1B).</summary>
    public string ColorHex { get; set; } = "#F54A1B";

    /// <summary>Base tokens to publish a vacancy in this category.</summary>
    public decimal PublishCostTokens { get; set; }

    public bool HighlightAvailable { get; set; } = true;
    public decimal HighlightCostTokens { get; set; }

    public bool PushBomAvailable { get; set; } = true;

    /// <summary>
    /// Fixed PushBom token cost for this category.
    /// When null, legacy reach-based PushBom tiers are used (if PushBom is available).
    /// </summary>
    public decimal? PushBomCostTokens { get; set; }

    /// <summary>
    /// When true (vrijwilligerswerk): publish is free and highlight/PushBom are forced off.
    /// </summary>
    public bool IsAlwaysFree { get; set; }

    /// <summary>
    /// JSON array of extra field keys shown on create (see <c>VacancyCategoryExtraFields</c>).
    /// </summary>
    public string ExtraFieldsJson { get; set; } = "[]";

    /// <summary>
    /// Maps to legacy <see cref="VacancyKind"/> for exclusivity / pricing sync.
    /// </summary>
    public VacancyKind PlacementKind { get; set; } = VacancyKind.Regular;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowInMapFilter { get; set; } = true;
    public bool ShowInLegend { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
}
