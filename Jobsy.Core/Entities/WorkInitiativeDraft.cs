namespace Jobsy.Core.Entities;

/// <summary>Saved motivation when the candidate hit the weekly initiative limit.</summary>
public class WorkInitiativeDraft
{
    public Guid Id { get; set; }
    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;

    public Guid RootCompanyId { get; set; }
    public Company RootCompany { get; set; } = null!;

    public Guid? BranchCompanyId { get; set; }
    public Guid? InitiativeTeamId { get; set; }

    public string Motivation { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
