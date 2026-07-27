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
    public GeoPoint Location { get; set; } = null!;
    public TransportMode RequiredTransport { get; set; }

    /// <summary>Up to two branches (sector types) for discovery filtering.</summary>
    public WorkType WorkTypes { get; set; }

    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsHighlighted { get; set; }
    public int ExtensionCount { get; set; }

    /// <summary>Options requested while waiting for PendingApproval (applied on approve).</summary>
    public bool RequestedHighlight { get; set; }
    public bool RequestedPushBom { get; set; }
    public bool RequestedExtend { get; set; }

    public int MaxApplications { get; set; } = 5;
    public Guid? SalaryTableId { get; set; }
    public CompanySalaryTable? SalaryTable { get; set; }

    public ICollection<VacancyClick> Clicks { get; set; } = new List<VacancyClick>();
    public ICollection<VacancyLike> Likes { get; set; } = new List<VacancyLike>();
    public ICollection<VacancyShare> Shares { get; set; } = new List<VacancyShare>();
    public ICollection<VacancySearchImpression> SearchImpressions { get; set; } = new List<VacancySearchImpression>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
