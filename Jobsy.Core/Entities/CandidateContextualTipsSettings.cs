namespace Jobsy.Core.Entities;

/// <summary>Per-candidate preferences for contextual tips (hide list, moment toggle, optional interview reminder).</summary>
public class CandidateContextualTipsSettings
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>When false, no contextual tip surfaces (Alle tips hub stays available for settings restore).</summary>
    public bool MomentTipsEnabled { get; set; } = true;

    /// <summary>Optional reminder one day before an interview. Default off; candidate must opt in.</summary>
    public bool InterviewPrepReminderEnabled { get; set; }

    /// <summary>JSON array of hidden contextual tip ids.</summary>
    public string HiddenTipIdsJson { get; set; } = "[]";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
