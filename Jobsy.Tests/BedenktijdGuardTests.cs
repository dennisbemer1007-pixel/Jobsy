using System.Security.Claims;
using Jobsy.Api.Controllers;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Legal;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

/// <summary>
/// Public-pages 05 (bedenktijd, 05.2a "Present" path): nobody can start a deep-analysis checkout
/// without <c>waiverAccepted == true</c>, and the stored waiver version is one source of truth with
/// <see cref="LegalDocumentVersions.Terms"/> / <c>Terms.Waiver.Checkbox</c>.
/// </summary>
public class BedenktijdGuardTests
{
    [Fact]
    public async Task Service_rejects_checkout_without_waiver_and_makes_no_mollie_call()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(ConsentedUser(userId));
        await db.SaveChangesAsync();

        var mollie = new CountingMollie();
        var sut = CreatePayments(db, mollie);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateCheckoutAsync(userId, AssessmentKind.Competence, waiverAccepted: false, locale: "nl"));

        Assert.Equal(DeepTestPaymentService.WaiverRequiredCode, ex.Message);
        Assert.Equal(0, mollie.CreateCalls);
        Assert.Empty(db.DeepAnalysisCheckouts);
    }

    [Fact]
    public async Task Service_accepts_checkout_with_waiver_and_stores_terms_version()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(ConsentedUser(userId));
        await db.SaveChangesAsync();

        var mollie = new CountingMollie();
        var sut = CreatePayments(db, mollie);

        var result = await sut.CreateCheckoutAsync(
            userId, AssessmentKind.Competence, waiverAccepted: true, locale: "nl");

        Assert.Equal(0, mollie.CreateCalls); // stub mode: no real Mollie call either
        var checkout = await db.DeepAnalysisCheckouts.SingleAsync(c => c.Id == result.CheckoutId);
        Assert.Equal(LegalDocumentVersions.Terms.Version, checkout.WaiverTextVersion);
        Assert.NotEqual(default, checkout.WaiverAcceptedAtUtc);
    }

    [Fact]
    public void Waiver_text_version_follows_terms_version_not_a_literal()
    {
        Assert.Equal(LegalDocumentVersions.Terms.Version, DeepAnalysisPricing.WaiverTextVersion);
        Assert.NotEqual("2026-09", DeepAnalysisPricing.WaiverTextVersion);
    }

    [Fact]
    public async Task Controller_returns_400_waiver_required_without_touching_checkout_or_mollie()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(ConsentedUser(userId));
        await db.SaveChangesAsync();

        var mollie = new CountingMollie();
        var controller = CreateController(db, mollie, userId);

        var response = await controller.Checkout("competence", new DeepTestCheckoutRequest(false, "nl"), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        var code = badRequest.Value!.GetType().GetProperty("code")!.GetValue(badRequest.Value);
        Assert.Equal("waiver_required", code);
        Assert.Equal(0, mollie.CreateCalls);
        Assert.Empty(db.DeepAnalysisCheckouts);
    }

    [Fact]
    public async Task Controller_accepts_checkout_when_waiver_is_ticked()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(ConsentedUser(userId));
        await db.SaveChangesAsync();

        var mollie = new CountingMollie();
        var controller = CreateController(db, mollie, userId);

        var response = await controller.Checkout("competence", new DeepTestCheckoutRequest(true, "nl"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(response.Result);
        Assert.Single(db.DeepAnalysisCheckouts);
    }

    [Fact]
    public async Task Controller_defaults_to_waiver_required_when_body_is_missing()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(ConsentedUser(userId));
        await db.SaveChangesAsync();

        var mollie = new CountingMollie();
        var controller = CreateController(db, mollie, userId);

        var response = await controller.Checkout("competence", request: null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        var code = badRequest.Value!.GetType().GetProperty("code")!.GetValue(badRequest.Value);
        Assert.Equal("waiver_required", code);
        Assert.Empty(db.DeepAnalysisCheckouts);
    }

    private static User ConsentedUser(Guid id) => new()
    {
        Id = id,
        Email = "waiver@test.nl",
        FullName = "Waiver Test",
        Role = UserRole.Candidate,
        IsActive = true,
        ConsentVersion = PrivacyConstants.CurrentConsentVersion,
        TestAiConsentAt = DateTime.UtcNow,
        TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
    };

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static DeepTestPaymentService CreatePayments(JobsyDbContext db, IMollieApiClient mollie)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:AllowStubPayments"] = "true",
                ["PublicWebBaseUrl"] = "https://lobsy.test"
            })
            .Build();
        var env = new FakeHostEnvironment(Environments.Development);
        return new DeepTestPaymentService(
            db,
            new FlexCommercialService(db),
            CreateDeep(db, config, env),
            mollie,
            new StubFeatures(),
            new StubInvoices(db),
            new StubVatBuffer(),
            new StubMailer(),
            env,
            config,
            NullLogger<DeepTestPaymentService>.Instance);
    }

    private static DeepAnalysisService CreateDeep(JobsyDbContext db, IConfiguration config, IHostEnvironment env)
        => new(
            db,
            new FlexCommercialService(db),
            env,
            config,
            new StubCareerCompass(),
            new StubCompetenceDeepReportService(),
            NullLogger<DeepAnalysisService>.Instance,
            new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));

    private static DeepAnalysisController CreateController(JobsyDbContext db, IMollieApiClient mollie, Guid userId)
    {
        var payments = CreatePayments(db, mollie);
        var deep = CreateDeep(db, new ConfigurationBuilder().Build(), new FakeHostEnvironment(Environments.Development));
        var controller = new DeepAnalysisController(
            deep,
            payments,
            new StubInvoices(db),
            new StubReportPdf(),
            new FixedUserLookup(db, userId),
            db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
        return controller;
    }

    private sealed class FixedUserLookup(JobsyDbContext db, Guid userId) : IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            => db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    private sealed class StubReportPdf : IAssessmentReportPdfService
    {
        public Task<AssessmentReportPdf?> TryRenderAsync(Guid userId, AssessmentKind kind, CancellationToken cancellationToken = default)
            => Task.FromResult<AssessmentReportPdf?>(null);
        public Task<AssessmentReportPdf?> TryRenderAsync(Guid userId, AssessmentKind kind, string? lang, CancellationToken cancellationToken = default)
            => Task.FromResult<AssessmentReportPdf?>(null);
    }

    private sealed class CountingMollie : IMollieApiClient
    {
        public int CreateCalls { get; private set; }
        public Task<bool> TryGetApiKeyAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public string? ResolveWebhookUrl() => null;
        public string? ResolvePublicWebBaseUrl(string? configuredPublicWebBaseUrl) => configuredPublicWebBaseUrl;
        public Task<MolliePaymentSnapshot> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("no mollie in guard tests");
        public Task<MolliePaymentSnapshot> CreatePaymentAsync(MollieCreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            throw new InvalidOperationException("no mollie in guard tests");
        }
        public Task TryCancelPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, false, "https://lobsy.test", DateTime.UtcNow));
        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubInvoices(JobsyDbContext db) : IConsumerInvoiceService
    {
        public Task<ConsumerPurchaseInvoice> CreateForDeepCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken = default)
        {
            var checkout = db.DeepAnalysisCheckouts.First(c => c.Id == checkoutId);
            var invoice = new ConsumerPurchaseInvoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"LOB-KT-{DateTime.UtcNow.Year}-TEST",
                UserId = checkout.UserId,
                DeepAnalysisCheckoutId = checkoutId,
                Kind = checkout.Kind,
                TotalAmountCents = checkout.TotalAmountCents,
                IssuedAt = DateTime.UtcNow
            };
            db.ConsumerPurchaseInvoices.Add(invoice);
            return Task.FromResult(invoice);
        }
        public Task<ConsumerPurchaseInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default)
            => Task.FromResult(db.ConsumerPurchaseInvoices.FirstOrDefault(i => i.Id == invoiceId));
        public Task<IReadOnlyList<ConsumerPurchaseInvoice>> ListAsync(int? year = null, int? quarter = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConsumerPurchaseInvoice>>(db.ConsumerPurchaseInvoices.ToList());
        public Task<byte[]> RenderPdfAsync(Guid invoiceId, string? culture = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Array.Empty<byte>());
    }

    private sealed class StubVatBuffer : IVatBufferTransferService
    {
        public Task<VatBufferTransfer> QueueForInvoiceAsync(TokenPurchaseInvoice invoice, CancellationToken cancellationToken = default)
            => Task.FromResult(new VatBufferTransfer { Id = Guid.NewGuid(), InvoiceNumber = invoice.InvoiceNumber });
        public Task<VatBufferTransfer> QueueForInvoiceAsync(ConsumerPurchaseInvoice invoice, CancellationToken cancellationToken = default)
            => Task.FromResult(new VatBufferTransfer { Id = Guid.NewGuid(), InvoiceNumber = invoice.InvoiceNumber });
        public Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<VatBufferTransfer>> ListAsync(int? year = null, int? quarter = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<VatBufferTransfer>>([]);
    }

    private sealed class StubMailer : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            Jobsy.Core.Email.ComposedEmail mail, string to, EmailSendOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null));
    }

    private sealed class StubCompetenceDeepReportService : ICompetenceDeepReportService
    {
        public Task<Jobsy.Core.Reports.Competence.CompetenceDeepReport?> GetStoredAsync(Guid userId, CancellationToken ct)
            => Task.FromResult<Jobsy.Core.Reports.Competence.CompetenceDeepReport?>(null);
        public Task<Jobsy.Core.Reports.Competence.CompetenceDeepReport> BuildAndStoreAsync(Guid userId, bool tryAi, CancellationToken ct)
            => Task.FromResult(new Jobsy.Core.Reports.Competence.CompetenceDeepReport());
        public Task RefineAiAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StubCareerCompass : ICareerCompassGenerationService
    {
        public Task<CareerCompassSnapshot> GenerateFromCareerDeepAsync(
            IReadOnlyDictionary<int, int> answers, CancellationToken cancellationToken = default)
        {
            var scores = DeepAnalysisCatalog.ToRiasecScores(
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career));
            return Task.FromResult(CareerCompassBuilder.Build(scores, fromDeepAnalysis: true));
        }
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
