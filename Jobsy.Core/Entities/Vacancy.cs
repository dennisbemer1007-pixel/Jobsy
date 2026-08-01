using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Entities;

public class Vacancy
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal HourlyWage { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VacancyStatus Status { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>
    /// When set, this vacancy was posted by an intermediary for an end-client (<see cref="CompanyId"/>).
    /// End-client KVK/establishment always remain on <see cref="Company"/> for admin / travel / SROI.
    /// </summary>
    public Guid? IntermediaryCompanyId { get; set; }
    public Company? IntermediaryCompany { get; set; }

    /// <summary>
    /// When false (default): banenkaart shows intermediary name + address.
    /// When true: banenkaart shows end-client name + address.
    /// </summary>
    public bool ShowClientAddressOnMap { get; set; }

    /// <summary>Whether the vacancy was created via the UI, external API, or CSV import.</summary>
    public VacancySource CreatedVia { get; set; } = VacancySource.Manual;

    /// <summary>UTC creation timestamp (used for never-published draft cleanup).</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// First successful publish timestamp. Null means the vacancy was never live —
    /// only those drafts are eligible for automatic cleanup.
    /// </summary>
    public DateTime? PublishedAtUtc { get; set; }

    /// <summary>When the 30-day draft cleanup warning e-mail was sent (once).</summary>
    public DateTime? DraftCleanupWarningSentAtUtc { get; set; }

    public GeoPoint Location { get; set; } = null!;
    public TransportMode RequiredTransport { get; set; }

    /// <summary>Up to two branches (sector types) for discovery filtering.</summary>
    public WorkType WorkTypes { get; set; }

    /// <summary>Selected branch labels (comma-separated). Preferred over WorkTypes flags when set.</summary>
    public string? WorkTypeLabels { get; set; }

    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsHighlighted { get; set; }

    /// <summary>
    /// UTC expiry of the paid highlight. When in the past, the vacancy is no longer treated as featured
    /// on the banenkaart (carousel / pulse marker), even if <see cref="IsHighlighted"/> is still true.
    /// </summary>
    public DateTime? HighlightedUntil { get; set; }

    public int ExtensionCount { get; set; }

    /// <summary>Options requested while waiting for PendingApproval (applied on approve).</summary>
    public bool RequestedHighlight { get; set; }
    public bool RequestedPushBom { get; set; }
    public bool RequestedExtend { get; set; }
    public string? RequiredDrivingLicense { get; set; }
    public string? RequiredEducation { get; set; }
    public int? MinimumEmployers { get; set; }
    public Guid? FulfilledByApplicationId { get; set; }

    public int MaxApplications { get; set; } = 5;
    public Guid? SalaryTableId { get; set; }
    public CompanySalaryTable? SalaryTable { get; set; }

    /// <summary>Minimum hours/week for this role (matching + filters).</summary>
    public decimal? MinHoursPerWeek { get; set; }

    /// <summary>Maximum hours/week for this role (matching + filters).</summary>
    public decimal? MaxHoursPerWeek { get; set; }

    /// <summary>Day-parts matrix JSON (<see cref="Rules.SchedulePayload"/>).</summary>
    public string? ScheduleJson { get; set; }

    public bool FlexibleTimes { get; set; }

    /// <summary>Manual | ImportEmpty | ApiEmpty | AtsEmpty when FlexibleTimes.</summary>
    public string? FlexibleScheduleSource { get; set; }

    public bool? LegalWorksAfter19 { get; set; }
    public bool? LegalNightShift23To06 { get; set; }
    public bool? LegalAdultSupervisorPresent { get; set; }
    public bool? LegalHandlesMoneyOrClosing { get; set; }
    public bool? LegalHeavyOrHazardousWork { get; set; }

    /// <summary>
    /// When true, this vacancy uses its own direct-contact flags instead of the company (or parent) defaults.
    /// Contact values (e-mail/phone/WhatsApp) always come from the company profile.
    /// </summary>
    public bool OverrideContactPreference { get; set; }

    public bool DirectContactEnabled { get; set; }
    public bool ContactPreferMail { get; set; }
    public bool ContactPreferPhone { get; set; }
    public bool ContactPreferWhatsApp { get; set; }

    public ICollection<VacancyClick> Clicks { get; set; } = new List<VacancyClick>();
    public ICollection<VacancyLike> Likes { get; set; } = new List<VacancyLike>();
    public ICollection<VacancyShare> Shares { get; set; } = new List<VacancyShare>();
    public ICollection<VacancySearchImpression> SearchImpressions { get; set; } = new List<VacancySearchImpression>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
