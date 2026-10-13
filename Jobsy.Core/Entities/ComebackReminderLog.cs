namespace Jobsy.Core.Entities;

/// <summary>
/// One reminder event (not one channel). No message body and no e-mail address.
/// <see cref="UserId"/> is cleared when the account is deleted.
/// </summary>
public class ComebackReminderLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary><see cref="Reminders.ComebackReminderKinds"/>.</summary>
    public string Kind { get; set; } = "";

    public DateTime SentAtUtc { get; set; }

    /// <summary>Channel names that accepted the send, for example <c>email,push</c>.</summary>
    public string Channels { get; set; } = "";
}
