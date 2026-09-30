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
            2m, "X", 1.11m, 2.22m, 3.33m, 4.44m, 4000m, 1m, DateTime.UtcNow);
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
            2m, "X", 2.99m, 2.99m, 2.99m, 2.99m, 4000m, 1m, DateTime.UtcNow);
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
        JobsyDbContext db, bool isDevelopment, bool allowStub, IMollieApiClient mollie)
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
                env,
                config,
                new StubCareer(),
                new StubCompetence(),
                NullLogger<DeepAnalysisService>.Instance,
                new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db))),
            mollie,
            new StubFeatures(),
            new RealInvoices(db),
            new StubVat(),
            new StubMail(),
            env,
            config,
            NullLogger<DeepTestPaymentService>.Instance);
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

    private sealed class StubFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, false, "https://lobsy.test", DateTime.UtcNow));
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
