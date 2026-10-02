using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class DeepAnalysisPrivacySecurityTests
{
    [Fact]
    public async Task Stub_fulfill_requires_stub_gate_and_matching_user()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        db.Users.Add(ConsentedUser(userId, "deep@test.nl", "Deep"));
        await db.SaveChangesAsync();

        var payments = CreatePayments(db, isDevelopment: true, allowStub: false);
        var checkout = await payments.CreateCheckoutAsync(
            userId, AssessmentKind.Competence, waiverAccepted: true, locale: "nl");

        Assert.Equal("pending", (await payments.GetStatusAsync(userId, checkout.CheckoutId)).Status);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => payments.GetStatusAsync(otherId, checkout.CheckoutId));

        var noop = await payments.TryFulfillAsync(checkout.CheckoutId, DeepTestFulfillSource.Return);
        Assert.Equal("pending", noop.Status);

        var production = CreatePayments(db, isDevelopment: false, allowStub: false);
        var prodNoop = await production.TryFulfillAsync(checkout.CheckoutId, DeepTestFulfillSource.Stub);
        Assert.Equal("pending", prodNoop.Status);

        Assert.True((await payments.TryFulfillAsync(checkout.CheckoutId, DeepTestFulfillSource.Stub)).Unlocked);

        var deep = CreateDeep(db);
        var state = await deep.GetStateAsync(userId, AssessmentKind.Competence);
        Assert.True(state.IsUnlocked);
    }

    [Fact]
    public async Task Create_checkout_unavailable_when_stub_and_mollie_disabled()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(ConsentedUser(userId, "nostub@test.nl", "No Stub"));
        await db.SaveChangesAsync();

        var sut = CreatePayments(db, isDevelopment: false, allowStub: false);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateCheckoutAsync(userId, AssessmentKind.Competence, true, "nl"));
        Assert.Equal(DeepTestPaymentService.PaymentsUnavailableCode, ex.Message);
    }

    [Fact]
    public async Task Empty_save_does_not_wipe_existing_answers()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "save@test.nl",
            FullName = "Save",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.CandidateDeepAnalyses.Add(new CandidateDeepAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = AssessmentKind.Competence,
            Status = CandidateDeepAnalysisStatuses.Draft,
            AnswersJson = """{"1":4,"2":3}""",
            TagsJson = "[]",
            UnlockedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateDeep(db);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SaveAsync(userId, AssessmentKind.Competence, new Dictionary<int, int>(), complete: false));

        var row = await db.CandidateDeepAnalyses.SingleAsync(d => d.UserId == userId);
        Assert.Contains("\"1\":4", row.AnswersJson);
    }

    [Fact]
    public async Task Tag_backfill_fills_empty_match_tags_for_completed_quick_scan()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var answers = Enumerable.Range(1, 25).ToDictionary(
            i => i,
            i => CompetencyTestCatalog.Questions.First(q => q.Id == i).Reverse ? 1 : 5);
        answers[21] = 5;
        answers[22] = 1;
        answers[23] = 1;
        answers[24] = 5;
        answers[25] = 1;
        var preview = CompetencyTestCatalog.Score(answers)!;
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = CompetencyTestCatalog.SerializeAnswers(answers),
            SamenwerkenPercent = preview.Samenwerken,
            ResultaatgerichtheidPercent = preview.Resultaatgerichtheid,
            StressbestendigheidPercent = preview.Stressbestendigheid,
            InnovatiePercent = preview.Innovatie,
            RiasecTagsJson = "[]",
            MatchTagsJson = "[]",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await CompetencyTagBackfillSeeder.BackfillAsync(db, NullLogger.Instance);

        var row = await db.CandidateCompetencies.SingleAsync();
        Assert.NotEqual("[]", row.MatchTagsJson);
        var career = await db.CandidateCareerInterests.SingleAsync();
        Assert.Contains(CompetencyTestCatalog.RiasecRealistic, CareerTestCatalog.ParseTagsJson(career.RiasecTagsJson));
        Assert.Contains(CompetencyTestCatalog.RiasecSocial, CareerTestCatalog.ParseTagsJson(career.RiasecTagsJson));
    }

    private static User ConsentedUser(Guid id, string email, string name) => new()
    {
        Id = id,
        Email = email,
        FullName = name,
        Role = UserRole.Candidate,
        IsActive = true,
        ConsentVersion = PrivacyConstants.CurrentConsentVersion,
        TestAiConsentAt = DateTime.UtcNow,
        TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
    };

    private static DeepAnalysisService CreateDeep(JobsyDbContext db)
    {
        return new DeepAnalysisService(
            db,
            new FlexCommercialService(db),
            new StubCareerCompass(),
            new StubCompetenceDeepReportService(),
            NullLogger<DeepAnalysisService>.Instance,
            new AssessmentSaveGuard(db, new AssessmentAdjustmentService(db)));
    }

    private static DeepTestPaymentService CreatePayments(JobsyDbContext db, bool isDevelopment, bool allowStub)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:AllowStubPayments"] = allowStub ? "true" : "false",
                ["PublicWebBaseUrl"] = "https://lobsy.test"
            })
            .Build();
        var env = new FakeHostEnvironment(isDevelopment ? Environments.Development : Environments.Production);
        return new DeepTestPaymentService(
            db,
            new FlexCommercialService(db),
            CreateDeep(db),
            new StubMollie(),
            new StubFeatures(),
            new StubInvoices(db),
            new StubVatBuffer(),
            new StubMailer(),
            env,
            config,
            NullLogger<DeepTestPaymentService>.Instance);
    }

    private sealed class StubMollie : IMollieApiClient
    {
        public Task<bool> TryGetApiKeyAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public string? ResolveWebhookUrl() => null;
        public string? ResolvePublicWebBaseUrl(string? configuredPublicWebBaseUrl) => configuredPublicWebBaseUrl;
        public Task<MolliePaymentSnapshot> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("no mollie");
        public Task<MolliePaymentSnapshot> CreatePaymentAsync(MollieCreatePaymentRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("no mollie");
        public Task TryCancelPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                VacancyContentModerationEnabled: false,
                AuthenticatorEnabled: false,
                ExposeRegistrationActivationLinks: false,
                PublicWebBaseUrl: "https://lobsy.test",
                UpdatedAtUtc: DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubInvoices(JobsyDbContext db) : IConsumerInvoiceService
    {
        public Task<ConsumerPurchaseInvoice> CreateForDeepCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken = default)
        {
            var existing = db.ConsumerPurchaseInvoices.FirstOrDefault(i => i.DeepAnalysisCheckoutId == checkoutId);
            if (existing is not null)
            {
                return Task.FromResult(existing);
            }

            var checkout = db.DeepAnalysisCheckouts.First(c => c.Id == checkoutId);
            var inv = new ConsumerPurchaseInvoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "LOB-KT-2026-0001",
                DeepAnalysisCheckoutId = checkoutId,
                UserId = checkout.UserId,
                CustomerName = "Deep",
                CustomerEmail = "deep@test.nl",
                Description = "test",
                Kind = checkout.Kind,
                AmountExVatCents = checkout.AmountExVatCents,
                VatAmountCents = checkout.VatAmountCents,
                TotalAmountCents = checkout.TotalAmountCents,
                MolliePaymentId = checkout.PaymentId,
                IssuedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.ConsumerPurchaseInvoices.Add(inv);
            checkout.InvoiceId = inv.Id;
            db.SaveChanges();
            return Task.FromResult(inv);
        }

        public Task<ConsumerPurchaseInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default)
            => Task.FromResult(db.ConsumerPurchaseInvoices.FirstOrDefault(i => i.Id == invoiceId));

        public Task<IReadOnlyList<ConsumerPurchaseInvoice>> ListAsync(
            int? year = null,
            int? quarter = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConsumerPurchaseInvoice>>(
                db.ConsumerPurchaseInvoices.OrderByDescending(i => i.IssuedAt).ToList());

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
            Jobsy.Core.Email.ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null));
    }

    private sealed class StubCompetenceDeepReportService : ICompetenceDeepReportService
    {
        public Task<CompetenceDeepReport?> GetStoredAsync(Guid userId, CancellationToken ct)
            => Task.FromResult<CompetenceDeepReport?>(null);

        public Task<CompetenceDeepReport> BuildAndStoreAsync(Guid userId, bool tryAi, CancellationToken ct)
            => Task.FromResult(new CompetenceDeepReport());

        public Task RefineAiAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StubCareerCompass : ICareerCompassGenerationService
    {
        public Task<CareerCompassSnapshot> GenerateFromCareerDeepAsync(
            IReadOnlyDictionary<int, int> answers,
            CancellationToken cancellationToken = default)
        {
            var scores = DeepAnalysisCatalog.ToRiasecScores(
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career));
            return Task.FromResult(CareerCompassBuilder.Build(scores, fromDeepAnalysis: true));
        }
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
