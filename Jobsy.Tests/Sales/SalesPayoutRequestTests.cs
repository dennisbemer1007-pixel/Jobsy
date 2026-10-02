using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Sales;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.Sales;

public class SalesPayoutRequestTests
{
    [Fact]
    public async Task Preview_blocks_below_minimum_consent_profile_open_request_and_mfa()
    {
        await using var db = CreateDb();
        var (user, profile) = await SeedBeneficiaryAsync(db, withIban: false, withConsent: false);
        await AddAvailableLineAsync(db, user.Id, 32.10m);

        var sut = CreateRequestService(db);
        var preview = await sut.PreviewAsync(user.Id, mfaSatisfied: false);

        Assert.False(preview.CanRequest);
        Assert.Contains(preview.Blockers, b => b.Code == "below_minimum");
        Assert.Contains(preview.Blockers, b => b.Code == "no_consent");
        Assert.Contains(preview.Blockers, b => b.Code == "incomplete_profile");
        Assert.Contains(preview.Blockers, b => b.Code == "mfa");
    }

    [Fact]
    public async Task Request_links_all_available_lines_including_negative_corrections_and_is_idempotent()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedBeneficiaryAsync(db, withIban: true, withConsent: true);
        await AddAvailableLineAsync(db, user.Id, 1000m);
        await AddAvailableLineAsync(db, user.Id, 328.25m);
        await AddAvailableLineAsync(db, user.Id, -43.75m, CommissionEntryKind.RefundCorrection);

        var sut = CreateRequestService(db);
        var preview = await sut.PreviewAsync(user.Id, mfaSatisfied: true);
        Assert.True(preview.CanRequest);
        Assert.Equal(1284.50m, preview.AmountExVat);
        Assert.Equal(SalesCommissionRules.VatOn(1284.50m), preview.VatAmount);
        Assert.Equal(1284.50m + preview.VatAmount, preview.TotalInclVat);

        var first = await sut.RequestAsync(user.Id, mfaSatisfied: true);
        var second = await sut.RequestAsync(user.Id, mfaSatisfied: true);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(nameof(SalesPayoutRequestStatus.Requested), first.Status);

        var linked = await db.CommissionLedgerEntries
            .CountAsync(e => e.SalesPayoutRequestId == first.Id);
        Assert.Equal(3, linked);

        var open = await db.SalesPayoutRequests.CountAsync(r =>
            r.BeneficiaryUserId == user.Id && r.Status == SalesPayoutRequestStatus.Requested);
        Assert.Equal(1, open);
    }

    [Fact]
    public async Task Request_kor_has_zero_vat()
    {
        await using var db = CreateDb();
        var (user, profile) = await SeedBeneficiaryAsync(db, withIban: true, withConsent: true);
        profile.VatTreatment = SalesManagerVatTreatment.SmallBusinessScheme;
        profile.VatNumber = null;
        await db.SaveChangesAsync();
        await AddAvailableLineAsync(db, user.Id, 1284.50m);

        var sut = CreateRequestService(db);
        var preview = await sut.PreviewAsync(user.Id, mfaSatisfied: true);
        Assert.True(preview.IsKor);
        Assert.Equal(0m, preview.VatAmount);
        Assert.Equal(1284.50m, preview.TotalInclVat);

        var req = await sut.RequestAsync(user.Id, mfaSatisfied: true);
        Assert.Equal(0m, req.VatAmount);
        Assert.Equal(nameof(SalesManagerVatTreatment.SmallBusinessScheme), req.VatTreatment);
    }

    [Fact]
    public async Task Cancel_only_while_requested_unlinks_lines()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedBeneficiaryAsync(db, withIban: true, withConsent: true);
        await AddAvailableLineAsync(db, user.Id, 100m);

        var sut = CreateRequestService(db);
        var req = await sut.RequestAsync(user.Id, mfaSatisfied: true);
        await sut.CancelAsync(user.Id, req.Id);

        var cancelled = await db.SalesPayoutRequests.SingleAsync(r => r.Id == req.Id);
        Assert.Equal(SalesPayoutRequestStatus.Cancelled, cancelled.Status);
        Assert.Equal(0, await db.CommissionLedgerEntries.CountAsync(e => e.SalesPayoutRequestId == req.Id));

        // Re-request then move to InRun → cancel fails
        var again = await sut.RequestAsync(user.Id, mfaSatisfied: true);
        var tracked = await db.SalesPayoutRequests.SingleAsync(r => r.Id == again.Id);
        tracked.Status = SalesPayoutRequestStatus.InRun;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CancelAsync(user.Id, again.Id));
    }

    [Fact]
    public async Task IssueForRequest_honours_vat_and_pdf_legal_text()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedBeneficiaryAsync(db, withIban: true, withConsent: true);
        await AddAvailableLineAsync(db, user.Id, 200m);

        var sut = CreateRequestService(db);
        var reqDto = await sut.RequestAsync(user.Id, mfaSatisfied: true);
        var request = await db.SalesPayoutRequests.SingleAsync(r => r.Id == reqDto.Id);
        var consent = await db.SalesSelfBillingConsents.SingleAsync(c => c.UserId == user.Id);

        var ledger = new CommissionLedgerService(db, new AlwaysOnFeatures());
        var invoices = new SelfBillingInvoiceService(db, ledger);
        var invoice = await invoices.IssueForRequestAsync(request, consent);
        Assert.Equal(SelfBillingInvoiceStatus.Issued, invoice.Status);
        Assert.Equal(SalesManagerVatTreatment.Standard21, invoice.VatTreatment);
        Assert.Equal(SalesCommissionRules.VatOn(200m), invoice.VatAmount);
        Assert.StartsWith("SB-", invoice.InvoiceNumber);
        Assert.Equal(consent.Id, invoice.SelfBillingConsentId);
        Assert.Equal(request.Id, invoice.SalesPayoutRequestId);

        // KOR invoice
        var (user2, profile2) = await SeedBeneficiaryAsync(db, withIban: true, withConsent: true, email: "kor@test.local");
        profile2.VatTreatment = SalesManagerVatTreatment.SmallBusinessScheme;
        await db.SaveChangesAsync();
        await AddAvailableLineAsync(db, user2.Id, 100m);
        var req2 = await sut.RequestAsync(user2.Id, mfaSatisfied: true);
        var request2 = await db.SalesPayoutRequests.SingleAsync(r => r.Id == req2.Id);
        var consent2 = await db.SalesSelfBillingConsents.SingleAsync(c => c.UserId == user2.Id);
        var invoice2 = await invoices.IssueForRequestAsync(request2, consent2);
        Assert.Equal(0m, invoice2.VatAmount);
        Assert.Equal(SalesManagerVatTreatment.SmallBusinessScheme, invoice2.VatTreatment);

        var company = new PlatformCompanySettingsService(db);
        var payouts = new SalesManagerPayoutService(
            db, invoices, ledger, company, new AlwaysOnFeatures(),
            new StubHostEnvironment(),
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            NullLogger<SalesManagerPayoutService>.Instance);

        var pdf = await payouts.RenderInvoicePdfAsync(invoice.Id, user.Id);
        Assert.True(pdf.Length > 100);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);

        // Legal copy contract (QuestPDF streams are compressed; assert the source strings).
        Assert.Equal("Factuur uitgereikt door afnemer", SalesPdfInvoiceCopy.IssuedByCustomer);
        Assert.Contains("KOR", SalesPdfInvoiceCopy.VatKor);
        Assert.DoesNotContain("SELF-BILLING", SalesPdfInvoiceCopy.InvoiceTitle);
    }

    [Fact]
    public async Task Request_does_not_mark_paid()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedBeneficiaryAsync(db, withIban: true, withConsent: true);
        await AddAvailableLineAsync(db, user.Id, 1284.50m);

        var sut = CreateRequestService(db);
        var req = await sut.RequestAsync(user.Id, mfaSatisfied: true);
        Assert.Equal(nameof(SalesPayoutRequestStatus.Requested), req.Status);
        Assert.Null(req.SelfBillingInvoiceId);
        Assert.Equal(0, await db.SelfBillingInvoices.CountAsync());
        Assert.Equal(0, await db.CommissionLedgerEntries.CountAsync(e => e.Kind == CommissionEntryKind.Payout));
    }

    private static SalesPayoutRequestService CreateRequestService(JobsyDbContext db)
    {
        var wallet = new SalesWalletReadService(db);
        return new SalesPayoutRequestService(
            db,
            wallet,
            new CapturingEmail(),
            new PlatformCompanySettingsService(db),
            new AlwaysOnFeatures());
    }

    private static async Task<(User User, SalesManagerProfile Profile)> SeedBeneficiaryAsync(
        JobsyDbContext db,
        bool withIban,
        bool withConsent,
        string? email = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"sm-{Guid.NewGuid():N}@test.local",
            FullName = "Tom Hendriks",
            Role = UserRole.SalesManager,
            AuthenticatorEnabled = true
        };
        db.Users.Add(user);
        var profile = new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CompanyName = "Hendriks Sales & Advies",
            KvkNumber = "12345678",
            VatNumber = "NL123456789B01",
            Address = "Straat 1",
            PostalCode = "1234AB",
            City = "Amsterdam",
            Country = "NL",
            Iban = withIban ? "NL91ABNA0417164300" : null,
            PayoutAccountHolderName = withIban ? "T. Hendriks" : null,
            VatTreatment = SalesManagerVatTreatment.Standard21,
            OnboardingCompletedAt = DateTime.UtcNow,
            AgreementSignedAt = DateTime.UtcNow,
            TrackingCode = "SM-TEST01",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.SalesManagerProfiles.Add(profile);

        if (!await db.SalesCommercialSettings.AnyAsync())
        {
            db.SalesCommercialSettings.Add(new SalesCommercialSettings
            {
                Id = Guid.NewGuid(),
                PayoutMinimumEuro = 50m,
                CommissionHoldDays = 14,
                IbanChangeHoldDays = 3
            });
        }

        if (withConsent)
        {
            db.SalesSelfBillingConsents.Add(new SalesSelfBillingConsent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Version = SalesSelfBilling.CurrentVersion,
                TextSha256 = SalesSelfBilling.CurrentTextSha256,
                AcceptedAtUtc = DateTime.UtcNow.AddDays(-30)
            });
        }

        await db.SaveChangesAsync();
        return (user, profile);
    }

    private static async Task AddAvailableLineAsync(
        JobsyDbContext db,
        Guid userId,
        decimal amount,
        CommissionEntryKind kind = CommissionEntryKind.TokenCommission)
    {
        db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = userId,
            Kind = kind,
            AmountExVat = amount,
            VatAmount = amount > 0 ? SalesCommissionRules.VatOn(amount) : 0m,
            VatRate = 0.21m,
            AvailableFromUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Reason = kind is CommissionEntryKind.RefundCorrection or CommissionEntryKind.ChargebackCorrection
                ? "Aankoop terugbetaald"
                : null
        });
        await db.SaveChangesAsync();
    }

    private static JobsyDbContext CreateDb()
    {
        // Avoid racing with WebApplicationFactory tests that dispose a real DataProtection
        // provider after configuring the static IbanEfProtection hook.
        IbanEfProtection.Configure(new PassThroughIban());
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("sales-payout-req-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CapturingEmail : IEmailService, ITransactionalMailer
    {
        public async Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var delivery = await SendAsync(
                new EmailMessage(to, mail.Subject, mail.Html ?? string.Empty, mail.Category),
                cancellationToken);
            return new EmailSendOutcome(true, false, null, delivery.Kind);
        }

        public List<EmailMessage> Sent { get; } = [];
        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class PassThroughIban : IIbanProtector
    {
        public bool IsProtected(string? value) => false;
        public string? Protect(string? plaintext) => plaintext;
        public string? Unprotect(string? protectedPayload) => protectedPayload;
    }

    private sealed class StubHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
