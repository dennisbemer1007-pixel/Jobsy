namespace Jobsy.Core.Entities;

/// <summary>Last Functie-Fit Checker result for one candidate (job title + structured advice, no NAW).</summary>
public class CandidateRoleFitCheck
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string JobTitle { get; set; } = "";
    public int MatchPercent { get; set; }
    public string ResultJson { get; set; } = "{}";
    /// <summary>Hash of job title/vacancy + test scores + preferences at evaluate time.</summary>
    public string InputFingerprint { get; set; } = "";
    public bool FromDeepAnalysis { get; set; }
    public bool FromOpenAi { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
