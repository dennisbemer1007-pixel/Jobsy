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
    /// <summary>JSON map puzzle key → <see cref="Jobsy.Core.Scholen.QuestionSets.PupilPuzzleStatus"/> (int).</summary>
    public string PuzzlesJson { get; set; } = "{}";
    public string? DreamJobKey { get; set; }
    /// <summary>Exact text the pupil typed or picked (may differ from catalog title).</summary>
    public string? DreamJobDisplayTitle { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>JSON map subject key → leuk/goed (G78 vakkentest). Wiped with progress after school year.</summary>
    public string? VakScoresJson { get; set; }

    // VO-only optional fields (D3/D8: no names; wiped with progress after school year).
    /// <summary>JSON array of subject chip keys (VO flow).</summary>
    public string? VoFavoriteSubjectsJson { get; set; }
    /// <summary>e.g. natuur-gezondheid, economie (VO flow).</summary>
    public string? VoProfileKey { get; set; }
    /// <summary>JSON side-job availability windows (VO flow).</summary>
    public string? VoSideJobAvailabilityJson { get; set; }
    /// <summary>fiets, ov, etc.</summary>
    public string? VoTravelModeKey { get; set; }
    public string? VoOwnWords { get; set; }
    /// <summary>JSON array of learning-goal strings (VO flow).</summary>
    public string? VoLearningGoalsJson { get; set; }
}
