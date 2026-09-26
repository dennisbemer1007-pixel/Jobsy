namespace Jobsy.Core.Entities;

/// <summary>Persisted Horizon career plan for one candidate (one active plan per user).</summary>
public class CandidateCareerPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DreamTitle { get; set; } = "";
    /// <summary>Normalized dream title for change detection.</summary>
    public string DreamKey { get; set; } = "";
    /// <summary>JSON array of step content (StepKey, Order, Title, Courses, …).</summary>
    public string PlanJson { get; set; } = "[]";
    public int MatchPercent { get; set; }
    public string MatchSummary { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<CandidateCareerStepProgress> StepProgress { get; set; } = [];
}
