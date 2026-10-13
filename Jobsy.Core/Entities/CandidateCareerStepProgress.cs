namespace Jobsy.Core.Entities;

/// <summary>Per-step completion state for a persisted career plan.</summary>
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

    /// <summary>Manual | Auto | ManualUndo | CarriedOver</summary>
    public string Source { get; set; } = "Manual";

    /// <summary>
    /// Hash of matching certificate names at undo time; auto-complete stays blocked
    /// only while the fingerprint still matches.
    /// </summary>
    public string? UndoFingerprint { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
