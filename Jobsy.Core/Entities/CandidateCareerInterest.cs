namespace Jobsy.Core.Entities;

/// <summary>
/// Free RIASEC / Holland-code career interest Quick-Scan (25 items) for one candidate.
/// </summary>
public class CandidateCareerInterest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary><c>Draft</c> or <c>Completed</c>.</summary>
    public string Status { get; set; } = CandidateCompetencyStatuses.Draft;

    /// <summary>JSON object of question id → Likert 1–5.</summary>
    public string AnswersJson { get; set; } = "{}";

    public int? RealisticPercent { get; set; }
    public int? InvestigativePercent { get; set; }
    public int? ArtisticPercent { get; set; }
    public int? SocialPercent { get; set; }
    public int? EnterprisingPercent { get; set; }
    public int? ConventionalPercent { get; set; }

    /// <summary>Top-3 Holland letters, e.g. <c>RSE</c>.</summary>
    public string HollandCode { get; set; } = "";

    /// <summary>JSON array of RIASEC interest tags.</summary>
    public string RiasecTagsJson { get; set; } = "[]";

    /// <summary>JSON array of match tags for talent-pool search and vacancy ranking.</summary>
    public string MatchTagsJson { get; set; } = "[]";

    /// <summary>Persisted OpenAI (or local fallback) compass JSON for Mijn Beroepen-kompas.</summary>
    public string CompassJson { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
