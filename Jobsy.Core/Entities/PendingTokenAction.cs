using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Product action queued on a Mollie token checkout. After payment fulfillment the platform
/// spends the new balance and applies the action without a second user click.
/// </summary>
public class PendingTokenAction
{
    public Guid Id { get; set; }
    public Guid TokenPurchaseCheckoutId { get; set; }
    public TokenPurchaseCheckout Checkout { get; set; } = null!;

    /// <summary>Company whose ledger is debited (vacancy owner / branch).</summary>
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid VacancyId { get; set; }
    public Vacancy Vacancy { get; set; } = null!;

    public PendingTokenActionKind ActionKind { get; set; }
    public bool OptionHighlight { get; set; }
    public bool OptionPushBom { get; set; }
    public bool OptionExtend { get; set; }

    /// <summary>Snapshot of tokens needed at checkout time (informational).</summary>
    public decimal RequiredTokens { get; set; }

    public Guid? ActorUserId { get; set; }
    public PendingTokenActionStatus Status { get; set; } = PendingTokenActionStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
