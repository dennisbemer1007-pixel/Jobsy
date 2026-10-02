namespace Jobsy.Core.Entities;

/// <summary>Generation-guard row for career plan AI/local builds (D10).</summary>
public class CandidateCareerGeneration
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DreamKey { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>Ok | Failed | Reused</summary>
    public string Outcome { get; set; } = CareerGenerationOutcomes.Ok;
}

public static class CareerGenerationOutcomes
{
    public const string Ok = "Ok";
    public const string Failed = "Failed";
    public const string Reused = "Reused";
}
