namespace Jobsy.Core.Entities;

/// <summary>Precomputed top vacancy matches for one candidate (recompute-on-write).</summary>
public class CandidateMatchSnapshot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>JSON array of <see cref="Jobsy.Core.Interfaces.CandidateMatchedVacancyDto"/>.</summary>
    public string MatchesJson { get; set; } = "[]";
    public string InputFingerprint { get; set; } = "";
    public DateTime ComputedAtUtc { get; set; }
    /// <summary>Ready | Updating</summary>
    public string Status { get; set; } = "Ready";
}
