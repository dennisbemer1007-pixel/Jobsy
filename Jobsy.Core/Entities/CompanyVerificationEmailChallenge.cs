namespace Jobsy.Core.Entities;

/// <summary>Business e-mail verification OTP (may differ from the login e-mail).</summary>
public class CompanyVerificationEmailChallenge
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }

    /// <summary>Address the code was sent to (not necessarily the user's login e-mail).</summary>
    public string Email { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public int FailedAttempts { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    /// <summary>When 5 wrong attempts kill the code; next send allowed after cooldown.</summary>
    public DateTime? LockedUntilUtc { get; set; }
}
