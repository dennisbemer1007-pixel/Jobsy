namespace Jobsy.Core.Entities;

/// <summary>Who agreed to what, and when. No message body and no token.</summary>
public class ReferenceConfirmationConsentLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? ReferenceConfirmationId { get; set; }

    /// <summary>candidate or referee.</summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>requested, submitted, declined, misuse, or share.</summary>
    public string Action { get; set; } = string.Empty;

    public string ConsentVersion { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime AtUtc { get; set; }
}
