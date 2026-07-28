using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Idempotent demo data so the SalesManager dashboard / invoices have rich content.
/// </summary>
internal static class SalesManagerDemoSeeder
{
    private const string SeedMarkerV2 = "SalesManager dashboard seed v2";

    private static readonly Guid SalesManagerUserId = Guid.Parse("aaaaaaaa-5555-5555-5555-555555555555");
    private static readonly Guid WestlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FredId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Guid LedgerFounderWestlandId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555501");
    private static readonly Guid LedgerTokenCafeId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555502");
    private static readonly Guid LedgerFounderCafeId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555503");
    private static readonly Guid LedgerFounderFredId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555504");
    private static readonly Guid LedgerTokenWestlandId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555505");
    private static readonly Guid LedgerTokenFredId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555506");
    private static readonly Guid LedgerBonusId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555507");
    private static readonly Guid LedgerPayoutWestlandId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555508");

    private static readonly Guid InvoicePaidId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555510");
    private static readonly Guid InvoicePaidLineId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555511");
    private static readonly Guid InvoiceIssuedId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555512");
    private static readonly Guid InvoiceIssuedLineId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555513");

    private static readonly Guid CheckoutWestlandId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555520");
    private static readonly Guid CheckoutCafeId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555521");
    private static readonly Guid CheckoutFredId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555522");

    public static async Task SeedAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.PlatformLogs.AnyAsync(l =>
                l.Category == "Seed" && l.Message == SeedMarkerV2))
        {
            return;
        }

        var sm = await db.Users.FirstOrDefaultAsync(u => u.Id == SalesManagerUserId || u.Email == "sales@jobsy.local");
        if (sm is null)
        {
            logger.LogWarning("SalesManager demo seed skipped: sales@jobsy.local not found.");
            return;
        }

        var profile = await db.SalesManagerProfiles.FirstOrDefaultAsync(p => p.UserId == sm.Id);
        if (profile is null || !profile.IsOnboardingComplete)
        {
            logger.LogWarning("SalesManager demo seed skipped: onboarding profile incomplete.");
            return;
        }

        var now = DateTime.UtcNow;
        await LinkReferralAsync(db, sm.Id, WestlandId, slot: 1, startedDaysAgo: 95, now);
        await LinkReferralAsync(db, sm.Id, CafeId, slot: 2, startedDaysAgo: 55, now);
        await LinkReferralAsync(db, sm.Id, FredId, slot: 3, startedDaysAgo: 28, now);

        await EnsureCheckoutAsync(db, CheckoutWestlandId, WestlandId, "demo-sm-onboarding-westland", daysAgo: 90, now);
        await EnsureCheckoutAsync(db, CheckoutCafeId, CafeId, "demo-sm-onboarding-cafe", daysAgo: 50, now);
        await EnsureCheckoutAsync(db, CheckoutFredId, FredId, "demo-sm-onboarding-fred", daysAgo: 25, now);

        await EnsureLedgerAsync(db, sm.Id, now);
        await EnsureInvoicesAsync(db, sm.Id, profile, now);

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Seed",
            Message = SeedMarkerV2,
            CreatedAt = now
        });

        await db.SaveChangesAsync();
        logger.LogInformation("SalesManager dashboard demo data seeded (v2 — rich mock).");
    }

    private static async Task LinkReferralAsync(
        JobsyDbContext db,
        Guid smId,
        Guid companyId,
        int slot,
        int startedDaysAgo,
        DateTime now)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null)
        {
            return;
        }

        company.ReferredBySalesManagerUserId ??= smId;

        if (company.FirstYearSupplierSlot is null)
        {
            var taken = await db.Companies.AnyAsync(c => c.FirstYearSupplierSlot == slot && c.Id != companyId);
            if (!taken)
            {
                company.FirstYearSupplierSlot = slot;
            }
        }

        company.FirstYearStartedAt ??= now.AddDays(-startedDaysAgo);
    }

    private static async Task EnsureCheckoutAsync(
        JobsyDbContext db,
        Guid checkoutId,
        Guid companyId,
        string paymentId,
        int daysAgo,
        DateTime now)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return;
        }

        if (await db.SupplierOnboardingCheckouts.AnyAsync(c =>
                c.Id == checkoutId
                || c.PaymentId == paymentId
                || (c.CompanyId == companyId && c.Status == SupplierOnboardingCheckoutStatus.Credited)))
        {
            return;
        }

        db.SupplierOnboardingCheckouts.Add(new SupplierOnboardingCheckout
        {
            Id = checkoutId,
            PaymentId = paymentId,
            CompanyId = companyId,
            AmountEuro = SalesCommissionRules.FirstYearOnboardingEuro,
            Status = SupplierOnboardingCheckoutStatus.Credited,
            CreatedAt = now.AddDays(-(daysAgo + 1)),
            CreditedAt = now.AddDays(-daysAgo)
        });
    }

    private static async Task EnsureLedgerAsync(JobsyDbContext db, Guid smId, DateTime now)
    {
        var founder = SalesCommissionRules.FounderBonusExVat;
        var founderVat = SalesCommissionRules.VatOn(founder);

        await EnsureLedgerEntryAsync(db, new CommissionLedgerEntry
        {
            Id = LedgerFounderWestlandId,
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.FounderBonus,
            AmountExVat = founder,
            VatAmount = founderVat,
            VatRate = SalesCommissionRules.VatRate,
            Note = "Demo founder bonus Westland Fresh Logistics",
            CompanyId = WestlandId,
            SourcePaymentId = "demo-sm-onboarding-westland",
            CreatedAt = now.AddDays(-90)
        });

        await EnsureLedgerEntryAsync(db, new CommissionLedgerEntry
        {
            Id = LedgerFounderCafeId,
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.FounderBonus,
            AmountExVat = founder,
            VatAmount = founderVat,
            VatRate = SalesCommissionRules.VatRate,
            Note = "Demo founder bonus Boutique Café De Stad",
            CompanyId = CafeId,
            SourcePaymentId = "demo-sm-onboarding-cafe",
            CreatedAt = now.AddDays(-50)
        });

        await EnsureLedgerEntryAsync(db, new CommissionLedgerEntry
        {
            Id = LedgerFounderFredId,
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.FounderBonus,
            AmountExVat = founder,
            VatAmount = founderVat,
            VatRate = SalesCommissionRules.VatRate,
            Note = "Demo founder bonus Supermarkt De Fred",
            CompanyId = FredId,
            SourcePaymentId = "demo-sm-onboarding-fred",
            CreatedAt = now.AddDays(-25)
        });

        await EnsureLedgerEntryAsync(db, new CommissionLedgerEntry
        {
            Id = LedgerTokenCafeId,
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 75m,
            VatAmount = SalesCommissionRules.VatOn(75m),
            VatRate = SalesCommissionRules.VatRate,
            Note = "Demo tokencommissie Café (jaar 1, 10%)",
            CompanyId = CafeId,
            CreatedAt = now.AddDays(-18)
        });

        await EnsureLedgerEntryAsync(db, new CommissionLedgerEntry
        {
            Id = LedgerTokenWestlandId,
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 120m,
            VatAmount = SalesCommissionRules.VatOn(120m),
            VatRate = SalesCommissionRules.VatRate,
            Note = "Demo tokencommissie Westland (highlight + PushBom)",
            CompanyId = WestlandId,
            CreatedAt = now.AddDays(-14)
        });

        await EnsureLedgerEntryAsync(db, new CommissionLedgerEntry
        {
            Id = LedgerTokenFredId,
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 42.50m,
            VatAmount = SalesCommissionRules.VatOn(42.50m),
            VatRate = SalesCommissionRules.VatRate,
            Note = "Demo tokencommissie De Fred (publicatie)",
            CompanyId = FredId,
            CreatedAt = now.AddDays(-7)
        });

        await EnsureLedgerEntryAsync(db, new CommissionLedgerEntry
        {
            Id = LedgerBonusId,
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.Adjustment,
            AmountExVat = 25m,
            VatAmount = SalesCommissionRules.VatOn(25m),
            VatRate = SalesCommissionRules.VatRate,
            Note = "Demo correctie / kick-off bonus",
            CreatedAt = now.AddDays(-5)
        });
    }

    private static async Task EnsureLedgerEntryAsync(JobsyDbContext db, CommissionLedgerEntry entry)
    {
        if (await db.CommissionLedgerEntries.AnyAsync(e => e.Id == entry.Id))
        {
            return;
        }

        // Respect unique founder-per-company / SourcePaymentId indexes.
        if (entry.Kind == CommissionEntryKind.FounderBonus && entry.CompanyId is Guid companyId)
        {
            if (await db.CommissionLedgerEntries.AnyAsync(e =>
                    e.Kind == CommissionEntryKind.FounderBonus && e.CompanyId == companyId))
            {
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.SourcePaymentId)
            && await db.CommissionLedgerEntries.AnyAsync(e => e.SourcePaymentId == entry.SourcePaymentId))
        {
            return;
        }

        if (entry.CompanyId is Guid cid && !await db.Companies.AnyAsync(c => c.Id == cid))
        {
            return;
        }

        db.CommissionLedgerEntries.Add(entry);
    }

    private static async Task EnsureInvoicesAsync(
        JobsyDbContext db,
        Guid smId,
        SalesManagerProfile profile,
        DateTime now)
    {
        var address = FormatAddress(profile);

        // Paid invoice for Westland founder (shows history + downloadable PDF).
        if (!await db.SelfBillingInvoices.AnyAsync(i => i.Id == InvoicePaidId))
        {
            var invoice = new SelfBillingInvoice
            {
                Id = InvoicePaidId,
                SalesManagerUserId = smId,
                InvoiceNumber = "SB-DEMO-2026-001",
                SalesManagerCompanyName = profile.CompanyName ?? "Demo Sales BV",
                SalesManagerKvkNumber = profile.KvkNumber ?? "87654321",
                SalesManagerVatNumber = profile.VatNumber ?? "NL87654321B01",
                SalesManagerAddress = address,
                SubtotalExVat = SalesCommissionRules.FounderBonusExVat,
                VatAmount = SalesCommissionRules.VatOn(SalesCommissionRules.FounderBonusExVat),
                TotalInclVat = SalesCommissionRules.InclVat(SalesCommissionRules.FounderBonusExVat),
                VatRate = SalesCommissionRules.VatRate,
                Status = SelfBillingInvoiceStatus.Paid,
                CreatedAt = now.AddDays(-60),
                IssuedAt = now.AddDays(-60),
                PaidAt = now.AddDays(-55)
            };
            db.SelfBillingInvoices.Add(invoice);
            db.SelfBillingInvoiceLines.Add(new SelfBillingInvoiceLine
            {
                Id = InvoicePaidLineId,
                SelfBillingInvoiceId = InvoicePaidId,
                Description = "Founder bonus Westland Fresh Logistics (demo)",
                AmountExVat = SalesCommissionRules.FounderBonusExVat,
                SourceLedgerEntryId = LedgerFounderWestlandId
            });
        }
        else
        {
            var existing = await db.SelfBillingInvoices.FirstAsync(i => i.Id == InvoicePaidId);
            if (existing.Status != SelfBillingInvoiceStatus.Paid)
            {
                existing.Status = SelfBillingInvoiceStatus.Paid;
                existing.PaidAt ??= now.AddDays(-55);
            }
        }

        var founderWestland = await db.CommissionLedgerEntries.FirstOrDefaultAsync(e => e.Id == LedgerFounderWestlandId);
        if (founderWestland is not null)
        {
            founderWestland.SelfBillingInvoiceId = InvoicePaidId;
        }

        if (!await db.CommissionLedgerEntries.AnyAsync(e => e.Id == LedgerPayoutWestlandId))
        {
            db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
            {
                Id = LedgerPayoutWestlandId,
                SalesManagerUserId = smId,
                Kind = CommissionEntryKind.Payout,
                AmountExVat = -SalesCommissionRules.FounderBonusExVat,
                VatAmount = -SalesCommissionRules.VatOn(SalesCommissionRules.FounderBonusExVat),
                VatRate = SalesCommissionRules.VatRate,
                Note = "Demo uitbetaling SB-DEMO-2026-001",
                SelfBillingInvoiceId = InvoicePaidId,
                CreatedAt = now.AddDays(-55)
            });
        }

        // Issued invoice for Fred founder (outstanding, downloadable).
        if (!await db.SelfBillingInvoices.AnyAsync(i => i.Id == InvoiceIssuedId))
        {
            var invoice = new SelfBillingInvoice
            {
                Id = InvoiceIssuedId,
                SalesManagerUserId = smId,
                InvoiceNumber = "SB-DEMO-2026-002",
                SalesManagerCompanyName = profile.CompanyName ?? "Demo Sales BV",
                SalesManagerKvkNumber = profile.KvkNumber ?? "87654321",
                SalesManagerVatNumber = profile.VatNumber ?? "NL87654321B01",
                SalesManagerAddress = address,
                SubtotalExVat = SalesCommissionRules.FounderBonusExVat,
                VatAmount = SalesCommissionRules.VatOn(SalesCommissionRules.FounderBonusExVat),
                TotalInclVat = SalesCommissionRules.InclVat(SalesCommissionRules.FounderBonusExVat),
                VatRate = SalesCommissionRules.VatRate,
                Status = SelfBillingInvoiceStatus.Issued,
                CreatedAt = now.AddDays(-10),
                IssuedAt = now.AddDays(-10)
            };
            db.SelfBillingInvoices.Add(invoice);
            db.SelfBillingInvoiceLines.Add(new SelfBillingInvoiceLine
            {
                Id = InvoiceIssuedLineId,
                SelfBillingInvoiceId = InvoiceIssuedId,
                Description = "Founder bonus Supermarkt De Fred (demo)",
                AmountExVat = SalesCommissionRules.FounderBonusExVat,
                SourceLedgerEntryId = LedgerFounderFredId
            });
        }

        var founderFred = await db.CommissionLedgerEntries.FirstOrDefaultAsync(e => e.Id == LedgerFounderFredId);
        if (founderFred is not null)
        {
            founderFred.SelfBillingInvoiceId = InvoiceIssuedId;
        }
    }

    private static string FormatAddress(SalesManagerProfile profile)
    {
        var address = string.Join(", ", new[]
        {
            profile.Address,
            $"{profile.PostalCode} {profile.City}".Trim(),
            profile.Country
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return string.IsNullOrWhiteSpace(address) ? "Voorbeeldstraat 1, Naaldwijk" : address;
    }
}
