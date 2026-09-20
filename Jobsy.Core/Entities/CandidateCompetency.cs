namespace Jobsy.Core.Entities;

/// <summary>
/// Quick-Scan competency test (25 items: Big Five + RIASEC) for one candidate.
/// One row per user; draft answers may be incomplete.
/// </summary>
public class CandidateCompetency
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary><c>Draft</c> or <c>Completed</c>.</summary>
    public string Status { get; set; } = CandidateCompetencyStatuses.Draft;

    /// <summary>JSON object of question id → Likert 1–5, e.g. <c>{"1":4,"2":5}</c>.</summary>
    public string AnswersJson { get; set; } = "{}";

    public int? SamenwerkenPercent { get; set; }
    public int? ResultaatgerichtheidPercent { get; set; }
    public int? StressbestendigheidPercent { get; set; }
    public int? InnovatiePercent { get; set; }

    /// <summary>JSON array of RIASEC interest tags, e.g. <c>["Social","Enterprising"]</c>.</summary>
    public string RiasecTagsJson { get; set; } = "[]";

    /// <summary>JSON array of match tags derived from scores + RIASEC for talent-pool search.</summary>
    public string MatchTagsJson { get; set; } = "[]";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public static class CandidateCompetencyStatuses
{
    public const string Draft = "Draft";
    public const string Completed = "Completed";

    public static bool IsCompleted(string? status)
        => string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase);
}
