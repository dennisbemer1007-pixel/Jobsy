namespace Jobsy.Core.Entities;

/// <summary>
/// Short-lived one-time code so external OAuth can finish inside the PWA scope
/// (iOS standalone uses a different cookie jar than the system browser).
/// </summary>
public class DeviceLoginHandoff
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 hex of the opaque code (never store plaintext).</summary>
    public string CodeHash { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool RememberDevice { get; set; } = true;
    public string? ReturnUrl { get; set; }
    public string? UserAgent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
