using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Idempotent demo data so the SalesManager dashboard / invoices have non-empty content.
/// </summary>
internal static class SalesManagerDemoSeeder
{
    private static readonly Guid SalesManagerUserId = Guid.Parse("aaaaaaaa-5555-5555-5555-555555555555");
    private static readonly Guid WestlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid LedgerFounderId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555501");
    private static readonly Guid LedgerTokenId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555502");
    private static readonly Guid InvoiceId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555510");
    private static readonly Guid CheckoutId = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555520");

    public static async Task SeedAsync(JobsyDbContext db, ILogger logger)
    {
        if (await db.PlatformLogs.AnyAsync(l =>
                l.Category == "Seed" && l.Message == "SalesManager dashboard seed"))
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
        await LinkReferralAsync(db, sm.Id, WestlandId, slot: 1, now);
        await LinkReferralAsync(db, sm.Id, CafeId, slot: 2, now);
        await EnsureCheckoutAsync(db, WestlandId, now);
        await EnsureLedgerAsync(db, sm.Id, WestlandId, CafeId, profile, now);
        await EnsureInvoiceAsync(db, sm.Id, profile, now);

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Seed",
            Message = "SalesManager dashboard seed",
            CreatedAt = now
        });

        await db.SaveChangesAsync();
        logger.LogInformation("SalesManager dashboard demo data seeded.");
    }

    private static async Task LinkReferralAsync(JobsyDbContext db, Guid smId, Guid companyId, int slot, DateTime now)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null)
        {
            return;
        }

        if (company.ReferredBySalesManagerUserId is null)
        {
            company.ReferredBySalesManagerUserId = smId;
        }

        if (company.FirstYearSupplierSlot is null)
        {
            // Avoid unique-slot collisions with other seed data.
            var taken = await db.Companies.AnyAsync(c => c.FirstYearSupplierSlot == slot && c.Id != companyId);
            if (!taken)
            {
                company.FirstYearSupplierSlot = slot;
            }
        }

        company.FirstYearStartedAt ??= now.AddDays(-40);
    }

    private static async Task EnsureCheckoutAsync(JobsyDbContext db, Guid companyId, DateTime now)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId))
        {
            return;
        }

        if (await db.SupplierOnboardingCheckouts.AnyAsync(c =>
                c.CompanyId == companyId && c.Status == SupplierOnboardingCheckoutStatus.Credited))
        {
            return;
        }

        if (await db.SupplierOnboardingCheckouts.AnyAsync(c => c.Id == CheckoutId))
        {
            return;
        }

        db.SupplierOnboardingCheckouts.Add(new SupplierOnboardingCheckout
        {
            Id = CheckoutId,
            PaymentId = "demo-sm-onboarding-westland",
            CompanyId = companyId,
            AmountEuro = 2500m,
            Status = SupplierOnboardingCheckoutStatus.Credited,
            CreatedAt = now.AddDays(-35),
            CreditedAt = now.AddDays(-34)
        });
    }

    private static async Task EnsureLedgerAsync(
        JobsyDbContext db,
        Guid smId,
        Guid westlandId,
        Guid cafeId,
        SalesManagerProfile profile,
        DateTime now)
    {
        if (!await db.CommissionLedgerEntries.AnyAsync(e => e.Id == LedgerFounderId)
            && await db.Companies.AnyAsync(c => c.Id == westlandId))
        {
            db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
            {
                Id = LedgerFounderId,
                SalesManagerUserId = smId,
                Kind = CommissionEntryKind.FounderBonus,
                AmountExVat = 500m,
                VatAmount = 105m,
                VatRate = 0.21m,
                Note = "Demo founder bonus Westland",
                CompanyId = westlandId,
                SourcePaymentId = "demo-sm-onboarding-westland",
                CreatedAt = now.AddDays(-34)
            });
        }

        if (!await db.CommissionLedgerEntries.AnyAsync(e => e.Id == LedgerTokenId)
            && await db.Companies.AnyAsync(c => c.Id == cafeId))
        {
            db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
            {
                Id = LedgerTokenId,
                SalesManagerUserId = smId,
                Kind = CommissionEntryKind.TokenCommission,
                AmountExVat = 75m,
                VatAmount = 15.75m,
                VatRate = 0.21m,
                Note = "Demo tokencommissie Café",
                CompanyId = cafeId,
                CreatedAt = now.AddDays(-12)
            });
        }

        _ = profile; // profile already validated by caller
    }

    private static async Task EnsureInvoiceAsync(
        JobsyDbContext db,
        Guid smId,
        SalesManagerProfile profile,
        DateTime now)
    {
        if (await db.SelfBillingInvoices.AnyAsync(i => i.Id == InvoiceId || i.SalesManagerUserId == smId))
        {
            return;
        }

        var address = string.Join(", ", new[]
        {
            profile.Address,
            $"{profile.PostalCode} {profile.City}".Trim(),
            profile.Country
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var invoice = new SelfBillingInvoice
        {
            Id = InvoiceId,
            SalesManagerUserId = smId,
            InvoiceNumber = "SB-DEMO-2026-001",
            SalesManagerCompanyName = profile.CompanyName ?? "Demo Sales BV",
            SalesManagerKvkNumber = profile.KvkNumber ?? "87654321",
            SalesManagerVatNumber = profile.VatNumber ?? "NL87654321B01",
            SalesManagerAddress = string.IsNullOrWhiteSpace(address) ? "Voorbeeldstraat 1, Naaldwijk" : address,
            SubtotalExVat = 500m,
            VatAmount = 105m,
            TotalInclVat = 605m,
            VatRate = 0.21m,
            Status = SelfBillingInvoiceStatus.Issued,
            CreatedAt = now.AddDays(-20),
            IssuedAt = now.AddDays(-20)
        };
        db.SelfBillingInvoices.Add(invoice);
        db.SelfBillingInvoiceLines.Add(new SelfBillingInvoiceLine
        {
            Id = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555511"),
            SelfBillingInvoiceId = InvoiceId,
            Description = "Founder bonus Westland (demo)",
            AmountExVat = 500m,
            SourceLedgerEntryId = LedgerFounderId
        });

        var founder = await db.CommissionLedgerEntries.FirstOrDefaultAsync(e => e.Id == LedgerFounderId);
        if (founder is not null)
        {
            founder.SelfBillingInvoiceId = InvoiceId;
        }
    }
}
