using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Single-use expiring link (set-password invite or API-key reveal). Only the hash is stored.
/// </summary>
public class OneTimeLink
{
    public Guid Id { get; set; }

    public OneTimeLinkPurpose Purpose { get; set; }

    /// <summary>Peppered hash of the opaque token (never store plaintext).</summary>
    public string TokenHash { get; set; } = string.Empty;

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>Normalized recipient e-mail (max 254).</summary>
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
