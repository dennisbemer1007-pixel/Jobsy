namespace Jobsy.Core.Entities;

/// <summary>
/// Short-lived pending payout-account change. IBAN is Data-Protection encrypted at rest;
/// never returned to the browser until confirmed.
/// </summary>
public class SalesIbanChangePending
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Encrypted IBAN (same protector as profile IBAN).</summary>
    public string EncryptedIban { get; set; } = string.Empty;

    public string HolderName { get; set; } = string.Empty;

    /// <summary>"totp" or "email".</summary>
    public string Method { get; set; } = "totp";

    /// <summary>HMAC hash of the one-time e-mail link token (email method only).</summary>
    public string? EmailTokenHash { get; set; }

    public int FailedAttempts { get; set; }
    public string? LastAcceptedTotpCodeHash { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
}
