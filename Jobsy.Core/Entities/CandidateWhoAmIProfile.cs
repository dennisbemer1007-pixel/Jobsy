namespace Jobsy.Core.Entities;

/// <summary>Cached "Wie ben ik?" story and Lobsy-CV bijlage opt-in for one candidate.</summary>
public class CandidateWhoAmIProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IncludeOnCv { get; set; }

    public string StoryText { get; set; } = "";
    public string KeywordsJson { get; set; } = "[]";
    public string InputFingerprint { get; set; } = "";
    public bool FromOpenAi { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? StoryGeneratedAtUtc { get; set; }
}
