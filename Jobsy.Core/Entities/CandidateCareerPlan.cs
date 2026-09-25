namespace Jobsy.Core.Entities;

/// <summary>Persisted candidate career plan (one active plan per user).</summary>
public class CandidateCareerPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DreamTitle { get; set; } = "";
    public string DreamKey { get; set; } = "";
    public string PlanJson { get; set; } = "{}";
    public int MatchPercent { get; set; }
    public string MatchSummary { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<CandidateCareerStepProgress> StepProgress { get; set; } = [];
}
