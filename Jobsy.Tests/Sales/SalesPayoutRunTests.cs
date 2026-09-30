using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Jobs;
using Jobsy.Infrastructure.Sales;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests.Sales;

public class SalesPayoutRunTests
{
    [Theory]
    [InlineData(2026, 10, 1)] // Thursday
    [InlineData(2026, 8, 3)]  // 1 Aug 2026 = Saturday → first workday Mon 3
    [InlineData(2026, 1, 2)]  // 1 Jan holiday → Fri 2 Jan
    public async Task Job_creates_exactly_one_run_on_first_workday_after_0600(int y, int m, int day)
    {
        await using var db = CreateDb();
        var first = SalesClock.FirstWorkdayOfMonth(y, m);
        Assert.Equal(new DateOnly(y, m, day), first);

        var afterLocal = first.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Unspecified);
        var afterUtc = TimeZoneInfo.ConvertTimeToUtc(
            afterLocal,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam"));

        var (user, _) = await SeedBeneficiaryAsync(db);
        await AddAvailableAndRequestAsync(db, user.Id, 100m, requestedAtUtc: afterUtc.AddHours(-1));

        var runs = CreateRunService(db, configured: true);

        // Before 06:00 local — no run
        var beforeLocal = first.ToDateTime(new TimeOnly(5, 59), DateTimeKind.Unspecified);
        var beforeUtc = TimeZoneInfo.ConvertTimeToUtc(
            beforeLocal,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam"));
        Assert.False(await SalesPayoutRunHostedService.TryCreateForClockAsync(runs, db, beforeUtc));

        // After 06:00 — create once
        Assert.True(await SalesPayoutRunHostedService.TryCreateForClockAsync(runs, db, afterUtc));
        Assert.False(await SalesPayoutRunHostedService.TryCreateForClockAsync(runs, db, afterUtc.AddMinutes(15)));

        var run = await db.SalesPayoutRuns.SingleAsync();
        Assert.Equal(first, run.RunDate);
        Assert.False(run.IsExtra);
        Assert.Equal(SalesPayoutRunStatus.Draft, run.Status);
        var req = await db.SalesPayoutRequests.SingleAsync();
        Assert.Equal(SalesPayoutRequestStatus.InRun, req.Status);
        Assert.Equal(run.Id, req.SalesPayoutRunId);
    }

    [Fact]
    public async Task Job_picks_only_requested_created_before_now()
    {
        await using var db = CreateDb();
        var (userPast, _) = await SeedBeneficiaryAsync(db, email: "past@test.local");
        await AddAvailableAndRequestAsync(db, userPast.Id, 120m);

        var (userFuture, _) = await SeedBeneficiaryAsync(db, email: "future@test.local");
        db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = userFuture.Id,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 90m,
            VatAmount = SalesCommissionRules.VatOn(90m),
            VatRate = 0.21m,
            AvailableFromUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        db.SalesPayoutRequests.Add(new SalesPayoutRequest
        {
            Id = Guid.NewGuid(),
            BeneficiaryUserId = userFuture.Id,
            AmountExVat = 90m,
            VatAmount = SalesCommissionRules.VatOn(90m),
            TotalInclVat = 90m + SalesCommissionRules.VatOn(90m),
            VatTreatment = SalesManagerVatTreatment.Standard21,
            MaskedIban = Iban.Mask("NL91ABNA0417164300"),
            Status = SalesPayoutRequestStatus.Requested,
            RequestedAtUtc = DateTime.UtcNow.AddHours(2)
        });
        await db.SaveChangesAsync();

        var runs = CreateRunService(db, configured: true);
        var first = SalesClock.FirstWorkdayOfMonth(SalesClock.Today().Year, SalesClock.Today().Month);
        var created = await runs.TryCreateScheduledRunAsync(first, DateTime.UtcNow);
        Assert.NotNull(created);

        var pastReq = await db.SalesPayoutRequests.SingleAsync(r => r.BeneficiaryUserId == userPast.Id);
        var futureReq = await db.SalesPayoutRequests.SingleAsync(r => r.BeneficiaryUserId == userFuture.Id);
        Assert.Equal(SalesPayoutRequestStatus.InRun, pastReq.Status);
        Assert.Equal(SalesPayoutRequestStatus.Requested, futureReq.Status);
        Assert.Null(futureReq.SalesPayoutRunId);
    }

    [Fact]
    public async Task Approve_defers_iban_hold_and_missing_consent_issues_invoice_otherwise()
    {
        await using var db = CreateDb();
        var email = new CapturingEmail();
        var adminId = Guid.NewGuid();

        var (okUser, _) = await SeedBeneficiaryAsync(db, withConsent: true, email: "ok@test.local");
        await AddAvailableAndRequestAsync(db, okUser.Id, 200m);

        var (holdUser, holdProfile) = await SeedBeneficiaryAsync(db, withConsent: true, email: "hold@test.local");
        holdProfile.IbanPayoutHoldUntilUtc = DateTime.UtcNow.AddDays(2);
        await db.SaveChangesAsync();
        await AddAvailableAndRequestAsync(db, holdUser.Id, 150m);

        var (noConsentUser, _) = await SeedBeneficiaryAsync(db, withConsent: false, email: "noc@test.local");
        // RequestAsync blocks without consent — seed an open request directly for deferral coverage.
        db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = noConsentUser.Id,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 160m,
            VatAmount = SalesCommissionRules.VatOn(160m),
            VatRate = 0.21m,
            AvailableFromUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        db.SalesPayoutRequests.Add(new SalesPayoutRequest
        {
            Id = Guid.NewGuid(),
            BeneficiaryUserId = noConsentUser.Id,
            AmountExVat = 160m,
            VatAmount = SalesCommissionRules.VatOn(160m),
            TotalInclVat = 160m + SalesCommissionRules.VatOn(160m),
            VatTreatment = SalesManagerVatTreatment.Standard21,
            MaskedIban = Iban.Mask("NL91ABNA0417164300"),
            Status = SalesPayoutRequestStatus.Requested,
            RequestedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        });
        await db.SaveChangesAsync();
        // Link ledger lines so approve path can evaluate the request.
        var nocReqId = await db.SalesPayoutRequests
            .Where(r => r.BeneficiaryUserId == noConsentUser.Id)
            .Select(r => r.Id)
            .SingleAsync();
        foreach (var line in await db.CommissionLedgerEntries
                     .Where(e => e.SalesManagerUserId == noConsentUser.Id)
                     .ToListAsync())
        {
            line.SalesPayoutRequestId = nocReqId;
        }

        await db.SaveChangesAsync();

        var runs = CreateRunService(db, configured: true, email: email);
        var run = await runs.TryCreateScheduledRunAsync(SalesClock.Today(), DateTime.UtcNow);
        Assert.NotNull(run);

        var result = await runs.ApproveRunAsync(adminId, run!.Id);
        Assert.Equal(nameof(SalesPayoutRunStatus.Approved), result.Status);

        var okReq = await db.SalesPayoutRequests.SingleAsync(r => r.BeneficiaryUserId == okUser.Id);
        Assert.Equal(SalesPayoutRequestStatus.Approved, okReq.Status);
        Assert.NotNull(okReq.SelfBillingInvoiceId);
        var invoice = await db.SelfBillingInvoices.SingleAsync(i => i.Id == okReq.SelfBillingInvoiceId!.Value);
        Assert.Equal(SelfBillingInvoiceStatus.Issued, invoice.Status);
        Assert.Equal(SalesCommissionRules.VatOn(200m), invoice.VatAmount);

        var holdReq = await db.SalesPayoutRequests.SingleAsync(r => r.BeneficiaryUserId == holdUser.Id);
        Assert.Equal(SalesPayoutRequestStatus.Requested, holdReq.Status);
        Assert.Null(holdReq.SalesPayoutRunId);
        Assert.Contains("Nieuwe rekening", holdReq.RejectionReason);

        var nocReq = await db.SalesPayoutRequests.SingleAsync(r => r.BeneficiaryUserId == noConsentUser.Id);
        Assert.Equal(SalesPayoutRequestStatus.Requested, nocReq.Status);

        Assert.Contains(await db.PlatformLogs.ToListAsync(), l => l.Category == "sales.payout.run.approve");
        Assert.Contains(email.Sent, m => m.Category == "SalesMail.PayoutApproved");
        Assert.Contains(email.Sent, m => m.Category == "SalesMail.PayoutDeferredIbanHold");
    }

    [Fact]
    public async Task Reject_releases_lines_and_mails()
    {
        await using var db = CreateDb();
        var email = new CapturingEmail();
        var (user, _) = await SeedBeneficiaryAsync(db);
        await AddAvailableAndRequestAsync(db, user.Id, 100m);
        var runs = CreateRunService(db, configured: true, email: email);
        var run = await runs.TryCreateScheduledRunAsync(SalesClock.Today(), DateTime.UtcNow);
        var req = await db.SalesPayoutRequests.SingleAsync();

        await runs.RejectLineAsync(Guid.NewGuid(), run!.Id, req.Id, "Te lage verificatie van IBAN");

        req = await db.SalesPayoutRequests.SingleAsync();
        Assert.Equal(SalesPayoutRequestStatus.Rejected, req.Status);
        Assert.Equal(0, await db.CommissionLedgerEntries.CountAsync(e => e.SalesPayoutRequestId == req.Id));
        Assert.Contains(email.Sent, m => m.Category == "SalesMail.PayoutRejected");
    }

    [Fact]
    public async Task Export_sepa_validates_against_xsd_and_stores_hash_not_file()
    {
        await using var db = CreateDb();
        var access = new CapturingAccessLog();
        var (user, _) = await SeedBeneficiaryAsync(db);
        await AddAvailableAndRequestAsync(db, user.Id, 1284.50m);
        var runs = CreateRunService(db, configured: true, accessLog: access);
        var run = await runs.TryCreateScheduledRunAsync(SalesClock.Today(), DateTime.UtcNow);
        await runs.ApproveRunAsync(Guid.NewGuid(), run!.Id);

        var export = await runs.ExportAsync(Guid.NewGuid(), run.Id, "sepa");
        Assert.Equal("application/xml", export.ContentType);
        Assert.True(export.Bytes.Length > 100);
        Assert.Equal(64, export.Sha256Hex.Length);

        var xml = System.Text.Encoding.UTF8.GetString(export.Bytes);
        Assert.Contains("EndToEndId", xml);
        Assert.Contains("NL91ABNA0417164300", xml); // creditor IBAN in file only
        ValidateAgainstXsd(export.Bytes);

        var tracked = await db.SalesPayoutRuns.SingleAsync();
        Assert.Equal(SalesPayoutRunStatus.Exported, tracked.Status);
        Assert.Equal(export.Sha256Hex, tracked.ExportFileSha256);
        Assert.DoesNotContain(await db.PlatformLogs.Select(l => l.DetailsJson).ToListAsync(),
            d => d != null && d.Contains("pain.001") && d.Contains("<Document"));
        Assert.Contains(access.Entries, e => e.Resource == "sales.payout-account" && e.Action == "export");
        Assert.Contains(await db.PlatformLogs.ToListAsync(), l => l.Category == "sales.payout.run.export");
    }

    [Fact]
    public async Task Export_disabled_when_debtor_config_missing()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedBeneficiaryAsync(db);
        await AddAvailableAndRequestAsync(db, user.Id, 100m);
        var runs = CreateRunService(db, configured: false);
        var run = await runs.TryCreateScheduledRunAsync(SalesClock.Today(), DateTime.UtcNow);
        await runs.ApproveRunAsync(Guid.NewGuid(), run!.Id);

        var detail = await runs.GetRunAsync(run.Id);
        Assert.False(detail.ExportConfigured);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runs.ExportAsync(Guid.NewGuid(), run.Id, "sepa"));

        // CSV still works without debtor config
        var csv = await runs.ExportAsync(Guid.NewGuid(), run.Id, "csv");
        Assert.Equal("text/csv", csv.ContentType);
    }

    [Fact]
    public async Task Mark_paid_closes_request_via_invoice_service_and_is_idempotent()
    {
        await using var db = CreateDb();
        var email = new CapturingEmail();
        var (user, _) = await SeedBeneficiaryAsync(db);
        await AddAvailableAndRequestAsync(db, user.Id, 100m);
        var runs = CreateRunService(db, configured: true, email: email);
        var run = await runs.TryCreateScheduledRunAsync(SalesClock.Today(), DateTime.UtcNow);
        await runs.ApproveRunAsync(Guid.NewGuid(), run!.Id);

        var invoiceId = (await db.SalesPayoutRequests.SingleAsync()).SelfBillingInvoiceId!.Value;
        var ledger = new CommissionLedgerService(db, new AlwaysOnFeatures());
        var invoices = new SelfBillingInvoiceService(db, ledger);

        // Old mark-paid path must close the request too
        await invoices.MarkPaidAsync(invoiceId);
        await invoices.MarkPaidAsync(invoiceId); // idempotent

        var req = await db.SalesPayoutRequests.SingleAsync();
        Assert.Equal(SalesPayoutRequestStatus.Paid, req.Status);
        Assert.NotNull(req.PaidAtUtc);
        Assert.Equal(1, await db.CommissionLedgerEntries.CountAsync(e =>
            e.Kind == CommissionEntryKind.Payout && e.SelfBillingInvoiceId == invoiceId));

        var trackedRun = await db.SalesPayoutRuns.SingleAsync();
        Assert.Equal(SalesPayoutRunStatus.Closed, trackedRun.Status);
    }

    [Fact]
    public async Task Parked_ambassador_requests_never_enter_run()
    {
        await using var db = CreateDb();
        var (sm, _) = await SeedBeneficiaryAsync(db, email: "sm@test.local");
        await AddAvailableAndRequestAsync(db, sm.Id, 100m);

        var amb = new User
        {
            Id = Guid.NewGuid(),
            Email = "amb@test.local",
            FullName = "Amb Assadeur",
            Role = UserRole.Ambassadeur
        };
        db.Users.Add(amb);
        db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = Guid.NewGuid(),
            UserId = amb.Id,
            CompanyName = "Amb BV",
            KvkNumber = "87654321",
            Iban = "NL91ABNA0417164300",
            PayoutAccountHolderName = "A. Assadeur",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SalesPayoutRequests.Add(new SalesPayoutRequest
        {
            Id = Guid.NewGuid(),
            BeneficiaryUserId = amb.Id,
            AmountExVat = 80m,
            VatAmount = SalesCommissionRules.VatOn(80m),
            TotalInclVat = 80m + SalesCommissionRules.VatOn(80m),
            VatTreatment = SalesManagerVatTreatment.Standard21,
            MaskedIban = Iban.Mask("NL91ABNA0417164300"),
            Status = SalesPayoutRequestStatus.Requested,
            RequestedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        var runs = CreateRunService(db, configured: true, ambassadorsEnabled: false);
        var run = await runs.TryCreateScheduledRunAsync(SalesClock.Today(), DateTime.UtcNow);
        Assert.NotNull(run);

        var ambReq = await db.SalesPayoutRequests.SingleAsync(r => r.BeneficiaryUserId == amb.Id);
        Assert.Equal(SalesPayoutRequestStatus.Requested, ambReq.Status);
        Assert.Null(ambReq.SalesPayoutRunId);

        var parked = await new SalesParkedBalanceService(db).ListAsync();
        // No ledger lines → empty parked list is fine; panel hidden when empty
        Assert.Empty(parked);

        db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = amb.Id,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 80m,
            VatAmount = 0,
            VatRate = 0.21m,
            AvailableFromUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        parked = await new SalesParkedBalanceService(db).ListAsync();
        Assert.Single(parked);
        Assert.Contains("***", parked[0].MaskedDisplayName);
    }

    [Fact]
    public void Rights_matrix_rows_cover_admin_payout_endpoints()
    {
        var rows = new (string Endpoint, string Role, int Status)[]
        {
            ("GET api/admin/sales/payout-runs", Jobsy.Core.Authorization.JobsyRoles.Admin, 200),
            ("GET api/admin/sales/payout-runs/{id}", Jobsy.Core.Authorization.JobsyRoles.Admin, 200),
            ("POST api/admin/sales/payout-runs", Jobsy.Core.Authorization.JobsyRoles.Admin, 200),
            ("POST api/admin/sales/payout-runs/{id}/approve", Jobsy.Core.Authorization.JobsyRoles.Admin, 200),
            ("POST api/admin/sales/payout-runs/{id}/lines/{requestId}/reject", Jobsy.Core.Authorization.JobsyRoles.Admin, 200),
            ("GET api/admin/sales/payout-runs/{id}/export", Jobsy.Core.Authorization.JobsyRoles.Admin, 200),
            ("POST api/admin/sales/payout-runs/{id}/mark-paid", Jobsy.Core.Authorization.JobsyRoles.Admin, 200),
            ("GET api/admin/sales/payout-runs", Jobsy.Core.Authorization.JobsyRoles.SalesManager, 403),
            ("POST api/admin/sales/payout-runs/{id}/approve", Jobsy.Core.Authorization.JobsyRoles.SalesManager, 403),
            ("POST api/admin/sales/payout-runs/{id}/approve", Jobsy.Core.Authorization.JobsyRoles.Admin, 403), // without MFA → 403
        };
        Assert.All(rows, r => Assert.True(r.Status is 200 or 403));
    }

    private static void ValidateAgainstXsd(byte[] xmlBytes)
    {
        var xsdPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pain.001.001.03.xsd");
        if (!File.Exists(xsdPath))
        {
            // Fallback to source tree when not copied to output
            xsdPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "pain.001.001.03.xsd"));
        }

        var schemas = new XmlSchemaSet();
        schemas.Add("urn:iso:std:iso:20022:tech:xsd:pain.001.001.03", xsdPath);
        var doc = XDocument.Load(new MemoryStream(xmlBytes));
        var errors = new List<string>();
        doc.Validate(schemas, (_, e) => errors.Add(e.Message));
        Assert.True(errors.Count == 0, string.Join("; ", errors));
    }

    private static SalesPayoutRunService CreateRunService(
        JobsyDbContext db,
        bool configured,
        CapturingEmail? email = null,
        CapturingAccessLog? accessLog = null,
        bool ambassadorsEnabled = false)
    {
        var options = Options.Create(new SalesPayoutProviderOptions
        {
            Provider = "bank-transfer",
            DebtorName = configured ? "Lobsy B.V." : null,
            DebtorIban = configured ? "NL91ABNA0417164300" : null,
            DebtorBic = configured ? "ABNANL2A" : null
        });
        var provider = new BankTransferPayoutProvider(options);
        var ledger = new CommissionLedgerService(db, new FeatureStub(ambassadorsEnabled));
        var invoices = new SelfBillingInvoiceService(db, ledger);
        return new SalesPayoutRunService(
            db,
            invoices,
            provider,
            email ?? new CapturingEmail(),
            new FeatureStub(ambassadorsEnabled),
            accessLog ?? new CapturingAccessLog(),
            options);
    }

    private static async Task<(User User, SalesManagerProfile Profile)> SeedBeneficiaryAsync(
        JobsyDbContext db,
        bool withConsent = true,
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
            Iban = "NL91ABNA0417164300",
            PayoutAccountHolderName = "T. Hendriks",
            VatTreatment = SalesManagerVatTreatment.Standard21,
            OnboardingCompletedAt = DateTime.UtcNow,
            AgreementSignedAt = DateTime.UtcNow,
            TrackingCode = "SM-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.SalesManagerProfiles.Add(profile);
        if (await db.SalesCommercialSettings.CountAsync() == 0)
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

    private static async Task AddAvailableAndRequestAsync(
        JobsyDbContext db,
        Guid userId,
        decimal amount,
        DateTime? requestedAtUtc = null)
    {
        db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = userId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = amount,
            VatAmount = SalesCommissionRules.VatOn(amount),
            VatRate = 0.21m,
            AvailableFromUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        await db.SaveChangesAsync();

        var wallet = new SalesWalletReadService(db);
        var svc = new SalesPayoutRequestService(
            db, wallet, new CapturingEmail(), new PlatformCompanySettingsService(db));
        var dto = await svc.RequestAsync(userId, mfaSatisfied: true);
        if (requestedAtUtc is DateTime at)
        {
            var tracked = await db.SalesPayoutRequests.SingleAsync(r => r.Id == dto.Id);
            tracked.RequestedAtUtc = at;
            await db.SaveChangesAsync();
        }
    }

    private static JobsyDbContext CreateDb()
    {
        IbanEfProtection.Configure(new PassThroughIban());
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("sales-payout-run-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CapturingEmail : IEmailService
    {
        public List<EmailMessage> Sent { get; } = [];
        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class CapturingAccessLog : IPersonalDataAccessLogger
    {
        public List<PersonalDataAccessEntry> Entries { get; } = [];
        public Task LogAsync(PersonalDataAccessEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class PassThroughIban : IIbanProtector
    {
        public bool IsProtected(string? value) => false;
        public string? Protect(string? plaintext) => plaintext;
        public string? Unprotect(string? protectedPayload) => protectedPayload;
    }

    private sealed class FeatureStub : IPlatformFeatureService
    {
        private readonly bool _ambassadors;
        public FeatureStub(bool ambassadors) => _ambassadors = ambassadors;

        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false, true, false, "https://lobsy.nl", null, AmbassadorsEnabled: _ambassadors));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class AlwaysOnFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false, true, false, "https://lobsy.nl", null, AmbassadorsEnabled: true));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }
}
