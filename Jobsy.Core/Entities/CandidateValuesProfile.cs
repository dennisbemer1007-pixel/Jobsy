namespace Jobsy.Core.Entities;

/// <summary>
/// Candidate Schwartz values Quick-Scan (25 items across five workplace drivers).
/// Deep analysis (150 items) lives in <see cref="CandidateDeepAnalysis"/> with
/// <see cref="Enums.AssessmentKind.Values"/>.
/// </summary>
public class CandidateValuesProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Status { get; set; } = CandidateCompetencyStatuses.Draft;
    public string AnswersJson { get; set; } = "{}";

    public int? AutonomyPercent { get; set; }
    public int? ConnectionPercent { get; set; }
    public int? AchievementPercent { get; set; }
    public int? StabilityPercent { get; set; }
    public int? ImpactPercent { get; set; }

    public string MatchTagsJson { get; set; } = "[]";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
