using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Per-candidate opt-in for proactive vacancy matching (Lobsy zoekt voor je).</summary>
public class CandidateProactiveMatchingSettings
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool Enabled { get; set; }

    public ProactiveMatchingFrequency Frequency { get; set; } = ProactiveMatchingFrequency.Daily;

    /// <summary>When set, no proactive notifications until this moment (UTC).</summary>
    public DateTime? PausedUntilUtc { get; set; }

    public DateTime? ConsentAcceptedAtUtc { get; set; }
    public string? ConsentVersion { get; set; }

    public DateTime? LastProcessedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
