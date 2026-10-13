namespace Jobsy.Core.Entities.Scholen;

/// <summary>In-progress answers for a pupil code. Never returned to staff APIs.</summary>
public class PupilProgress
{
    public Guid PupilCodeId { get; set; }
    public PupilCode? PupilCode { get; set; }
    /// <summary>JSON map item id → Likert 1..5.</summary>
    public string AnswersJson { get; set; } = "{}";
    public int CurrentIndex { get; set; }
    /// <summary>JSON array of hobby chip keys.</summary>
    public string LikesJson { get; set; } = "[]";
    /// <summary>JSON array of dislike chip keys.</summary>
    public string DislikesJson { get; set; } = "[]";
    public string? LikeOtherWord { get; set; }
    public string? DislikeOtherWord { get; set; }
    /// <summary>Set when the pupil confirms pauze-eiland chips (even if empty).</summary>
    public DateTime? ChipsSavedAtUtc { get; set; }
    public string? DreamJobKey { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
