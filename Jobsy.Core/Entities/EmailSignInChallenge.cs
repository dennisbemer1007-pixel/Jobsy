using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Passwordless e-mail code challenge for candidate self sign-up / sign-in.
/// Plaintext codes are never stored — only <see cref="CodeHash"/>.
/// </summary>
public class EmailSignInChallenge
{
    public Guid Id { get; set; }

    public string EmailNormalized { get; set; } = string.Empty;

    /// <summary>Peppered hash from <c>VerificationCodes.Hash</c>.</summary>
    public string CodeHash { get; set; } = string.Empty;

    public EmailSignInPurpose Purpose { get; set; }

    public string? FirstName { get; set; }

    public string? ReferralCode { get; set; }

    public string? ReturnUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public int FailedAttempts { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    /// <summary>Optimistic concurrency for parallel verify races.</summary>
    public int Version { get; set; }
}
