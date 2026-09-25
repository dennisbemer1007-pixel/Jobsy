namespace Jobsy.Core.Entities;

/// <summary>Per-step completion progress for a candidate career plan.</summary>
public class CandidateCareerStepProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PlanId { get; set; }
    public CandidateCareerPlan Plan { get; set; } = null!;

    public string StepKey { get; set; } = "";
    public int StepOrder { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Manual | Auto | ManualUndo</summary>
    public string Source { get; set; } = nameof(Rules.CareerStepProgressSource.Manual);

    public DateTime UpdatedAtUtc { get; set; }
}
