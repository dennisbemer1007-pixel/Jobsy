namespace Jobsy.Core.Entities;

/// <summary>
/// Big Five / OCEAN workplace competency test (20 items) for one candidate.
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
