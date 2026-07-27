using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public async Task Token_commission_year1_is_10_percent_and_idempotent()
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
        Assert.Equal(4.00m, first.AmountExVat);
        Assert.Equal(4.00m, await ledger.GetBalanceExVatAsync(smId));
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
            SalesManagerTrackingCode: "SM-TEST01"));

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
            SalesManagerTrackingCode: null));
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
                SalesManagerTrackingCode: "SM-NOPE01")));
        Assert.Contains("salesmanager-code", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await db.CompanyRegistrations.CountAsync(r => r.ContactEmail == "nova.badcode@jobsy.local"));
    }

    [Fact]
    public async Task Privacy_export_and_anonymize_cover_salesmanager_pii()
    {
        await using var db = CreateDb();
        var (smId, companyId) = await SeedReferredCompanyAsync(db, slot: 4);
        var ledger = new CommissionLedgerService(db);
        await ledger.TryCreditFounderBonusAsync(smId, companyId, "pay_privacy", 4);
        await new SelfBillingInvoiceService(db, ledger).CreateFromUninvoicedBalanceAsync(smId);

        var privacy = new PrivacyDataService(db, new StubUserLookup(db));
        var principal = CreatePrincipal("sm@jobsy.local", smId);
        var export = await privacy.ExportAsync(principal);
        var json = System.Text.Json.JsonSerializer.Serialize(export);
        Assert.Contains("NL87654321B01", json);
        Assert.Contains("CommissionLedger", json);
        Assert.Contains("SelfBillingInvoices", json);

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
    }

    private static SupplierOnboardingPaymentService CreateOnboardingService(JobsyDbContext db) =>
        new(db, new CommissionLedgerService(db), new TestHostEnvironment(),
            NullLogger<SupplierOnboardingPaymentService>.Instance);

    private static CompanyRegistrationService CreateRegistrationService(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder().Build();
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions { ExposeRegistrationActivationLinks = true }),
            config);

        return new CompanyRegistrationService(
            db,
            new Sprint7KvkAdapter(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            features,
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
            var inUse = await _db.Companies.AsNoTracking()
                .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);

            return Catalog
                .Where(c => c.KvkNumber == kvkNumber)
                .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId) })
                .ToList();
        }
    }
}
