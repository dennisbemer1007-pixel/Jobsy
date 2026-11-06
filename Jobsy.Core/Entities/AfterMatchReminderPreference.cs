namespace Jobsy.Core.Entities;

/// <summary>Per-candidate reminder toggles for interview notifications (mail now; push later).</summary>
public class AfterMatchReminderPreference
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool WeekBefore { get; set; }
    public bool DayBefore18 { get; set; } = true;
    public bool TwoHoursBefore { get; set; } = true;

    public DateTime UpdatedAtUtc { get; set; }
}
