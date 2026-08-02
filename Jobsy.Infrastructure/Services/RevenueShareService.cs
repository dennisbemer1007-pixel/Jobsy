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
    private readonly ISalesCommercialService _commercial;

    public RevenueShareService(
        JobsyDbContext db,
        ITokenLedgerService tokens,
        ICommissionLedgerService commissions,
        ISalesCommercialService commercial)
    {
        _db = db;
        _tokens = tokens;
        _commissions = commissions;
        _commercial = commercial;
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

        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        var directRate = settings.DirectCommissionRate;
        var indirectRateConfigured = settings.IndirectCommissionRate;
        var durationDays = settings.CommissionDurationDays > 0
            ? settings.CommissionDurationDays
            : SalesCommissionRules.DefaultCommissionDurationDays;

        var asOf = DateTime.UtcNow;
        var withinWindow = SalesCommissionRules.IsWithinCommissionWindow(
            firstYearStartedAt, asOf, durationDays);

        Guid? referringSmId = null;
        if (withinWindow)
        {
            referringSmId = await _db.SalesManagerProfiles.AsNoTracking()
                .Where(p => p.UserId == salesManagerUserId.Value)
                .Select(p => p.ReferredBySalesManagerUserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var appliedDirectRate = withinWindow ? Math.Max(0m, directRate) : 0m;
        var appliedIndirectRate = referringSmId is not null && withinWindow
            ? Math.Max(0m, indirectRateConfigured)
            : 0m;

        var ambassadorTokens = SalesCommissionRules.AmbassadorTokens(packSize);
        var ambassadorEuro = SalesCommissionRules.ShareEuro(
            purchaseAmountEuro, SalesCommissionRules.AmbassadorShareRate);
        var smEuro = SalesCommissionRules.ShareEuro(purchaseAmountEuro, appliedDirectRate);
        var indirectEuro = SalesCommissionRules.ShareEuro(purchaseAmountEuro, appliedIndirectRate);
        var platformRate = SalesCommissionRules.PlatformShareRate(appliedDirectRate, appliedIndirectRate);
        var platformEuro = SalesCommissionRules.ShareEuro(purchaseAmountEuro, platformRate);

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
                directRate,
                durationDays,
                cancellationToken);
        }

        if (indirectEuro > 0 && referringSmId is Guid parentSmId)
        {
            await _commissions.TryCreditIndirectTokenCommissionAsync(
                parentSmId,
                companyId,
                tokenCheckoutId,
                purchaseAmountEuro,
                firstYearStartedAt,
                indirectRateConfigured,
                durationDays,
                cancellationToken);
        }

        var now = DateTime.UtcNow;
        var logs = new List<RevenueShareLog>
        {
            new()
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
            new()
            {
                Id = Guid.NewGuid(),
                TokenCheckoutId = tokenCheckoutId,
                TokenTransactionId = purchaseTokenTransactionId,
                CompanyId = companyId,
                RecipientUserId = salesManagerUserId,
                RecipientKind = RevenueShareRecipientKind.SalesManager,
                Percentage = appliedDirectRate * 100m,
                AmountEuro = smEuro,
                Tokens = null,
                CreatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                TokenCheckoutId = tokenCheckoutId,
                TokenTransactionId = purchaseTokenTransactionId,
                CompanyId = companyId,
                RecipientKind = RevenueShareRecipientKind.Platform,
                Percentage = platformRate * 100m,
                AmountEuro = platformEuro,
                Tokens = null,
                CreatedAtUtc = now
            }
        };

        if (referringSmId is Guid indirectUserId && appliedIndirectRate > 0)
        {
            logs.Add(new RevenueShareLog
            {
                Id = Guid.NewGuid(),
                TokenCheckoutId = tokenCheckoutId,
                TokenTransactionId = purchaseTokenTransactionId,
                CompanyId = companyId,
                RecipientUserId = indirectUserId,
                RecipientKind = RevenueShareRecipientKind.IndirectSalesManager,
                Percentage = appliedIndirectRate * 100m,
                AmountEuro = indirectEuro,
                Tokens = null,
                CreatedAtUtc = now
            });
        }

        _db.RevenueShareLogs.AddRange(logs);

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
