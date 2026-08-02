using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class RevenueShareService : IRevenueShareService
{
    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _tokens;
    private readonly ICommissionLedgerService _commissions;

    public RevenueShareService(
        JobsyDbContext db,
        ITokenLedgerService tokens,
        ICommissionLedgerService commissions)
    {
        _db = db;
        _tokens = tokens;
        _commissions = commissions;
    }

    public async Task ApplyTokenPurchaseShareAsync(
        Guid tokenCheckoutId,
        Guid companyId,
        Guid? purchaseTokenTransactionId,
        int packSize,
        decimal purchaseAmountEuro,
        Guid? salesManagerUserId,
        DateTime? firstYearStartedAt,
        CancellationToken cancellationToken = default)
    {
        if (purchaseAmountEuro <= 0 || packSize <= 0)
        {
            return;
        }

        // Only referred (tracked) companies participate in the automated split.
        if (salesManagerUserId is null)
        {
            return;
        }

        if (await _db.RevenueShareLogs.AnyAsync(l => l.TokenCheckoutId == tokenCheckoutId, cancellationToken))
        {
            return;
        }

        var ambassadorTokens = SalesCommissionRules.AmbassadorTokens(packSize);
        var ambassadorEuro = SalesCommissionRules.ShareEuro(
            purchaseAmountEuro, SalesCommissionRules.AmbassadorShareRate);
        var smEuro = SalesCommissionRules.ShareEuro(
            purchaseAmountEuro, SalesCommissionRules.SalesManagerShareRate);
        var platformEuro = SalesCommissionRules.ShareEuro(
            purchaseAmountEuro, SalesCommissionRules.PlatformShareRate);

        if (ambassadorTokens > 0)
        {
            await _tokens.GrantAsync(
                companyId,
                ambassadorTokens,
                actorUserId: null,
                note: $"Revenue-share ambassadeur 15% ({tokenCheckoutId:N})",
                cancellationToken);
        }

        if (smEuro > 0)
        {
            await _commissions.TryCreditTokenCommissionAsync(
                salesManagerUserId.Value,
                companyId,
                tokenCheckoutId,
                purchaseAmountEuro,
                firstYearStartedAt,
                cancellationToken);
        }

        var now = DateTime.UtcNow;
        _db.RevenueShareLogs.AddRange(
            new RevenueShareLog
            {
                Id = Guid.NewGuid(),
                TokenCheckoutId = tokenCheckoutId,
                TokenTransactionId = purchaseTokenTransactionId,
                CompanyId = companyId,
                RecipientCompanyId = companyId,
                RecipientKind = RevenueShareRecipientKind.Ambassador,
                Percentage = SalesCommissionRules.AmbassadorShareRate * 100m,
                AmountEuro = ambassadorEuro,
                Tokens = ambassadorTokens,
                CreatedAtUtc = now
            },
            new RevenueShareLog
            {
                Id = Guid.NewGuid(),
                TokenCheckoutId = tokenCheckoutId,
                TokenTransactionId = purchaseTokenTransactionId,
                CompanyId = companyId,
                RecipientUserId = salesManagerUserId,
                RecipientKind = RevenueShareRecipientKind.SalesManager,
                Percentage = SalesCommissionRules.SalesManagerShareRate * 100m,
                AmountEuro = smEuro,
                Tokens = null,
                CreatedAtUtc = now
            },
            new RevenueShareLog
            {
                Id = Guid.NewGuid(),
                TokenCheckoutId = tokenCheckoutId,
                TokenTransactionId = purchaseTokenTransactionId,
                CompanyId = companyId,
                RecipientKind = RevenueShareRecipientKind.Platform,
                Percentage = SalesCommissionRules.PlatformShareRate * 100m,
                AmountEuro = platformEuro,
                Tokens = null,
                CreatedAtUtc = now
            });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent duplicate — unique index on (TokenCheckoutId, RecipientKind).
        }
    }

    public async Task<IReadOnlyList<RevenueShareLog>> ListForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.RevenueShareLogs
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId || l.RecipientCompanyId == companyId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
