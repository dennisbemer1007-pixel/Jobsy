using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class WorkInitiative
{
    public Guid Id { get; set; }
    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;

    public Guid RootCompanyId { get; set; }
    public Company RootCompany { get; set; } = null!;

    public Guid? BranchCompanyId { get; set; }
    public Company? BranchCompany { get; set; }

    public Guid? InitiativeTeamId { get; set; }
    public InitiativeTeam? InitiativeTeam { get; set; }

    public WorkInitiativeStatus Status { get; set; } = WorkInitiativeStatus.Pending;
    public InitiativeMatchLabel MatchLabel { get; set; }

    public string Motivation { get; set; } = string.Empty;
    public string? SnapshotAttachedDocument { get; set; }

    public string SnapshotRoleTitle { get; set; } = string.Empty;
    public string SnapshotRegionLabel { get; set; } = string.Empty;
    public string SnapshotWorkDnaSummary { get; set; } = string.Empty;
    public string SnapshotAvailabilitySummary { get; set; } = string.Empty;
    public int? SnapshotHoursPerWeek { get; set; }

    public string CandidateDisplayName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? SnapshotAboutMe { get; set; }
    public string? SnapshotExperienceSummary { get; set; }

    /// <summary>False for auto-declined after 5 business days (does not count toward weekly limit).</summary>
    public bool CountsTowardWeeklyLimit { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EmployerViewedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime ResponseDeadlineUtc { get; set; }
}
