namespace Jobsy.Core.Entities;

/// <summary>DISC Quick-Scan (25 workplace items) for one candidate.</summary>
public class CandidateDiscProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Status { get; set; } = CandidateCompetencyStatuses.Draft;
    public string AnswersJson { get; set; } = "{}";

    public int? DominantPercent { get; set; }
    public int? InvloedPercent { get; set; }
    public int? StabielPercent { get; set; }
    public int? NauwkeurigPercent { get; set; }

    public string MatchTagsJson { get; set; } = "[]";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
