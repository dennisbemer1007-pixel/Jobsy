namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton row for PushBom reach parameters (radius + max travel time).
/// </summary>
public class PushBomSettings
{
    public Guid Id { get; set; }

    /// <summary>Crow-flies radius in km for OpenForWork matching.</summary>
    public double RadiusKm { get; set; } = 10;

    /// <summary>Maximum route travel time in minutes for a candidate to count as in reach.</summary>
    public int MaxTravelMinutes { get; set; } = 30;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
