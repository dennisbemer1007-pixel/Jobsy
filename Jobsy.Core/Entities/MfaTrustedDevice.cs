namespace Jobsy.Core.Entities;

/// <summary>Trusted device that skips only the MFA code (not the password) for 30 days.</summary>
public class MfaTrustedDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>SHA-256 hex of the raw trust token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public string UserAgentSummary { get; set; } = string.Empty;
    public DateTime? RevokedAtUtc { get; set; }
    public int SessionVersionAtCreate { get; set; }
}
