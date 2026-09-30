using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
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

        var paidAt = DateTime.UtcNow;
        var checkoutPaidAt = await _db.TokenPurchaseCheckouts.AsNoTracking()
            .Where(c => c.Id == tokenCheckoutId)
            .Select(c => c.CreditedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (checkoutPaidAt != default)
        {
            paidAt = checkoutPaidAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(checkoutPaidAt, DateTimeKind.Utc)
                : checkoutPaidAt.ToUniversalTime();
        }

        var rootId = await ResolveRootCompanyIdAsync(companyId, cancellationToken);
        await EnsureActivatedAsync(rootId, salesManagerUserId.Value, paidAt, cancellationToken);

        var terms = await ResolveCommissionTermsAsync(rootId, salesManagerUserId.Value, cancellationToken);
        if (terms is null)
        {
            return;
        }

        var year = SalesCommissionRules.YearFor(terms, paidAt);
        var appliedDirectRate = SalesCommissionRules.DirectRate(terms, year) ?? 0m;
        var appliedIndirectRate = SalesCommissionRules.IndirectRate(terms, year) ?? 0m;
        var referringSmId = appliedIndirectRate > 0
            ? await _db.Companies.AsNoTracking()
                .Where(c => c.Id == rootId)
                .Select(c => c.CommissionIndirectSalesManagerUserId)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var grantBonusTokens = SalesCommissionRules.BonusTokensAllowed(terms, paidAt);
        var ambassadorTokens = grantBonusTokens
            ? SalesCommissionRules.AmbassadorTokens(packSize)
            : 0m;
        var ambassadorEuro = grantBonusTokens
            ? SalesCommissionRules.ShareEuro(purchaseAmountExVatEuro, SalesCommissionRules.AmbassadorShareRate)
            : 0m;
        var smEuro = SalesCommissionRules.ShareEuro(purchaseAmountExVatEuro, appliedDirectRate);
        var indirectEuro = SalesCommissionRules.ShareEuro(purchaseAmountExVatEuro, appliedIndirectRate);
        var platformRate = SalesCommissionRules.PlatformShareRate(appliedDirectRate, appliedIndirectRate);
        var platformEuro = SalesCommissionRules.ShareEuro(purchaseAmountExVatEuro, platformRate);
        var now = DateTime.UtcNow;
        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        var holdDays = settings.CommissionHoldDays is >= 0 and <= 60
            ? settings.CommissionHoldDays
            : 14;
        var availableFromUtc = SalesClock.HoldAvailableFromUtc(paidAt, holdDays);

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
                terms.StartsAtUtc,
                terms.DirectYear1Rate,
                terms.DurationDays,
                terms.Year2Rate,
                terms.Year3Rate,
                availableFromUtc,
                cancellationToken);
        }

        if (indirectEuro > 0 && referringSmId is Guid parentSmId)
        {
            await _commissions.TryCreditIndirectTokenCommissionAsync(
                parentSmId,
                companyId,
                tokenCheckoutId,
                purchaseAmountExVatEuro,
                terms.StartsAtUtc,
                appliedIndirectRate,
                terms.DurationDays,
                availableFromUtc,
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

    private async Task EnsureActivatedAsync(
        Guid rootCompanyId,
        Guid salesManagerUserId,
        DateTime paidAtUtc,
        CancellationToken cancellationToken)
    {
        var root = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == rootCompanyId)
            .Select(c => new
            {
                c.CommissionStartsAtUtc,
                c.CommissionDirectRateSnapshot,
                c.CommissionIndirectRateSnapshot,
                c.CommissionDurationDaysSnapshot,
                c.CommissionYear2RateSnapshot,
                c.CommissionYear3RateSnapshot,
                c.CommissionIndirectSalesManagerUserId,
                c.CommissionTermsSnapshottedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (root is null)
        {
            return;
        }

        if (root.CommissionStartsAtUtc is not null
            && root.CommissionYear2RateSnapshot is not null
            && root.CommissionYear3RateSnapshot is not null
            && root.CommissionDirectRateSnapshot is not null)
        {
            return;
        }

        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        Guid? referringSmId = root.CommissionIndirectSalesManagerUserId;
        var directRate = root.CommissionDirectRateSnapshot;
        var indirectRate = root.CommissionIndirectRateSnapshot;
        var durationDays = root.CommissionDurationDaysSnapshot;
        var year2 = root.CommissionYear2RateSnapshot ?? settings.Year2DirectCommissionRate;
        var year3 = root.CommissionYear3RateSnapshot ?? settings.Year3DirectCommissionRate;

        if (directRate is null || root.CommissionTermsSnapshottedAtUtc is null)
        {
            referringSmId = await _db.SalesManagerProfiles.AsNoTracking()
                .Where(p => p.UserId == salesManagerUserId)
                .Select(p => p.ReferredBySalesManagerUserId)
                .FirstOrDefaultAsync(cancellationToken);

            directRate = SalesCommissionRules.Year1RateForSalesManager(
                referringSmId is not null,
                settings.DirectCommissionRate,
                settings.ReferredYear1DirectCommissionRate);
            indirectRate = referringSmId is not null ? settings.IndirectCommissionRate : 0m;
            durationDays = settings.CommissionDurationDays > 0
                ? settings.CommissionDurationDays
                : SalesCommissionRules.DefaultCommissionDurationDays;
        }
        else
        {
            durationDays = durationDays is > 0
                ? durationDays.Value
                : SalesCommissionRules.DefaultCommissionDurationDays;
            indirectRate ??= 0m;
        }

        // Conditional activation: only the first writer wins CommissionStartsAtUtc.
        var tracked = await _db.Companies.FirstOrDefaultAsync(c => c.Id == rootCompanyId, cancellationToken);
        if (tracked is null)
        {
            return;
        }

        if (tracked.CommissionStartsAtUtc is null)
        {
            tracked.CommissionStartsAtUtc = paidAtUtc;
            tracked.CommissionDirectRateSnapshot = Math.Max(0m, directRate!.Value);
            tracked.CommissionIndirectRateSnapshot = Math.Max(0m, indirectRate!.Value);
            tracked.CommissionDurationDaysSnapshot = durationDays is > 0
                ? durationDays.Value
                : SalesCommissionRules.DefaultCommissionDurationDays;
            tracked.CommissionYear2RateSnapshot = year2;
            tracked.CommissionYear3RateSnapshot = year3;
            tracked.CommissionIndirectSalesManagerUserId = referringSmId;
            tracked.CommissionTermsSnapshottedAtUtc = DateTime.UtcNow;
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Concurrent activation — re-read winner.
                await _db.Entry(tracked).ReloadAsync(cancellationToken);
            }
        }
        else
        {
            // Already activated — fill missing year-2/3 snapshots only.
            var dirty = false;
            if (tracked.CommissionYear2RateSnapshot is null)
            {
                tracked.CommissionYear2RateSnapshot = year2;
                dirty = true;
            }

            if (tracked.CommissionYear3RateSnapshot is null)
            {
                tracked.CommissionYear3RateSnapshot = year3;
                dirty = true;
            }

            if (dirty)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task<SalesCommissionRules.CommissionTerms?> ResolveCommissionTermsAsync(
        Guid rootCompanyId,
        Guid salesManagerUserId,
        CancellationToken cancellationToken)
    {
        var company = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == rootCompanyId)
            .Select(c => new
            {
                c.CommissionStartsAtUtc,
                c.CommissionIndirectSalesManagerUserId,
                c.CommissionDirectRateSnapshot,
                c.CommissionIndirectRateSnapshot,
                c.CommissionDurationDaysSnapshot,
                c.CommissionYear2RateSnapshot,
                c.CommissionYear3RateSnapshot,
                c.CommissionTermsSnapshottedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (company?.CommissionStartsAtUtc is null
            || company.CommissionDirectRateSnapshot is null
            || company.CommissionYear2RateSnapshot is null
            || company.CommissionYear3RateSnapshot is null)
        {
            // Activation should have filled these; avoid live settings reads (D1).
            return null;
        }

        var duration = company.CommissionDurationDaysSnapshot is > 0
            ? company.CommissionDurationDaysSnapshot.Value
            : SalesCommissionRules.DefaultCommissionDurationDays;

        return new SalesCommissionRules.CommissionTerms(
            company.CommissionDirectRateSnapshot.Value,
            company.CommissionYear2RateSnapshot.Value,
            company.CommissionYear3RateSnapshot.Value,
            company.CommissionIndirectRateSnapshot ?? 0m,
            duration,
            company.CommissionStartsAtUtc.Value);
    }

    private async Task<Guid> ResolveRootCompanyIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new { c.Id, c.ParentCompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (company is null)
        {
            return companyId;
        }

        return SalesCommercialUnit.RootIdOf(company.Id, company.ParentCompanyId);
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
}
