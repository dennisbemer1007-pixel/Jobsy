using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
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
        decimal purchaseAmountExVatEuro,
        Guid? salesManagerUserId,
        DateTime? firstYearStartedAt,
        CancellationToken cancellationToken = default)
    {
        if (purchaseAmountExVatEuro <= 0 || packSize <= 0)
        {
            return;
        }

        // Only referred (tracked) companies participate in the automated split.
        if (salesManagerUserId is null)
        {
            return;
        }

        var existingKinds = await _db.RevenueShareLogs.AsNoTracking()
            .Where(l => l.TokenCheckoutId == tokenCheckoutId)
            .Select(l => l.RecipientKind)
            .ToListAsync(cancellationToken);

        // Fully settled (ambassador log present) — repair grant if needed and exit.
        if (existingKinds.Contains(RevenueShareRecipientKind.Ambassador))
        {
            await EnsureAmbassadorGrantFromLogsAsync(tokenCheckoutId, companyId, cancellationToken);
            return;
        }

        var terms = await ResolveCommissionTermsAsync(
            companyId, salesManagerUserId.Value, cancellationToken);

        var asOf = DateTime.UtcNow;
        var withinWindow = SalesCommissionRules.IsWithinCommissionWindow(
            firstYearStartedAt, asOf, terms.DurationDays);

        var referringSmId = withinWindow ? terms.ReferringSmId : null;
        var appliedDirectRate = withinWindow ? Math.Max(0m, terms.DirectRate) : 0m;
        var appliedIndirectRate = referringSmId is not null && withinWindow
            ? Math.Max(0m, terms.IndirectRate)
            : 0m;

        var ambassadorTokens = SalesCommissionRules.AmbassadorTokens(packSize);
        var ambassadorEuro = SalesCommissionRules.ShareEuro(
            purchaseAmountExVatEuro, SalesCommissionRules.AmbassadorShareRate);
        var smEuro = SalesCommissionRules.ShareEuro(purchaseAmountExVatEuro, appliedDirectRate);
        var indirectEuro = SalesCommissionRules.ShareEuro(purchaseAmountExVatEuro, appliedIndirectRate);
        var platformRate = SalesCommissionRules.PlatformShareRate(appliedDirectRate, appliedIndirectRate);
        var platformEuro = SalesCommissionRules.ShareEuro(purchaseAmountExVatEuro, platformRate);
        var now = DateTime.UtcNow;

        var claimed = existingKinds.Contains(RevenueShareRecipientKind.Platform);
        if (!claimed)
        {
            // Atomic claim via Platform marker (unique on TokenCheckoutId + RecipientKind).
            var platformClaim = new RevenueShareLog
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
            };

            _db.RevenueShareLogs.Add(platformClaim);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                claimed = true;
            }
            catch (DbUpdateException)
            {
                _db.Entry(platformClaim).State = EntityState.Detached;
                // Another worker claimed — if they already finished, exit; else continue repair.
                if (await _db.RevenueShareLogs.AsNoTracking().AnyAsync(
                        l => l.TokenCheckoutId == tokenCheckoutId
                             && l.RecipientKind == RevenueShareRecipientKind.Ambassador,
                        cancellationToken))
                {
                    await EnsureAmbassadorGrantFromLogsAsync(tokenCheckoutId, companyId, cancellationToken);
                    return;
                }

                claimed = true;
            }
        }

        if (!claimed)
        {
            return;
        }

        if (ambassadorTokens > 0)
        {
            var noteFragment = tokenCheckoutId.ToString("N");
            var legacyGrantExists = await _db.TokenTransactions.AsNoTracking()
                .AnyAsync(
                    t => t.CompanyId == companyId
                         && t.Kind == TokenTransactionKind.Grant
                         && t.TokenPurchaseCheckoutId == null
                         && t.Note != null
                         && t.Note.Contains(noteFragment),
                    cancellationToken);
            if (!legacyGrantExists)
            {
                await _tokens.GrantForCheckoutAsync(
                    companyId,
                    ambassadorTokens,
                    tokenCheckoutId,
                    actorUserId: null,
                    note: $"Revenue-share ambassadeur 15% ({noteFragment})",
                    cancellationToken);
            }
        }

        if (smEuro > 0)
        {
            await _commissions.TryCreditTokenCommissionAsync(
                salesManagerUserId.Value,
                companyId,
                tokenCheckoutId,
                purchaseAmountExVatEuro,
                firstYearStartedAt,
                appliedDirectRate,
                terms.DurationDays,
                cancellationToken);
        }

        if (indirectEuro > 0 && referringSmId is Guid parentSmId)
        {
            await _commissions.TryCreditIndirectTokenCommissionAsync(
                parentSmId,
                companyId,
                tokenCheckoutId,
                purchaseAmountExVatEuro,
                firstYearStartedAt,
                appliedIndirectRate,
                terms.DurationDays,
                cancellationToken);
        }

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
            foreach (var log in logs)
            {
                _db.Entry(log).State = EntityState.Detached;
            }
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

    private async Task<(decimal DirectRate, decimal IndirectRate, int DurationDays, Guid? ReferringSmId)>
        ResolveCommissionTermsAsync(
            Guid companyId,
            Guid salesManagerUserId,
            CancellationToken cancellationToken)
    {
        var company = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new
            {
                c.CommissionIndirectSalesManagerUserId,
                c.CommissionDirectRateSnapshot,
                c.CommissionIndirectRateSnapshot,
                c.CommissionDurationDaysSnapshot,
                c.CommissionTermsSnapshottedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (company?.CommissionTermsSnapshottedAtUtc is not null
            && company.CommissionDirectRateSnapshot is not null)
        {
            return (
                company.CommissionDirectRateSnapshot.Value,
                company.CommissionIndirectRateSnapshot ?? 0m,
                company.CommissionDurationDaysSnapshot is > 0
                    ? company.CommissionDurationDaysSnapshot.Value
                    : SalesCommissionRules.DefaultCommissionDurationDays,
                company.CommissionIndirectSalesManagerUserId);
        }

        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        var directRate = settings.DirectCommissionRate;
        var indirectRate = settings.IndirectCommissionRate;
        var durationDays = settings.CommissionDurationDays > 0
            ? settings.CommissionDurationDays
            : SalesCommissionRules.DefaultCommissionDurationDays;

        var referringSmId = await _db.SalesManagerProfiles.AsNoTracking()
            .Where(p => p.UserId == salesManagerUserId)
            .Select(p => p.ReferredBySalesManagerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        await FreezeLegacyCommissionTermsAsync(
            companyId,
            referringSmId,
            directRate,
            indirectRate,
            durationDays,
            cancellationToken);

        return (directRate, indirectRate, durationDays, referringSmId);
    }

    private async Task EnsureAmbassadorGrantFromLogsAsync(
        Guid tokenCheckoutId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var ambassador = await _db.RevenueShareLogs.AsNoTracking()
            .Where(l => l.TokenCheckoutId == tokenCheckoutId
                        && l.RecipientKind == RevenueShareRecipientKind.Ambassador
                        && l.Tokens > 0)
            .Select(l => l.Tokens)
            .FirstOrDefaultAsync(cancellationToken);

        if (ambassador is null or <= 0)
        {
            return;
        }

        // Legacy grants (pre-checkout-id) used a note containing the checkout N-format id.
        var legacyNoteFragment = tokenCheckoutId.ToString("N");
        var legacyGrantExists = await _db.TokenTransactions.AsNoTracking()
            .AnyAsync(
                t => t.CompanyId == companyId
                     && t.Kind == TokenTransactionKind.Grant
                     && t.TokenPurchaseCheckoutId == null
                     && t.Note != null
                     && t.Note.Contains(legacyNoteFragment),
                cancellationToken);
        if (legacyGrantExists)
        {
            return;
        }

        await _tokens.GrantForCheckoutAsync(
            companyId,
            ambassador.Value,
            tokenCheckoutId,
            actorUserId: null,
            note: $"Revenue-share ambassadeur 15% ({legacyNoteFragment})",
            cancellationToken);
    }

    private async Task FreezeLegacyCommissionTermsAsync(
        Guid companyId,
        Guid? indirectSalesManagerUserId,
        decimal directRate,
        decimal indirectRate,
        int durationDays,
        CancellationToken cancellationToken)
    {
        var tracked = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (tracked is null || tracked.CommissionTermsSnapshottedAtUtc is not null)
        {
            return;
        }

        tracked.CommissionIndirectSalesManagerUserId = indirectSalesManagerUserId;
        tracked.CommissionDirectRateSnapshot = Math.Max(0m, directRate);
        tracked.CommissionIndirectRateSnapshot = Math.Max(0m, indirectRate);
        tracked.CommissionDurationDaysSnapshot = durationDays;
        tracked.CommissionTermsSnapshottedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
