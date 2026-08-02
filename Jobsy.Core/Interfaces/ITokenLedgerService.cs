using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface ITokenLedgerService
{
    Task<decimal> GetBalanceAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<decimal?> GetCostAsync(TokenSpendReason reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<TokenSpendReason, decimal>> GetCostsAsync(
        IEnumerable<TokenSpendReason> reasons,
        CancellationToken cancellationToken = default);

    Task<TokenTransaction> GrantAsync(
        Guid companyId,
        decimal amount,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotent grant keyed by <paramref name="tokenPurchaseCheckoutId"/> (unique DB index on Grant+checkout).
    /// Concurrent callers receive the same ledger row instead of double-crediting.
    /// </summary>
    Task<TokenTransaction> GrantForCheckoutAsync(
        Guid companyId,
        decimal amount,
        Guid tokenPurchaseCheckoutId,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin goodwill / service compensation. Token balance increases; monetary value is € 0,00 (no BTW/omzet).
    /// <paramref name="note"/> (reason) is required.
    /// </summary>
    Task<TokenTransaction> GrantGoodwillAsync(
        Guid companyId,
        decimal amount,
        string reason,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Records a Mollie (stub) purchase credit on the company ledger.</summary>
    Task<TokenTransaction> RecordPurchaseAsync(
        Guid companyId,
        decimal amount,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a paid Mollie purchase with monetary amounts in whole cents and optional checkout/invoice links.
    /// </summary>
    Task<TokenTransaction> RecordPurchaseAsync(
        Guid companyId,
        decimal tokenAmount,
        int amountExVatCents,
        int vatAmountCents,
        int totalAmountCents,
        Guid? checkoutId,
        Guid? invoiceId,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves tokens from one accessible company to another (vestiging-allocatie).
    /// Writes two Allocation ledger rows in one transaction.
    /// </summary>
    Task<(TokenTransaction From, TokenTransaction To)> AllocateAsync(
        Guid fromCompanyId,
        Guid toCompanyId,
        decimal amount,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task<TokenSpendOutcome> TrySpendAsync(
        Guid companyId,
        TokenSpendReason reason,
        Guid? vacancyId = null,
        Guid? actorUserId = null,
        Guid? branchCompanyId = null,
        string? note = null,
        Func<CancellationToken, Task>? onSuccessBeforeCommit = null,
        IReadOnlyDictionary<TokenSpendReason, decimal>? costOverrides = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Debits multiple spend reasons in one serializable transaction (single balance check).
    /// Optional <paramref name="costOverrides"/> replaces configured costs for those reasons (e.g. PushBom tier pricing).
    /// </summary>
    Task<TokenMultiSpendOutcome> TrySpendManyAsync(
        Guid companyId,
        IReadOnlyList<TokenSpendReason> reasons,
        Guid? vacancyId = null,
        Guid? actorUserId = null,
        Guid? branchCompanyId = null,
        string? note = null,
        Func<CancellationToken, Task>? onSuccessBeforeCommit = null,
        IReadOnlyDictionary<TokenSpendReason, decimal>? costOverrides = null,
        CancellationToken cancellationToken = default);
}

public sealed record TokenSpendOutcome(
    bool Succeeded,
    string? ErrorMessage,
    TokenTransaction? Transaction,
    decimal Balance);

public sealed record TokenMultiSpendOutcome(
    bool Succeeded,
    string? ErrorMessage,
    IReadOnlyList<TokenTransaction> Transactions,
    decimal Balance);
