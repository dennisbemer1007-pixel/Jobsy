namespace Jobsy.Core.Entities;

/// <summary>Browser Web Push subscription endpoint for a user device.</summary>
public class WebPushSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Optional link to the remembered device session that created this subscription.</summary>
    public Guid? DeviceSessionId { get; set; }
    public UserDeviceSession? DeviceSession { get; set; }

    /// <summary>Push service endpoint URL.</summary>
    public string Endpoint { get; set; } = "";

    public string P256dh { get; set; } = "";
    public string Auth { get; set; } = "";

    /// <summary>Optional user-agent / device label for debugging.</summary>
    public string? UserAgent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAtUtc { get; set; }
}
