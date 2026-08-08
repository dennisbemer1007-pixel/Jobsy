namespace Jobsy.Core.Entities;

/// <summary>In-app notification for any authenticated user (candidate or employer).</summary>
public class UserNotification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public string Category { get; set; } = string.Empty;

    /// <summary>Optional CTA label shown in the notification panel / e-mail mirror.</summary>
    public string? ActionLabel { get; set; }

    /// <summary>Relative or absolute URL for the optional CTA.</summary>
    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }

    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
