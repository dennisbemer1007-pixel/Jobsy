namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton Admin-configurable Ambassadeur commission tier settings.
/// </summary>
public class AmbassadeurSettings
{
    public Guid Id { get; set; }

    /// <summary>Registered candidates required per commission step (default 50).</summary>
    public int CandidateThreshold { get; set; } = 50;

    /// <summary>Extra percentage points added per threshold reached (default 1.0).</summary>
    public decimal PercentPerThreshold { get; set; } = 1.0m;

    /// <summary>Hard ceiling for Ambassadeur commission percentage (default 15.0).</summary>
    public decimal MaxCommissionPercentage { get; set; } = 15.0m;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
