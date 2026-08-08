namespace Jobsy.Core.Entities;

/// <summary>
/// One-time signed action token for e-mail / notification CTAs
/// (set unavailable, withdraw other applications after hire).
/// </summary>
public class CandidateActionToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>See <see cref="Rules.CandidateActionPurposes"/>.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>HMAC hash of the opaque token (never store plaintext).</summary>
    public string TokenHash { get; set; } = string.Empty;

    public Guid? RelatedApplicationId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
