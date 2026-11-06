namespace Jobsy.Core.Entities;

/// <summary>SMS OTP for a candidate phone. Only the hash is stored.</summary>
public class PhoneVerificationChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string PhoneE164 { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
}
