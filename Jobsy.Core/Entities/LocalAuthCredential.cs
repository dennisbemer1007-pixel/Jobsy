namespace Jobsy.Core.Entities;

/// <summary>
/// Local password credential for users created via registration.
/// <see cref="PasswordHash"/> holds a PBKDF2 hash (never plaintext).
/// </summary>
public class LocalAuthCredential
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    /// <summary>PBKDF2 password hash (see JobsyPasswordHasher).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Failed password attempts since the last successful sign-in.</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>Until when password sign-in is temporarily blocked.</summary>
    public DateTime? LockoutUntil { get; set; }

    /// <summary>Number of lockouts started in the current 24 h window.</summary>
    public int LockoutCount { get; set; }

    /// <summary>When the most recent lockout started (UTC).</summary>
    public DateTime? LastLockoutAtUtc { get; set; }

    /// <summary>When the most recent lockout mail was sent (UTC). At most one per 24 h.</summary>
    public DateTime? LastLockoutMailAtUtc { get; set; }
}
