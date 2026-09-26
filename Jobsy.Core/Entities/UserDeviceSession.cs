namespace Jobsy.Core.Entities;

/// <summary>
/// Long-lived "remember this device" session. Raw refresh tokens are never stored —
/// only SHA-256 hashes. Rotation is single-use within a family (theft detection).
/// </summary>
public class UserDeviceSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>SHA-256 hex of the current refresh token.</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 of the immediately previous token (grace window for concurrent requests).
    /// Cleared after <see cref="PreviousTokenGraceUntilUtc"/>.
    /// </summary>
    public string? PreviousRefreshTokenHash { get; set; }

    public DateTime? PreviousTokenGraceUntilUtc { get; set; }

    /// <summary>Stable family id across rotations; reuse of an old token revokes the whole family.</summary>
    public Guid FamilyId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>Human-readable label derived from the user-agent (e.g. "iPhone – Safari").</summary>
    public string? DeviceName { get; set; }
}
