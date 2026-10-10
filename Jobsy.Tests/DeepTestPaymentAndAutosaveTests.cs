using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class DeepAnalysisPricingTests
{
    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Values)]
    [InlineData(AssessmentKind.Culture)]
    public void For_returns_per_kind_price(AssessmentKind kind)
    {
        var dto = new FlexCommercialSettingsDto(
            2m, "X", 1.11m, 2.22m, 3.33m, 4.44m, 4000m, 1m, 0.5m, null, 1m, DateTime.UtcNow);
        var price = DeepAnalysisPricing.For(dto, kind);
        Assert.Equal(kind switch
        {
            AssessmentKind.Career => 2.22m,
            AssessmentKind.Values => 3.33m,
            AssessmentKind.Culture => 4.44m,
            _ => 1.11m
        }, price);
    }

    [Fact]
    public void Split_returns_cents_incl_ex_vat()
    {
        var dto = new FlexCommercialSettingsDto(
            2m, "X", 2.99m, 2.99m, 2.99m, 2.99m, 4000m, 1m, 0.5m, null, 1m, DateTime.UtcNow);
        var (ex, vat, total) = DeepAnalysisPricing.Split(dto, AssessmentKind.Competence);
        Assert.Equal(299, total);
        Assert.Equal(ex + vat, total);
        Assert.True(vat > 0);
    }
}

public class NoRawExceptionMessageInTestPagesTests
{
    [Theory]
    [InlineData("Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Candidate/CareerTest.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Candidate/CultureScan.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Candidate/ValuesScan.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Candidate/DeepAnalysisCheckout.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Candidate/TestDetail.razor")]
    public void Razor_pages_do_not_bind_ex_Message(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var path = Path.Combine(root, relativePath);
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
    }
}

public class DeepTestStubGuardTests
{
    [Fact]
    public void Render_yaml_production_disables_stub_payments()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var yaml = File.ReadAllText(Path.Combine(root, "render.yaml"));
        Assert.Contains("JobsyAuth__AllowStubPayments", yaml);
        Assert.Contains("\"false\"", yaml);
    }

    [Fact]
    public async Task Production_create_never_returns_stub_without_mollie()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(Consented(userId));
        await db.SaveChangesAsync();

        var sut = CreatePayments(db, isDevelopment: false, allowStub: false, mollie: new NoKeyMollie());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateCheckoutAsync(userId, AssessmentKind.Competence, true, "nl"));
        Assert.Equal(DeepTestPaymentService.PaymentsUnavailableCode, ex.Message);
    }

    [Fact]
    public async Task Stub_fulfill_is_noop_in_production()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(Consented(userId));
        var checkoutId = Guid.NewGuid();
        db.DeepAnalysisCheckouts.Add(new DeepAnalysisCheckout
        {
            Id = checkoutId,
            UserId = userId,
            Kind = AssessmentKind.Competence,
            PaymentId = "stub_deep_" + Guid.NewGuid().ToString("N"),
            AmountEuro = 2.99m,
            AmountExVatCents = 247,
            VatAmountCents = 52,
            TotalAmountCents = 299,
            Status = DeepAnalysisCheckoutStatus.Pending,
            IsStub = true,
            WaiverAcceptedAtUtc = DateTime.UtcNow,
            WaiverTextVersion = "2026-09",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var prod = CreatePayments(db, isDevelopment: false, allowStub: false, mollie: new NoKeyMollie());
        var result = await prod.TryFulfillAsync(checkoutId, DeepTestFulfillSource.Stub);
        Assert.Equal("pending", result.Status);
        Assert.False(result.Unlocked);
    }

    [Fact]
    public async Task Flagged_test_account_unlocks_for_free_without_mollie()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var user = Consented(userId);
        user.Email = "sanne@example.com";
        user.IsTestAccount = true;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mollie = new RecordingMollie();
        var sut = CreatePayments(db, isDevelopment: false, allowStub: false, mollie: mollie);
        var created = await sut.CreateCheckoutAsync(userId, AssessmentKind.Competence, true, "nl");

        Assert.Equal(0, mollie.Creates);
        Assert.Equal(0, created.TotalCents);
        Assert.StartsWith("/candidate/deep-analysis/checkout?checkoutId=", created.CheckoutUrl, StringComparison.Ordinal);

        var checkout = await db.DeepAnalysisCheckouts.SingleAsync();
        Assert.Equal(DeepAnalysisCheckoutStatus.Paid, checkout.Status);
        Assert.Equal(0, checkout.AmountEuro);
        Assert.Equal(0, checkout.AmountExVatCents);
        Assert.Equal(0, checkout.VatAmountCents);
        Assert.Equal(0, checkout.TotalAmountCents);
        Assert.Equal(DeepTestFinanceRules.TestUnlockMethod, checkout.PaymentMethod);
        Assert.StartsWith("test_deep_", checkout.PaymentId, StringComparison.Ordinal);
        Assert.True(CandidateDeepAnalysisStatuses.IsUnlocked(
            (await db.CandidateDeepAnalyses.SingleAsync()).Status));
        Assert.Empty(db.ConsumerPurchaseInvoices);

        var replay = await sut.TryFulfillAsync(checkout.Id, DeepTestFulfillSource.Webhook);
        Assert.True(replay.Unlocked);
        Assert.Empty(db.ConsumerPurchaseInvoices);
    }

    [Fact]
    public async Task Unflagged_test_email_still_starts_a_mollie_payment()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var user = Consented(userId);
        user.Email = "test-kandidaat@lobsy.nl";
        user.IsTestAccount = false;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mollie = new RecordingMollie();
        var sut = CreatePayments(db, isDevelopment: false, allowStub: false, mollie: mollie, freeForEveryone: false);
        var created = await sut.CreateCheckoutAsync(userId, AssessmentKind.Career, true, "nl");

        Assert.Equal(1, mollie.Creates);
        Assert.Equal(299, created.TotalCents);
        Assert.StartsWith("https://pay.mollie.test/", created.CheckoutUrl, StringComparison.Ordinal);
        var checkout = await db.DeepAnalysisCheckouts.SingleAsync();
        Assert.Equal(DeepAnalysisCheckoutStatus.Pending, checkout.Status);
        Assert.False(DeepTestFinanceRules.IsTestUnlock(checkout.PaymentMethod));
        Assert.False(DeepTestFinanceRules.IsFreeForEveryoneUnlock(checkout.PaymentMethod));
        Assert.Empty(db.CandidateDeepAnalyses);
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Values)]
    [InlineData(AssessmentKind.Culture)]
    public async Task Free_for_everyone_unlocks_every_deep_test_without_mollie(AssessmentKind kind)
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var user = Consented(userId);
        user.IsTestAccount = false;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mollie = new RecordingMollie();
        var sut = CreatePayments(db, isDevelopment: false, allowStub: false, mollie: mollie, freeForEveryone: true);

        Assert.Equal(DeepTestFinanceRules.FreeForEveryoneMethod, await sut.GetPaymentModeAsync());

        var created = await sut.CreateCheckoutAsync(userId, kind, waiverAccepted: false, locale: "nl");

        Assert.Equal(0, mollie.Creates);
        Assert.Equal(0, created.TotalCents);
        var checkout = await db.DeepAnalysisCheckouts.SingleAsync();
        Assert.Equal(DeepAnalysisCheckoutStatus.Paid, checkout.Status);
        Assert.Equal(0, checkout.AmountEuro);
        Assert.Equal(DeepTestFinanceRules.FreeForEveryoneMethod, checkout.PaymentMethod);
        Assert.StartsWith("free_deep_", checkout.PaymentId, StringComparison.Ordinal);
        Assert.True(CandidateDeepAnalysisStatuses.IsUnlocked(
            (await db.CandidateDeepAnalyses.SingleAsync()).Status));
        Assert.Empty(db.ConsumerPurchaseInvoices);

        var replay = await sut.TryFulfillAsync(checkout.Id, DeepTestFulfillSource.Webhook);
        Assert.True(replay.Unlocked);
        Assert.Empty(db.ConsumerPurchaseInvoices);
    }

    [Fact]
    public async Task Gratis_unlock_stays_out_of_vat_totals()
    {
        await using var db = CreateDb();
        var issued = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc);
        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(2.99m);
        var buyerId = Guid.NewGuid();
        db.Users.Add(Consented(buyerId));
        var realCheckout = Guid.NewGuid();
        var freeCheckout = Guid.NewGuid();
        db.DeepAnalysisCheckouts.Add(PaidCheckout(realCheckout, buyerId, "tr_real", ex, vat, total, "ideal", issued));
        db.DeepAnalysisCheckouts.Add(PaidCheckout(freeCheckout, buyerId, "free_deep_x", 0, 0, 0, DeepTestFinanceRules.FreeForEveryoneMethod, issued));
        db.ConsumerPurchaseInvoices.Add(Invoice(realCheckout, buyerId, "LOB-KT-2026-0011", ex, vat, total, "ideal", issued));
        db.ConsumerPurchaseInvoices.Add(Invoice(freeCheckout, buyerId, "LOB-KT-2026-0012", 0, 0, 0, DeepTestFinanceRules.FreeForEveryoneMethod, issued));
        await db.SaveChangesAsync();

        var preview = await new VatDeclarationService(db, new PlatformCompanySettingsService(db))
            .PreviewAsync(2026, 2);
        Assert.Equal(1, preview.ConsumerInvoiceCount);
        Assert.Equal(total, preview.ConsumerOmzetExVatCents + preview.ConsumerVatCents);
    }

    [Fact]
    public async Task Test_unlock_does_not_change_vat_or_revenue_totals()
    {
        await using var db = CreateDb();
        var issued = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc);
        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(2.99m);
        var buyerId = Guid.NewGuid();
        db.Users.Add(Consented(buyerId));
        var realCheckout = Guid.NewGuid();
        var testCheckout = Guid.NewGuid();
        db.DeepAnalysisCheckouts.Add(PaidCheckout(realCheckout, buyerId, "tr_real", ex, vat, total, "ideal", issued));
        db.DeepAnalysisCheckouts.Add(PaidCheckout(testCheckout, buyerId, "test_deep_x", 0, 0, 0, DeepTestFinanceRules.TestUnlockMethod, issued));
        db.ConsumerPurchaseInvoices.Add(Invoice(realCheckout, buyerId, "LOB-KT-2026-0001", ex, vat, total, "ideal", issued));
        db.ConsumerPurchaseInvoices.Add(Invoice(testCheckout, buyerId, "LOB-KT-2026-0002", 0, 0, 0, DeepTestFinanceRules.TestUnlockMethod, issued));
        await db.SaveChangesAsync();

        var preview = await new VatDeclarationService(db, new PlatformCompanySettingsService(db))
            .PreviewAsync(2026, 2);
        Assert.Equal(1, preview.ConsumerInvoiceCount);
        Assert.Equal(ex, preview.ConsumerOmzetExVatCents);
        Assert.Equal(vat, preview.ConsumerVatCents);

        var listed = await new ConsumerInvoiceService(db, new PlatformCompanySettingsService(db)).ListAsync(2026, 2);
        var only = Assert.Single(listed);
        Assert.Equal(total, only.TotalAmountCents);

        var summary = await new AdminFinanceSummaryService(db).GetAsync("year");
        Assert.Equal(0, summary.RevenueInclVatCents);
        Assert.Equal(0, summary.RevenueExVatCents);
    }

    private static DeepAnalysisCheckout PaidCheckout(
        Guid id, Guid userId, string paymentId, int ex, int vat, int total, string method, DateTime issued)
        => new()
        {
            Id = id,
            UserId = userId,
            Kind = AssessmentKind.Competence,
            PaymentId = paymentId,
            AmountEuro = total / 100m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            PaymentMethod = method,
            IsStub = method == DeepTestFinanceRules.TestUnlockMethod,
            Status = DeepAnalysisCheckoutStatus.Paid,
            PaidAtUtc = issued,
            CreatedAtUtc = issued,
            WaiverAcceptedAtUtc = issued,
            WaiverTextVersion = "2026-09"
        };

    private static ConsumerPurchaseInvoice Invoice(
        Guid checkoutId, Guid userId, string number, int ex, int vat, int total, string method, DateTime issued)
        => new()
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = number,
            DeepAnalysisCheckoutId = checkoutId,
            UserId = userId,
            CustomerName = "K",
            CustomerEmail = "k@example.com",
            Description = "Uitgebreide test",
            Kind = AssessmentKind.Competence,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            PaymentMethod = method,
            MolliePaymentId = method == DeepTestFinanceRules.TestUnlockMethod ? "test_deep_x" : "tr_real",
            IsStub = method == DeepTestFinanceRules.TestUnlockMethod,
            IssuedAt = issued,
            CreatedAt = issued
        };

    private static User Consented(Guid id) => new()
    {
        Id = id,
        Email = "stub@test.nl",
        FullName = "Stub",
        Role = UserRole.Candidate,
        IsActive = true,
        ConsentVersion = Jobsy.Core.Privacy.PrivacyConstants.CurrentConsentVersion,
        TestAiConsentAt = DateTime.UtcNow,
        TestAiConsentVersion = Jobsy.Core.Privacy.PrivacyConstants.CandidateProfilingConsentVersion
    };

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static DeepTestPaymentService CreatePayments(
        JobsyDbContext db, bool isDevelopment, bool allowStub, IMollieApiClient mollie, bool freeForEveryone = false)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:AllowStubPayments"] = allowStub ? "true" : "false",
                ["PublicWebBaseUrl"] = "https://lobsy.test"
            })
            .Build();
        var env = new FakeHost(isDevelopment ? Environments.Development : Environments.Production);
        return new DeepTestPaymentService(
            db,
            new FlexCommercialService(db),
            new DeepAnalysisService(
                db,
                new FlexCommercialService(db),
                new StubCareer(),
                new StubCompetence(),
                NullLogger<DeepAnalysisService>.Instance,
                new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db))),
            mollie,
            new StubFeatures(freeForEveryone),
            new RealInvoices(db),
            new StubVat(),
            new StubMail(),
            env,
            config,
            NullLogger<DeepTestPaymentService>.Instance);
    }

    private sealed class RecordingMollie : IMollieApiClient
    {
        public int Creates { get; private set; }

        public Task<bool> TryGetApiKeyAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public string? ResolveWebhookUrl() => "https://lobsy.test/hook";

        public string? ResolvePublicWebBaseUrl(string? configuredPublicWebBaseUrl) => configuredPublicWebBaseUrl;

        public Task<MolliePaymentSnapshot> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
            => Task.FromResult(new MolliePaymentSnapshot(paymentId, "open", null, "2.99", null, null, null, new Dictionary<string, string>()));

        public Task<MolliePaymentSnapshot> CreatePaymentAsync(MollieCreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            Creates++;
            return Task.FromResult(new MolliePaymentSnapshot(
                "tr_real", "open", null, request.AmountValue, null, null, "https://pay.mollie.test/x", new Dictionary<string, string>()));
        }

        public Task TryCancelPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoKeyMollie : IMollieApiClient
    {
        public Task<bool> TryGetApiKeyAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public string? ResolveWebhookUrl() => null;
        public string? ResolvePublicWebBaseUrl(string? configuredPublicWebBaseUrl) => configuredPublicWebBaseUrl;
        public Task<MolliePaymentSnapshot> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("no");
        public Task<MolliePaymentSnapshot> CreatePaymentAsync(MollieCreatePaymentRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("no");
        public Task TryCancelPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubFeatures(bool freeForEveryone = false) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false, false, "https://lobsy.test", DateTime.UtcNow,
                FreeCandidateTestsEnabled: freeForEveryone));
        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RealInvoices(JobsyDbContext db) : IConsumerInvoiceService
    {
        public Task<ConsumerPurchaseInvoice> CreateForDeepCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken = default)
            => new ConsumerInvoiceService(db, new PlatformCompanySettingsService(db))
                .CreateForDeepCheckoutAsync(checkoutId, cancellationToken);
        public Task<ConsumerPurchaseInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default)
            => Task.FromResult(db.ConsumerPurchaseInvoices.FirstOrDefault(i => i.Id == invoiceId));
        public Task<IReadOnlyList<ConsumerPurchaseInvoice>> ListAsync(int? year = null, int? quarter = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConsumerPurchaseInvoice>>(db.ConsumerPurchaseInvoices.ToList());
        public Task<byte[]> RenderPdfAsync(Guid invoiceId, string? culture = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Array.Empty<byte>());
    }

    private sealed class StubVat : IVatBufferTransferService
    {
        public Task<VatBufferTransfer> QueueForInvoiceAsync(TokenPurchaseInvoice invoice, CancellationToken cancellationToken = default)
            => Task.FromResult(new VatBufferTransfer { Id = Guid.NewGuid(), InvoiceNumber = invoice.InvoiceNumber });
        public Task<VatBufferTransfer> QueueForInvoiceAsync(ConsumerPurchaseInvoice invoice, CancellationToken cancellationToken = default)
            => Task.FromResult(new VatBufferTransfer { Id = Guid.NewGuid(), InvoiceNumber = invoice.InvoiceNumber });
        public Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<VatBufferTransfer>> ListAsync(int? year = null, int? quarter = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<VatBufferTransfer>>([]);
    }

    private sealed class StubMail : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            Jobsy.Core.Email.ComposedEmail mail, string to, EmailSendOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null));
    }

    private sealed class StubCompetence : ICompetenceDeepReportService
    {
        public Task<Jobsy.Core.Reports.Competence.CompetenceDeepReport?> GetStoredAsync(Guid userId, CancellationToken ct)
            => Task.FromResult<Jobsy.Core.Reports.Competence.CompetenceDeepReport?>(null);
        public Task<Jobsy.Core.Reports.Competence.CompetenceDeepReport> BuildAndStoreAsync(Guid userId, bool tryAi, CancellationToken ct)
            => Task.FromResult(new Jobsy.Core.Reports.Competence.CompetenceDeepReport());
        public Task RefineAiAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StubCareer : ICareerCompassGenerationService
    {
        public Task<CareerCompassSnapshot> GenerateFromCareerDeepAsync(
            IReadOnlyDictionary<int, int> answers, CancellationToken cancellationToken = default)
        {
            var scores = DeepAnalysisCatalog.ToRiasecScores(
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career));
            return Task.FromResult(CareerCompassBuilder.Build(scores, fromDeepAnalysis: true));
        }
    }

    private sealed class FakeHost(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

public class ConsumerInvoiceNumberTests
{
    [Fact]
    public async Task Invoice_number_uses_LOB_KT_series()
    {
        await using var db = CreateDb();
        db.PlatformCompanySettings.Add(new PlatformCompanySettings
        {
            Id = PlatformCompanySettingsService.SingletonId,
            CompanyName = "Lobsy",
            KvkNumber = "1",
            VatNumber = "NL1",
            Address = "a",
            PostalCode = "1000AA",
            City = "Amsterdam"
        });
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "inv@test.nl",
            FullName = "Inv",
            Role = UserRole.Candidate,
            IsActive = true
        });
        var checkoutId = Guid.NewGuid();
        db.DeepAnalysisCheckouts.Add(new DeepAnalysisCheckout
        {
            Id = checkoutId,
            UserId = userId,
            Kind = AssessmentKind.Career,
            PaymentId = "tr_x",
            AmountEuro = 2.99m,
            AmountExVatCents = 247,
            VatAmountCents = 52,
            TotalAmountCents = 299,
            Status = DeepAnalysisCheckoutStatus.Paid,
            PaidAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            WaiverAcceptedAtUtc = DateTime.UtcNow,
            WaiverTextVersion = "2026-09"
        });
        await db.SaveChangesAsync();

        var sut = new ConsumerInvoiceService(db, new PlatformCompanySettingsService(db));
        var inv = await sut.CreateForDeepCheckoutAsync(checkoutId);
        Assert.StartsWith($"LOB-KT-{DateTime.UtcNow.Year}-", inv.InvoiceNumber);
        Assert.Equal(299, inv.TotalAmountCents);
        Assert.Equal(AssessmentKind.Career, inv.Kind);

        var again = await sut.CreateForDeepCheckoutAsync(checkoutId);
        Assert.Equal(inv.Id, again.Id);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
