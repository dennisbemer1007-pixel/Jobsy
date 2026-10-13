namespace Jobsy.Core.Entities;

/// <summary>
/// One completed quick-scan (or pilot run) eligible for anonymized psychometric export.
/// </summary>
public class AssessmentResearchAttempt
{
    public Guid Id { get; set; }

    /// <summary>Internal link only; never exported.</summary>
    public Guid? UserId { get; set; }

    public Guid? PilotSessionId { get; set; }

    /// <summary>Stable pseudonym for CSV (no PII).</summary>
    public string RespondentHash { get; set; } = string.Empty;

    public string TestKey { get; set; } = string.Empty;

    public int TestVersion { get; set; } = 1;

    public string TestLanguage { get; set; } = "nl";

    public string? AgeBand { get; set; }
    public string? Gender { get; set; }
    public string? EducationLevel { get; set; }
    public string? NativeLanguage { get; set; }

    public int Status { get; set; }

    public DateTime CompletedAtUtc { get; set; }

    public int? TotalDurationMs { get; set; }

    public string? PilotEndComment { get; set; }

    public bool IsPilot { get; set; }

    public ICollection<AssessmentResearchItemResponse> Items { get; set; } = [];
}
