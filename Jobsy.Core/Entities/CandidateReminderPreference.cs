namespace Jobsy.Core.Entities;

/// <summary>
/// Candidate-owned opt-in for come-back reminders. Missing row means email and WhatsApp are off.
/// </summary>
public class CandidateReminderPreference
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Set only when the candidate turns the email reminder on.</summary>
    public DateTime? EmailOptedInAtUtc { get; set; }

    /// <summary>Separate from employer WhatsApp contact consent.</summary>
    public DateTime? WhatsAppOptedInAtUtc { get; set; }

    /// <summary>Phone the candidate typed for WhatsApp reminders. Cleared when they turn it off.</summary>
    public string? WhatsAppPhone { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
