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
}
