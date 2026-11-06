using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class InterviewReminder
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public InterviewReminderMoment Moment { get; set; }
    public InterviewReminderChannel Channel { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime ScheduledForUtc { get; set; }
}
