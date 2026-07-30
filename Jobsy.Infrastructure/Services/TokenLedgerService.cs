using System.Data;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class TokenLedgerService : ITokenLedgerService
{
    private readonly JobsyDbContext _db;

    public TokenLedgerService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetBalanceAsync(Guid companyId, CancellationToken cancellationToken = default)
        => await _db.TokenTransactions
            .Where(t => t.CompanyId == companyId)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

    public async Task<decimal?> GetCostAsync(TokenSpendReason reason, CancellationToken cancellationToken = default)
        => await _db.TokenSpendCosts.AsNoTracking()
            .Where(c => c.Reason == reason && c.IsActive)
            .Select(c => (decimal?)c.CostTokens)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<TokenSpendReason, decimal>> GetCostsAsync(
        IEnumerable<TokenSpendReason> reasons,
        CancellationToken cancellationToken = default)
    {
        var wanted = reasons.Where(r => r != TokenSpendReason.None).Distinct().ToArray();
        if (wanted.Length == 0)
        {
            return new Dictionary<TokenSpendReason, decimal>();
        }

        var rows = await _db.TokenSpendCosts.AsNoTracking()
            .Where(c => wanted.Contains(c.Reason) && c.IsActive)
            .Select(c => new { c.Reason, c.CostTokens })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Reason, r => r.CostTokens);
    }

    public async Task<TokenTransaction> GrantAsync(
        Guid companyId,
        decimal amount,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Grant amount must be positive.");
        }

        return await CreditAsync(
            companyId,
            amount,
            TokenTransactionKind.Grant,
            actorUserId,
            note ?? "Grant",
            amountExVatCents: 0,
            vatAmountCents: 0,
            totalAmountCents: 0,
            checkoutId: null,
            invoiceId: null,
            cancellationToken);
    }

    public async Task<TokenTransaction> GrantGoodwillAsync(
        Guid companyId,
        decimal amount,
        string reason,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Goodwill amount must be positive.");
        }

        var note = reason?.Trim();
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("Reden is verplicht voor goodwill-/compensatietokens.", nameof(reason));
        }

        if (note.Length > 512)
        {
            throw new ArgumentException("Reden mag maximaal 512 tekens zijn.", nameof(reason));
        }

        // Monetary value € 0,00 — no BTW obligation / omzet; balance still increases.
        return await CreditAsync(
            companyId,
            amount,
            TokenTransactionKind.Goodwill,
            actorUserId,
            note,
            amountExVatCents: 0,
            vatAmountCents: 0,
            totalAmountCents: 0,
            checkoutId: null,
            invoiceId: null,
            cancellationToken);
    }

    public async Task<TokenTransaction> RecordPurchaseAsync(
        Guid companyId,
        decimal amount,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Purchase amount must be positive.");
        }

        return await CreditAsync(
            companyId,
            amount,
            TokenTransactionKind.Purchase,
            actorUserId,
            note ?? "Mollie purchase",
            amountExVatCents: 0,
            vatAmountCents: 0,
            totalAmountCents: 0,
            checkoutId: null,
            invoiceId: null,
            cancellationToken);
    }

    public async Task<TokenTransaction> RecordPurchaseAsync(
        Guid companyId,
        decimal tokenAmount,
        int amountExVatCents,
        int vatAmountCents,
        int totalAmountCents,
        Guid? checkoutId,
        Guid? invoiceId,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (tokenAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenAmount), "Purchase amount must be positive.");
        }

        if (totalAmountCents < 0 || amountExVatCents < 0 || vatAmountCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmountCents), "Monetary amounts must be non-negative cents.");
        }

        if (amountExVatCents + vatAmountCents != totalAmountCents)
        {
            throw new ArgumentException("Ex-BTW + BTW must equal total (cents).");
        }

        return await CreditAsync(
            companyId,
            tokenAmount,
            TokenTransactionKind.Purchase,
            actorUserId,
            note ?? "Mollie purchase",
            amountExVatCents,
            vatAmountCents,
            totalAmountCents,
            checkoutId,
            invoiceId,
            cancellationToken);
    }

    public async Task<(TokenTransaction From, TokenTransaction To)> AllocateAsync(
        Guid fromCompanyId,
        Guid toCompanyId,
        decimal amount,
        Guid? actorUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Allocation amount must be positive.");
        }

        if (fromCompanyId == toCompanyId)
        {
            throw new ArgumentException("Cannot allocate tokens to the same company.");
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            _ = await _db.Companies.FirstOrDefaultAsync(c => c.Id == fromCompanyId, cancellationToken)
                ?? throw new InvalidOperationException("Source company not found.");
            _ = await _db.Companies.FirstOrDefaultAsync(c => c.Id == toCompanyId, cancellationToken)
                ?? throw new InvalidOperationException("Target company not found.");

            var fromBalance = await GetBalanceAsync(fromCompanyId, cancellationToken);
            if (fromBalance < amount)
            {
                throw new InvalidOperationException(
                    $"Onvoldoende tokens. Benodigd: {amount}, saldo: {fromBalance}.");
            }

            var toBalance = await GetBalanceAsync(toCompanyId, cancellationToken);
            var allocationNote = note ?? "Vestiging-allocatie";

            var fromEntry = new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = fromCompanyId,
                Amount = -amount,
                Kind = TokenTransactionKind.Allocation,
                Reason = TokenSpendReason.None,
                OldBalance = fromBalance,
                NewBalance = fromBalance - amount,
                ActorUserId = actorUserId,
                BranchCompanyId = toCompanyId,
                Note = allocationNote,
                CreatedAt = DateTime.UtcNow
            };

            var toEntry = new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = toCompanyId,
                Amount = amount,
                Kind = TokenTransactionKind.Allocation,
                Reason = TokenSpendReason.None,
                OldBalance = toBalance,
                NewBalance = toBalance + amount,
                ActorUserId = actorUserId,
                BranchCompanyId = fromCompanyId,
                Note = allocationNote,
                CreatedAt = DateTime.UtcNow
            };

            _db.TokenTransactions.AddRange(fromEntry, toEntry);
            await _db.SaveChangesAsync(cancellationToken);
            return (fromEntry, toEntry);
        }, cancellationToken);
    }

    private async Task<TokenTransaction> CreditAsync(
        Guid companyId,
        decimal amount,
        TokenTransactionKind kind,
        Guid? actorUserId,
        string note,
        int amountExVatCents,
        int vatAmountCents,
        int totalAmountCents,
        Guid? checkoutId,
        Guid? invoiceId,
        CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            _ = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
                ?? throw new InvalidOperationException("Company not found.");

            var oldBalance = await GetBalanceAsync(companyId, cancellationToken);
            var entry = new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Amount = amount,
                Kind = kind,
                Reason = TokenSpendReason.None,
                OldBalance = oldBalance,
                NewBalance = oldBalance + amount,
                ActorUserId = actorUserId,
                Note = note,
                AmountExVatCents = amountExVatCents,
                VatAmountCents = vatAmountCents,
                TotalAmountCents = totalAmountCents,
                TokenPurchaseCheckoutId = checkoutId,
                TokenPurchaseInvoiceId = invoiceId,
                CreatedAt = DateTime.UtcNow
            };

            _db.TokenTransactions.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            return entry;
        }, cancellationToken);
    }

    public async Task<TokenSpendOutcome> TrySpendAsync(
        Guid companyId,
        TokenSpendReason reason,
        Guid? vacancyId = null,
        Guid? actorUserId = null,
        Guid? branchCompanyId = null,
        string? note = null,
        Func<CancellationToken, Task>? onSuccessBeforeCommit = null,
        IReadOnlyDictionary<TokenSpendReason, decimal>? costOverrides = null,
        CancellationToken cancellationToken = default)
    {
        if (reason == TokenSpendReason.None)
        {
            return new TokenSpendOutcome(false, "Spend reason is required.", null, 0);
        }

        var multi = await TrySpendManyAsync(
            companyId,
            [reason],
            vacancyId,
            actorUserId,
            branchCompanyId,
            note,
            onSuccessBeforeCommit,
            costOverrides,
            cancellationToken);

        return new TokenSpendOutcome(
            multi.Succeeded,
            multi.ErrorMessage,
            multi.Transactions.FirstOrDefault(),
            multi.Balance);
    }

    public async Task<TokenMultiSpendOutcome> TrySpendManyAsync(
        Guid companyId,
        IReadOnlyList<TokenSpendReason> reasons,
        Guid? vacancyId = null,
        Guid? actorUserId = null,
        Guid? branchCompanyId = null,
        string? note = null,
        Func<CancellationToken, Task>? onSuccessBeforeCommit = null,
        IReadOnlyDictionary<TokenSpendReason, decimal>? costOverrides = null,
        CancellationToken cancellationToken = default)
    {
        var spendReasons = reasons
            .Where(r => r != TokenSpendReason.None)
            .Distinct()
            .ToList();
        if (spendReasons.Count == 0)
        {
            return new TokenMultiSpendOutcome(false, "Minstens één spend-reden is verplicht.", [], 0);
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            var resolved = await ResolveSpendCostsAsync(spendReasons, costOverrides, cancellationToken);
            if (resolved.ErrorMessage is not null)
            {
                return new TokenMultiSpendOutcome(false, resolved.ErrorMessage, [], 0);
            }

            var costs = resolved.Costs!;

            _ = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
                ?? throw new InvalidOperationException("Company not found.");

            var totalCost = spendReasons.Sum(r => costs[r]);
            var oldBalance = await GetBalanceAsync(companyId, cancellationToken);
            if (oldBalance < totalCost)
            {
                return new TokenMultiSpendOutcome(
                    false,
                    $"Onvoldoende tokens. Benodigd: {totalCost} token(s), saldo: {oldBalance}.",
                    [],
                    oldBalance);
            }

            var running = oldBalance;
            var entries = new List<TokenTransaction>();
            foreach (var reason in spendReasons)
            {
                var cost = costs[reason];
                var entry = new TokenTransaction
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    Amount = -cost,
                    Kind = TokenTransactionKind.Spend,
                    Reason = reason,
                    OldBalance = running,
                    NewBalance = running - cost,
                    ActorUserId = actorUserId,
                    VacancyId = vacancyId,
                    BranchCompanyId = branchCompanyId,
                    Note = note,
                    CreatedAt = DateTime.UtcNow
                };
                running = entry.NewBalance;
                entries.Add(entry);
                _db.TokenTransactions.Add(entry);
            }

            if (onSuccessBeforeCommit is not null)
            {
                await onSuccessBeforeCommit(cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new TokenMultiSpendOutcome(true, null, entries, running);
        }, cancellationToken);
    }

    private async Task<(Dictionary<TokenSpendReason, decimal>? Costs, string? ErrorMessage)> ResolveSpendCostsAsync(
        IReadOnlyList<TokenSpendReason> spendReasons,
        IReadOnlyDictionary<TokenSpendReason, decimal>? costOverrides,
        CancellationToken cancellationToken)
    {
        var lookupReasons = spendReasons
            .Where(r => costOverrides is null || !costOverrides.ContainsKey(r))
            .ToArray();
        var configured = lookupReasons.Length == 0
            ? new Dictionary<TokenSpendReason, decimal>()
            : await GetCostsAsync(lookupReasons, cancellationToken);

        var costs = new Dictionary<TokenSpendReason, decimal>();
        foreach (var reason in spendReasons)
        {
            if (costOverrides is not null && costOverrides.TryGetValue(reason, out var overrideCost))
            {
                if (overrideCost <= 0)
                {
                    return (null, $"Ongeldige tokenkost voor {reason}.");
                }

                costs[reason] = overrideCost;
                continue;
            }

            if (!configured.TryGetValue(reason, out var cost) || cost <= 0)
            {
                return (null, $"Geen actieve tokenkost geconfigureerd voor {reason}.");
            }

            costs[reason] = cost;
        }

        return (costs, null);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            if (!_db.Database.IsRelational())
            {
                return await action();
            }

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var result = await action();
                await tx.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
