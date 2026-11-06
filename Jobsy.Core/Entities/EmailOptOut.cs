namespace Jobsy.Core.Entities;

/// <summary>
/// Optional-mail opt-out keyed by hashed e-mail + template category. No plaintext PII.
/// Kept after account deletion so future mail to the same address stays suppressed.
/// </summary>
public class EmailOptOut
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 hex of the normalized address (pepper from VerificationCodes).</summary>
    public string EmailHash { get; set; } = string.Empty;

    /// <summary>Template key (e.g. PushBom), max 64.</summary>
    public string Category { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>OneClick | Page | Settings.</summary>
    public string Source { get; set; } = string.Empty;
}
