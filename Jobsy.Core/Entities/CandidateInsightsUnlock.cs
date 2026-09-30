using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Paid unlock of Kandidaatinzichten for an organisation wallet (company-wide or per vestiging).
/// Price and duration are snapshotted at purchase; later admin changes do not rewrite existing rows.
/// </summary>
public class CandidateInsightsUnlock
{
    public Guid Id { get; set; }

    /// <summary>Organisation wallet that paid (via <c>ResolveWalletCompanyId</c>).</summary>
    public Guid WalletCompanyId { get; set; }
    public Company WalletCompany { get; set; } = null!;

    public CandidateInsightsUnlockScopeKind ScopeKind { get; set; }

    /// <summary>Organisation id (Company scope) or vestiging id (Branch scope).</summary>
    public Guid ScopeCompanyId { get; set; }
    public Company ScopeCompany { get; set; } = null!;

    public DateTime UnlockedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    public decimal PriceTokens { get; set; }
    public int DurationDays { get; set; }

    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;

    public Guid TokenTransactionId { get; set; }
    public TokenTransaction TokenTransaction { get; set; } = null!;

    /// <summary>Client-supplied idempotency key; unique across unlocks.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;
}
