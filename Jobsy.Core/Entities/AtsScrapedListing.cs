using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Staging row for a scraped vacancy. Unique on <see cref="DedupHash"/> (company+title+location).
/// </summary>
public class AtsScrapedListing
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public AtsScrapeSource Source { get; set; } = null!;

    /// <summary>SHA-256 hex of normalized CompanyName|Title|Location.</summary>
    public string DedupHash { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? LocationLabel { get; set; }
    public string? PostalCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SalaryText { get; set; }
    public decimal? HourlyWage { get; set; }
    public string? HoursText { get; set; }
    public decimal? MinHoursPerWeek { get; set; }
    public decimal? MaxHoursPerWeek { get; set; }

    /// <summary>Human-readable start indication, e.g. "per direct" or "1 mei 2026".</summary>
    public string? StartDateText { get; set; }

    /// <summary>Clean bullet-style requirements extracted / AI-normalized from the source page.</summary>
    public string? RequirementsText { get; set; }

    public string? TagsJson { get; set; }
    public string? ImageUrl { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>0–100 field completeness for admin review.</summary>
    public int CompletenessScore { get; set; }

    /// <summary>When AI (or local heuristic) last normalized readable fields.</summary>
    public DateTime? AiEnrichedAtUtc { get; set; }

    /// <summary>True when OpenAI produced the last enrichment (false = local heuristics only).</summary>
    public bool AiEnrichedFromOpenAi { get; set; }

    public AtsListingStatus Status { get; set; } = AtsListingStatus.PendingReview;
    public string? RejectReason { get; set; }

    public DateTime ScrapedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastCheckedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    /// <summary>Vacancy created when admin approved this listing.</summary>
    public Guid? LinkedVacancyId { get; set; }
    public Vacancy? LinkedVacancy { get; set; }
}
