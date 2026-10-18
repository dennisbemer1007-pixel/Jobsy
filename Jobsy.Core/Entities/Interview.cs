using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class Interview
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public InterviewStatus Status { get; set; } = InterviewStatus.Proposing;
    public Guid? ChosenSlotId { get; set; }
    public InterviewSlot? ChosenSlot { get; set; }

    public InterviewFormat Format { get; set; } = InterviewFormat.OnSite;
    public string? LocationOrLink { get; set; }
    public string? WithWhom { get; set; }
    public string? Note { get; set; }

    /// <summary>Stable UID for ICS updates when rescheduled.</summary>
    public string CalendarUid { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<InterviewSlot> Slots { get; set; } = new List<InterviewSlot>();
    public ICollection<InterviewChange> Changes { get; set; } = new List<InterviewChange>();
    public ICollection<InterviewReminder> Reminders { get; set; } = new List<InterviewReminder>();
}
