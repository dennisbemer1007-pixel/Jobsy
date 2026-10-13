namespace Jobsy.Core.Entities;

/// <summary>
/// First-login candidate onboarding wizard progress (one row per user).
/// Step analytics live in <see cref="StepsJson"/> for dropout analysis.
/// </summary>
public class CandidateOnboarding
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>1–10 (content steps). Install/notifications is post-step UI (not counted).</summary>
    public int CurrentStep { get; set; } = 1;

    /// <summary>
    /// Wizard schema version. Existing rows default to 1; new rows use 2.
    /// Incomplete v1 rows are mapped to v2 step numbers on read.
    /// </summary>
    public int WizardVersion { get; set; } = 1;

    /// <summary>
    /// True when the uncounted finish screen ("Je Kompas staat klaar") was reached
    /// but onboarding is not yet completed (e.g. migrated from v1 step 10).
    /// </summary>
    public bool FinishReached { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Optional origin (e.g. school, organic). Nullable for later vouchers.</summary>
    public string? Source { get; set; }

    /// <summary>Reserved for school vouchers; not used yet.</summary>
    public Guid? SchoolVoucherId { get; set; }

    /// <summary>
    /// JSON array of per-step analytics:
    /// [{ "step": 1, "startedAtUtc": "...", "completedAtUtc": "...", "skippedAtUtc": "..." }, …]
    /// </summary>
    public string StepsJson { get; set; } = "[]";

    public DateTime UpdatedAtUtc { get; set; }
}
