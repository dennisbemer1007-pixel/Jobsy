using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class SalesManagerCommissionTests
{
    [Fact]
    public async Task Onboarding_complete_credits_founder_bonus_once_per_company()
    {
        await using var db = CreateDb();
        var (smId, companyId) = await SeedReferredCompanyAsync(db, slot: 1);
        var payments = CreateOnboardingService(db);

        var checkout1 = await payments.CreateCheckoutAsync(companyId);
        var first = await payments.CompleteCheckoutAsync(checkout1.PaymentId, null, companyId);
        Assert.True(first.CommissionCredited);
        Assert.Equal(1, first.FirstYearSupplierSlot);

        var balance = await new CommissionLedgerService(db).GetBalanceExVatAsync(smId);
        Assert.Equal(SalesCommissionRules.FounderBonusExVat, balance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payments.CreateCheckoutAsync(companyId));

        var again = await payments.CompleteCheckoutAsync(checkout1.PaymentId, null, companyId);
        Assert.True(again.CommissionCredited);
        Assert.Equal(
            1,
            await db.CommissionLedgerEntries.CountAsync(e =>
                e.Kind == CommissionEntryKind.FounderBonus && e.CompanyId == companyId));
        Assert.Equal(SalesCommissionRules.FounderBonusExVat,
            await new CommissionLedgerService(db).GetBalanceExVatAsync(smId));
    }

    [Fact]
    public async Task Onboarding_complete_rejects_company_idor_before_credit()
    {
        await using var db = CreateDb();
        var (_, companyA) = await SeedReferredCompanyAsync(db, slot: 1, kvk: "11110001");
        var (_, companyB) = await SeedReferredCompanyAsync(
            db, slot: 2, kvk: "11110002", smEmail: "sm2@jobsy.local", trackingCode: "SM-DEMO02");
        var payments = CreateOnboardingService(db);

        var checkout = await payments.CreateCheckoutAsync(companyA);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            payments.CompleteCheckoutAsync(checkout.PaymentId, null, expectedCompanyId: companyB));

        Assert.Equal(0, await db.CommissionLedgerEntries.CountAsync());
        var session = await db.SupplierOnboardingCheckouts.SingleAsync(c => c.PaymentId == checkout.PaymentId);
        Assert.NotEqual(SupplierOnboardingCheckoutStatus.Credited, session.Status);
    }

    [Fact]
    public async Task Token_commission_year1_is_15_percent_and_idempotent()
    {
        await using var db = CreateDb();
        var (smId, companyId) = await SeedReferredCompanyAsync(db, slot: 1);
        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        company.FirstYearStartedAt = DateTime.UtcNow.AddMonths(-1);
        await db.SaveChangesAsync();

        var ledger = new CommissionLedgerService(db);
        var checkoutId = Guid.NewGuid();
        var first = await ledger.TryCreditTokenCommissionAsync(
            smId, companyId, checkoutId, 40.00m, company.FirstYearStartedAt);
        var second = await ledger.TryCreditTokenCommissionAsync(
            smId, companyId, checkoutId, 40.00m, company.FirstYearStartedAt);

        Assert.NotNull(first);
        Assert.Equal(first!.Id, second!.Id);
        Assert.Equal(6.00m, first.AmountExVat); // 15% direct commission
        Assert.Equal(6.00m, await ledger.GetBalanceExVatAsync(smId));
    }

    [Fact]
    public async Task Payout_stub_creates_paid_invoice_and_platform_log()
    {
        await using var db = CreateDb();
        var (smId, companyId) = await SeedReferredCompanyAsync(db, slot: 5, kvk: "55550005");
        var ledger = new CommissionLedgerService(db);
        await ledger.TryCreditFounderBonusAsync(smId, companyId, "pay_payout_1", 5);

        var invoices = new SelfBillingInvoiceService(db, ledger);
        var company = new PlatformCompanySettingsService(db);
        var features = new FakePublicWebFeatures("https://lobsy.nl");
        var payouts = new SalesManagerPayoutService(
            db, invoices, ledger, company, features, new TestHostEnvironment(),
            new ConfigurationBuilder().Build(),
            NullLogger<SalesManagerPayoutService>.Instance);

        var preview = await payouts.GetPreviewAsync(smId);
        Assert.True(preview.CanPayout);
        Assert.Equal("NL**4300", preview.MaskedIban);
        Assert.Null(preview.Iban);
        Assert.Equal(preview.AvailableExVat, preview.AmountExVat);

        var checkout = await payouts.CreateCheckoutAsync(smId, preview.AmountExVat);
        Assert.True(checkout.IsStub);
        Assert.StartsWith("stub_payout_", checkout.PaymentId);
        Assert.StartsWith(
            "https://lobsy.nl/salesmanager/payout-checkout?paymentId=",
            checkout.CheckoutUrl);
        Assert.DoesNotContain("localhost", checkout.CheckoutUrl, StringComparison.OrdinalIgnoreCase);

        var completed = await payouts.CompleteCheckoutAsync(checkout.PaymentId, smId);
        Assert.Equal(nameof(SalesManagerPayoutCheckoutStatus.Completed), completed.Status);
        Assert.Equal(0m, await ledger.GetBalanceExVatAsync(smId));

        var invoice = await db.SelfBillingInvoices.SingleAsync(i => i.Id == completed.InvoiceId);
        Assert.Equal(SelfBillingInvoiceStatus.Paid, invoice.Status);

        var pdf = await payouts.RenderInvoicePdfAsync(invoice.Id, smId);
        Assert.True(pdf.Length > 100);
        Assert.Equal(0x25, pdf[0]); // %
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);

        var log = await db.PlatformLogs.SingleAsync(l => l.Category == "SalesManagerPayout");
        Assert.Contains("NL**4300", log.Message);
        Assert.Contains("Uitbetaling naar rekening", log.Message);

        var again = await payouts.CompleteCheckoutAsync(checkout.PaymentId, smId);
        Assert.Equal(completed.InvoiceId, again.InvoiceId);
        Assert.Equal(1, await db.SelfBillingInvoices.CountAsync(i => i.SalesManagerUserId == smId));
    }

    [Fact]
    public async Task Partial_payout_leaves_remaining_uninvoiced_balance()
    {
        await using var db = CreateDb();
        var (smId, companyId) = await SeedReferredCompanyAsync(db, slot: 6, kvk: "55550006");
        var ledger = new CommissionLedgerService(db);
        await ledger.TryCreditFounderBonusAsync(smId, companyId, "pay_partial_1", 6);

        var invoices = new SelfBillingInvoiceService(db, ledger);
        var company = new PlatformCompanySettingsService(db);
        var payouts = new SalesManagerPayoutService(
            db, invoices, ledger, company, new FakePublicWebFeatures("https://lobsy.nl"),
            new TestHostEnvironment(),
            new ConfigurationBuilder().Build(),
            NullLogger<SalesManagerPayoutService>.Instance);

        var available = await ledger.GetUninvoicedBalanceExVatAsync(smId);
        Assert.Equal(SalesCommissionRules.FounderBonusExVat, available);

        var partial = 100.00m;
        var checkout = await payouts.CreateCheckoutAsync(smId, partial);
        var completed = await payouts.CompleteCheckoutAsync(checkout.PaymentId, smId);

        var invoice = await db.SelfBillingInvoices.SingleAsync(i => i.Id == completed.InvoiceId);
        Assert.Equal(partial, invoice.SubtotalExVat);
        Assert.Equal(SelfBillingInvoiceStatus.Paid, invoice.Status);

        var remaining = await ledger.GetUninvoicedBalanceExVatAsync(smId);
        Assert.Equal(available - partial, remaining);

        var balance = await ledger.GetBalanceExVatAsync(smId);
        Assert.Equal(remaining, balance);
    }

    [Fact]
    public async Task Self_billing_invoice_then_mark_paid_is_idempotent()
    {
        await using var db = CreateDb();
        var (smId, companyId) = await SeedReferredCompanyAsync(db, slot: 3);
        var ledger = new CommissionLedgerService(db);
        await ledger.TryCreditFounderBonusAsync(smId, companyId, "pay_test_1", 3);

        var invoices = new SelfBillingInvoiceService(db, ledger);
        var invoice = await invoices.CreateFromUninvoicedBalanceAsync(smId);
        Assert.Equal(SelfBillingInvoiceStatus.Issued, invoice.Status);
        Assert.Equal(SalesCommissionRules.FounderBonusExVat, invoice.SubtotalExVat);

        var paid1 = await invoices.MarkPaidAsync(invoice.Id);
        var paid2 = await invoices.MarkPaidAsync(invoice.Id);
        Assert.Equal(SelfBillingInvoiceStatus.Paid, paid1.Status);
        Assert.Equal(paid1.Id, paid2.Id);
        Assert.Equal(
            1,
            await db.CommissionLedgerEntries.CountAsync(e =>
                e.Kind == CommissionEntryKind.Payout && e.SelfBillingInvoiceId == invoice.Id));
        Assert.Equal(0m, await ledger.GetBalanceExVatAsync(smId));
    }

    [Fact]
    public async Task Registration_with_tracking_code_links_supplier_and_reserves_slot()
    {
        await using var db = CreateDb();
        var smId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = smId,
            Email = "sm.track@jobsy.local",
            FullName = "SM Track",
            Role = UserRole.SalesManager,
            IsActive = true
        });
        var now = DateTime.UtcNow;
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = smId,
            CompanyName = "SM BV",
            KvkNumber = "12345678",
            VatNumber = "NL123456789B01",
            Address = "A 1",
            PostalCode = "1234AB",
            City = "Delft",
            Country = "NL",
            TrackingCode = "SM-TEST01",
            AgreementSignedAt = now,
            AgreementVersion = SalesCommissionRules.CurrentAgreementVersion,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();

        var registration = CreateRegistrationService(db);
        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990001",
            "99990001_0001",
            RegistrationScope.BranchOnly,
            "Nova",
            "nova.sm@jobsy.local",
            null,
            AcceptedTerms: true,
            SalesManagerTrackingCode: "SM-TEST01",
            Password: "TestPass1!"));

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var activated = await registration.ActivateAsync(token);

        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.Equal(smId, branch.ReferredBySalesManagerUserId);
        Assert.Equal(1, branch.FirstYearSupplierSlot);
        Assert.NotNull(branch.FirstYearStartedAt);
    }

    [Fact]
    public async Task Registration_rejects_unknown_tracking_code_but_allows_empty()
    {
        await using var db = CreateDb();
        var registration = CreateRegistrationService(db);

        var emptyOk = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990001",
            "99990001_0001",
            RegistrationScope.BranchOnly,
            "Nova",
            "nova.emptycode@jobsy.local",
            null,
            AcceptedTerms: true,
            SalesManagerTrackingCode: null,
            Password: "TestPass1!"));
        Assert.Equal(CompanyRegistrationStatus.PendingActivation, emptyOk.Status);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => registration.SubmitAsync(
            new RegistrationSubmitRequest(
                "99990001",
                "99990001_0001",
                RegistrationScope.BranchOnly,
                "Nova",
                "nova.badcode@jobsy.local",
                null,
                AcceptedTerms: true,
                SalesManagerTrackingCode: "SM-NOPE01",
                Password: "TestPass1!")));
        Assert.Contains("trackingcode", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await db.CompanyRegistrations.CountAsync(r => r.ContactEmail == "nova.badcode@jobsy.local"));
    }

    [Fact]
    public async Task Registration_with_partner_tracking_code_links_company_as_pending_referral()
    {
        await using var db = CreateDb();
        var partnerCompanyId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = partnerCompanyId,
            Name = "BM Org",
            KvkNumber = "11112222",
            KvkEstablishmentId = "11112222_0001",
            Address = "A",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Users.Add(new User
        {
            Id = partnerId,
            Email = "bm.partner@jobsy.local",
            FullName = "BM Partner",
            Role = UserRole.EnterpriseManager,
            CompanyId = partnerCompanyId,
            IsActive = true
        });
        db.PartnerAffiliateProfiles.Add(new PartnerAffiliateProfile
        {
            Id = Guid.NewGuid(),
            UserId = partnerId,
            TrackingCode = "BM-TEST23",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var registration = CreateRegistrationService(db);
        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990001",
            "99990001_0001",
            RegistrationScope.BranchOnly,
            "Nova",
            "nova.bm@jobsy.local",
            null,
            AcceptedTerms: true,
            PartnerTrackingCode: "BM-TEST23",
            Password: "TestPass1!"));

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var activated = await registration.ActivateAsync(token);

        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.Equal(partnerId, branch.ReferredByPartnerUserId);
        Assert.Null(branch.ReferredBySalesManagerUserId);
        Assert.Equal(PartnerReferralStatus.Pending, branch.PartnerReferralStatus);
        Assert.True(branch.WelcomeTokenLedgerCredited);
        Assert.Equal(1m, await new TokenLedgerService(db).GetBalanceAsync(branch.Id));

        var partners = CreatePartnerAffiliateService(db);
        var mine = await partners.GetMineAsync(partnerId);
        Assert.NotNull(mine);
        Assert.Equal(1, mine!.ReferredCompanyCount);
        Assert.Equal(1, mine.PendingReferralCount);
        Assert.Equal(0m, mine.ReferralTokensEarned);
        Assert.Contains(mine.Referrals, r => r.StatusLabel == "Welkomsttoken nog beschikbaar");
    }

    [Fact]
    public async Task Partner_referral_rewards_half_token_once_on_welcome_spend()
    {
        await using var db = CreateDb();
        var (partnerId, partnerCompanyId, referredCompanyId) = await SeedPartnerAffiliateAsync(db);

        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.Publish,
            CostTokens = 1m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var services = new ServiceCollection()
            .AddScoped<IPartnerAffiliateService>(_ => CreatePartnerAffiliateService(db))
            .BuildServiceProvider();
        var ledger = new TokenLedgerService(db, services);

        var spend = await ledger.TrySpendAsync(referredCompanyId, TokenSpendReason.Publish);
        Assert.True(spend.Succeeded);

        var referred = await db.Companies.SingleAsync(c => c.Id == referredCompanyId);
        Assert.Equal(PartnerReferralStatus.Rewarded, referred.PartnerReferralStatus);
        Assert.NotNull(referred.PartnerReferralRewardedAtUtc);
        Assert.Equal(0.5m, await ledger.GetBalanceAsync(partnerCompanyId));

        // Second spend must not double-credit.
        await ledger.GrantAsync(referredCompanyId, 1m, note: "top-up");
        var second = await ledger.TrySpendAsync(referredCompanyId, TokenSpendReason.Publish);
        Assert.True(second.Succeeded);
        Assert.Equal(0.5m, await ledger.GetBalanceAsync(partnerCompanyId));

        var mine = await CreatePartnerAffiliateService(db).GetMineAsync(partnerId);
        Assert.Equal(0.5m, mine!.ReferralTokensEarned);
        Assert.Equal(1, mine.RewardedReferralCount);
        Assert.Contains(mine.Referrals, r => r.StatusLabel == "Actief - Bonus toegekend");
    }

    [Fact]
    public async Task Partner_referral_does_not_reward_without_welcome_ledger_credit()
    {
        await using var db = CreateDb();
        var (_, partnerCompanyId, referredCompanyId) = await SeedPartnerAffiliateAsync(db);
        var referred = await db.Companies.SingleAsync(c => c.Id == referredCompanyId);
        referred.WelcomeTokenLedgerCredited = false;
        await db.SaveChangesAsync();

        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.Publish,
            CostTokens = 1m,
            IsActive = true
        });
        await ledgerGrantAndSpendAsync(db, referredCompanyId, partnerCompanyId);

        referred = await db.Companies.SingleAsync(c => c.Id == referredCompanyId);
        Assert.Equal(PartnerReferralStatus.Pending, referred.PartnerReferralStatus);
        Assert.Equal(0m, await new TokenLedgerService(db).GetBalanceAsync(partnerCompanyId));
    }

    [Fact]
    public async Task Partner_affiliate_rejects_same_kvk_self_referral()
    {
        await using var db = CreateDb();
        var partnerId = Guid.NewGuid();
        var partnerCompanyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = partnerCompanyId,
            Name = "Partner Org",
            KvkNumber = "55556666",
            KvkEstablishmentId = "55556666_0001",
            Address = "A",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Users.Add(new User
        {
            Id = partnerId,
            Email = "self.partner@jobsy.local",
            FullName = "Self Partner",
            Role = UserRole.EnterpriseManager,
            CompanyId = partnerCompanyId,
            IsActive = true
        });
        db.PartnerAffiliateProfiles.Add(new PartnerAffiliateProfile
        {
            Id = Guid.NewGuid(),
            UserId = partnerId,
            TrackingCode = "BM-SELF23",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var partners = CreatePartnerAffiliateService(db);
        var target = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Same KVK co",
            KvkNumber = "55556666",
            KvkEstablishmentId = "55556666_0002",
            Address = "B",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        };
        Assert.False(await partners.ApplyReferralAsync(target, "BM-SELF23"));
        Assert.Null(target.ReferredByPartnerUserId);
    }

    private static async Task ledgerGrantAndSpendAsync(
        JobsyDbContext db,
        Guid referredCompanyId,
        Guid partnerCompanyId)
    {
        var services = new ServiceCollection()
            .AddScoped<IPartnerAffiliateService>(_ => CreatePartnerAffiliateService(db))
            .BuildServiceProvider();
        var ledger = new TokenLedgerService(db, services);
        await ledger.GrantAsync(referredCompanyId, 1m, note: "manual");
        var spend = await ledger.TrySpendAsync(referredCompanyId, TokenSpendReason.Publish);
        Assert.True(spend.Succeeded);
        _ = partnerCompanyId;
    }

    [Fact]
    public async Task Privacy_export_and_anonymize_cover_salesmanager_pii()
    {
        await using var db = CreateDb();
        var (smId, companyId) = await SeedReferredCompanyAsync(db, slot: 4);
        var ledger = new CommissionLedgerService(db);
        await ledger.TryCreditFounderBonusAsync(smId, companyId, "pay_privacy", 4);
        await new SelfBillingInvoiceService(db, ledger).CreateFromUninvoicedBalanceAsync(smId);

        var privacy = new PrivacyDataService(
            db,
            new StubUserLookup(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance));
        var principal = CreatePrincipal("sm@jobsy.local", smId);
        var export = await privacy.ExportAsync(principal);
        var json = System.Text.Json.JsonSerializer.Serialize(export);
        Assert.Contains("NL87654321B01", json);
        Assert.Contains("CommissionLedger", json);
        Assert.Contains("SelfBillingInvoices", json);
        Assert.Contains("SalesManagerPayouts", json);

        // Seed an application snapshot + site visit to assert Art. 17 completeness.
        var vacancyId = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Privacy test vacature",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = smId,
            CandidateName = "Demo SM",
            CandidateEmail = "sm@jobsy.local",
            PreferredTransport = "Bike",
            Status = ApplicationStatus.Pending,
            SnapshotAboutMe = "Persoonlijke bio",
            SnapshotDrivingLicenses = "B",
            EmailVerificationCode = VerificationCodes.Hash("123456"),
            DistanceKm = 4.2,
            CreatedAt = DateTime.UtcNow
        });
        db.SiteVisits.Add(new SiteVisit
        {
            Id = Guid.NewGuid(),
            UserId = smId,
            Path = "/salesmanager",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await privacy.DeleteOrAnonymizeAsync(principal);

        var profile = await db.SalesManagerProfiles.SingleAsync(p => p.UserId == smId);
        Assert.Null(profile.VatNumber);
        Assert.Null(profile.Iban);
        Assert.Null(profile.TrackingCode);
        Assert.Equal("Verwijderde salesmanager", profile.CompanyName);

        var user = await db.Users.SingleAsync(u => u.Id == smId);
        Assert.False(user.IsActive);
        Assert.StartsWith("deleted-", user.Email);

        var invoice = await db.SelfBillingInvoices.SingleAsync(i => i.SalesManagerUserId == smId);
        Assert.Equal("ANON", invoice.SalesManagerVatNumber);
        Assert.Equal("Geanonimiseerd", invoice.SalesManagerAddress);

        var app = await db.Applications.SingleAsync(a => a.VacancyId == vacancyId);
        Assert.Null(app.SnapshotAboutMe);
        Assert.Null(app.SnapshotDrivingLicenses);
        Assert.Null(app.EmailVerificationCode);
        Assert.Null(app.DistanceKm);
        Assert.Equal(0, await db.SiteVisits.CountAsync(v => v.UserId == smId));
    }

    private static SupplierOnboardingPaymentService CreateOnboardingService(JobsyDbContext db) =>
        new(db, new CommissionLedgerService(db), new TestHostEnvironment(),
            NullLogger<SupplierOnboardingPaymentService>.Instance);

    private static PartnerAffiliateService CreatePartnerAffiliateService(JobsyDbContext db) =>
        new(
            db,
            new TokenLedgerService(db),
            new FakePublicWebFeatures("https://lobsy.nl"));

    private static SalesManagerPayoutService CreatePayoutService(
        JobsyDbContext db,
        CommissionLedgerService ledger)
    {
        var invoices = new SelfBillingInvoiceService(db, ledger);
        return new SalesManagerPayoutService(
            db,
            invoices,
            ledger,
            new PlatformCompanySettingsService(db),
            new FakePublicWebFeatures("https://lobsy.nl"),
            new TestHostEnvironment(),
            new ConfigurationBuilder().Build(),
            NullLogger<SalesManagerPayoutService>.Instance);
    }

    private static CompanyRegistrationService CreateRegistrationService(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder().Build();
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions { ExposeRegistrationActivationLinks = true }),
            config);

        if (!db.PlatformFeatureSettings.Any())
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                ExposeRegistrationActivationLinks = true,
                FreePublishUntil = null,
                UpdatedAtUtc = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        return new CompanyRegistrationService(
            db,
            new Sprint7KvkAdapter(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            features,
            CreatePartnerAffiliateService(db),
            NullLogger<CompanyRegistrationService>.Instance);
    }

    private static async Task<(Guid SmId, Guid CompanyId)> SeedReferredCompanyAsync(
        JobsyDbContext db,
        int slot,
        string kvk = "55550001",
        string smEmail = "sm@jobsy.local",
        string trackingCode = "SM-DEMO01")
    {
        var smId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = smId,
            Email = smEmail,
            FullName = "Demo SM",
            Role = UserRole.SalesManager,
            IsActive = true
        });
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = smId,
            CompanyName = "Demo Sales BV",
            KvkNumber = "87654321",
            VatNumber = "NL87654321B01",
            Address = "Voorbeeldstraat 1",
            PostalCode = "2671AB",
            City = "Naaldwijk",
            Country = "NL",
            Iban = "NL91ABNA0417164300",
            TrackingCode = trackingCode,
            AgreementSignedAt = now,
            AgreementVersion = SalesCommissionRules.CurrentAgreementVersion,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Referred Co",
            KvkNumber = kvk,
            KvkEstablishmentId = $"{kvk}_0001",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer,
            ReferredBySalesManagerUserId = smId,
            FirstYearSupplierSlot = slot,
            FirstYearStartedAt = now
        });
        await db.SaveChangesAsync();
        return (smId, companyId);
    }

    private static async Task<(Guid PartnerId, Guid PartnerCompanyId, Guid ReferredCompanyId)> SeedPartnerAffiliateAsync(
        JobsyDbContext db)
    {
        var partnerId = Guid.NewGuid();
        var partnerCompanyId = Guid.NewGuid();
        var referredCompanyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Companies.Add(new Company
        {
            Id = partnerCompanyId,
            Name = "Partner Org",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_0001",
            Address = "Partnerstraat 1",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Users.Add(new User
        {
            Id = partnerId,
            Email = "partner@jobsy.local",
            FullName = "Partner User",
            Role = UserRole.EnterpriseManager,
            CompanyId = partnerCompanyId,
            IsActive = true
        });
        db.PartnerAffiliateProfiles.Add(new PartnerAffiliateProfile
        {
            Id = Guid.NewGuid(),
            UserId = partnerId,
            CompanyName = "Partner BV",
            KvkNumber = "12345678",
            TrackingCode = "BM-PART23",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Companies.Add(new Company
        {
            Id = referredCompanyId,
            Name = "Partner referred co",
            KvkNumber = "99998888",
            KvkEstablishmentId = "99998888_0001",
            Address = "Klantstraat 1",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer,
            ReferredByPartnerUserId = partnerId,
            PartnerReferralStatus = PartnerReferralStatus.Pending,
            PartnerReferredAtUtc = now,
            WelcomeTokenLedgerCredited = true,
            HasReceivedWelcomeToken = true,
            FirstYearStartedAt = now
        });
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = referredCompanyId,
            Amount = 1m,
            Kind = TokenTransactionKind.Grant,
            OldBalance = 0m,
            NewBalance = 1m,
            Note = CompanyRegistrationService.WelcomeTokenNote,
            CreatedAt = now
        });
        await db.SaveChangesAsync();
        return (partnerId, partnerCompanyId, referredCompanyId);
    }

    private static ClaimsPrincipal CreatePrincipal(string email, Guid userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, nameof(UserRole.SalesManager))
        ], "test");
        return new ClaimsPrincipal(identity);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubUserLookup(JobsyDbContext db) : IUserLookupService
    {
        public async Task<User?> FindByPrincipalAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }
    }

    private sealed class FakePublicWebFeatures(string publicWebBaseUrl) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false, false, false, publicWebBaseUrl, DateTime.UtcNow, 120));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class Sprint7KvkAdapter : IKvkService
    {
        private readonly JobsyDbContext _db;

        private static readonly KvkEstablishmentResult[] Catalog =
        [
            new("99990001", "0001", "99990001_0001", "Nova Branch", "Straat 1", 52, 4, false)
        ];

        public Sprint7KvkAdapter(JobsyDbContext db) => _db = db;

        public Task<KvkCompanyResult?> GetByKvkNumberAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var match = Catalog.FirstOrDefault(c => c.KvkNumber == kvkNumber);
            return Task.FromResult(match is null
                ? null
                : new KvkCompanyResult(match.KvkNumber, match.Name, match.Address));
        }

        public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var lookup = await LookupEstablishmentsAsync(kvkNumber, cancellationToken);
            return lookup.Establishments;
        }

        public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var inUse = await _db.Companies.AsNoTracking()
                .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);

            var items = Catalog
                .Where(c => c.KvkNumber == kvkNumber)
                .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId) })
                .ToList();
            return items.Count == 0
                ? KvkEstablishmentsLookup.NotFound()
                : KvkEstablishmentsLookup.Ok(items);
        }
    }
}
