using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Weekly hours registration for a Maqqie placement (pass-through to exporter).</summary>
public class MaqqieHoursWeek
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    /// <summary>Monday of the ISO week (UTC date).</summary>
    public DateOnly WeekStart { get; set; }

    public MaqqieHoursWeekStatus Status { get; set; } = MaqqieHoursWeekStatus.Draft;

    /// <summary>JSON map day-of-week (1=Mon) → hours (decimal).</summary>
    public string DailyHoursJson { get; set; } = "{}";

    public decimal TotalHours { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? EmployerApprovedAtUtc { get; set; }
    public DateTime? SentToMaqqieAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
