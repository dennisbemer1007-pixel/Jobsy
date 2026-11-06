namespace Jobsy.Core.Entities;

/// <summary>UWV agreement: activities per 28-day period.</summary>
public class ActivityAgreement
{
    public Guid CandidateUserId { get; set; }
    public User CandidateUser { get; set; } = null!;

    public int ActivitiesPerPeriod { get; set; } = 4;

    /// <summary>Inclusive start of the anchor period (Amsterdam calendar date).</summary>
    public DateOnly PeriodStart { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
