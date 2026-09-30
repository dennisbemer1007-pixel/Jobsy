using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Jobs;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class CompanyVerificationRulesTests
{
    [Theory]
    [InlineData(CompanyVerificationStatus.Verified, true)]
    [InlineData(CompanyVerificationStatus.Unverified, false)]
    [InlineData(CompanyVerificationStatus.Pending, false)]
    [InlineData(CompanyVerificationStatus.Rejected, false)]
    public void Gates_require_verified(CompanyVerificationStatus status, bool expected)
    {
        Assert.Equal(expected, CompanyVerificationRules.CanPublish(status));
        Assert.Equal(expected, CompanyVerificationRules.CanBuyTokens(status));
        Assert.Equal(expected, CompanyVerificationRules.CanSeeCandidates(status));
        Assert.Equal(expected, CompanyVerificationRules.CanUseWelcomeToken(status));
        Assert.Equal(expected, CompanyVerificationRules.CanUseFreePublishPromo(status));
    }
}

public class CompanyVerificationServiceTests
{
    [Fact]
    public async Task Welcome_token_not_granted_at_activation_but_once_at_verification()
    {
        await using var db = CreateDb();
        SeedFeatures(db, freeUntil: null);
        var registration = CreateRegistration(db);
        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990101", "99990101_0001", RegistrationScope.BranchOnly,
            "Verify Me", "verify.once@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPassphrase!"));
        var token = await db.CompanyRegistrations.Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken).SingleAsync();
        var activated = await registration.ActivateAsync(token!);

        Assert.False(activated.WelcomeTokenGranted);
        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.False(branch.HasReceivedWelcomeToken);
        Assert.Equal(CompanyVerificationStatus.Unverified, branch.VerificationStatus);
        Assert.Equal(0m, await BalanceAsync(db, branch.Id));

        var sut = CreateVerification(db, registration);
        await sut.MarkVerifiedAsync(
            branch.Id, CompanyVerificationMethod.BusinessEmail, activated.UserId, note: null);

        await db.Entry(branch).ReloadAsync();
        Assert.Equal(CompanyVerificationStatus.Verified, branch.VerificationStatus);
        Assert.True(branch.HasReceivedWelcomeToken);
        Assert.Equal(1m, await BalanceAsync(db, branch.Id));

        await sut.MarkVerifiedAsync(
            branch.Id, CompanyVerificationMethod.Manual, activated.UserId, note: "again");
        Assert.Equal(1m, await BalanceAsync(db, branch.Id));
    }

    [Fact]
    public async Task Welcome_token_skipped_during_promo_at_verification()
    {
        await using var db = CreateDb();
        SeedFeatures(db, FreePublishRules.DefaultUntil);
        var registration = CreateRegistration(db);
        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990102", "99990102_0001", RegistrationScope.BranchOnly,
            "Promo", "verify.promo@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPassphrase!"));
        var token = await db.CompanyRegistrations.Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken).SingleAsync();
        var activated = await registration.ActivateAsync(token!);
        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);

        var sut = CreateVerification(db, registration);
        await sut.MarkVerifiedAsync(branch.Id, CompanyVerificationMethod.Letter, activated.UserId, null);

        await db.Entry(branch).ReloadAsync();
        Assert.True(branch.HasReceivedWelcomeToken);
        Assert.False(branch.WelcomeTokenLedgerCredited);
        Assert.Equal(0m, await BalanceAsync(db, branch.Id));
    }

    [Fact]
    public async Task Ready_vacancies_publish_on_verification_when_tokens_available()
    {
        await using var db = CreateDb();
        SeedFeatures(db, freeUntil: null);
        SeedSpendCosts(db);
        var registration = CreateRegistration(db);
        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990103", "99990103_0001", RegistrationScope.BranchOnly,
            "Ready Pub", "ready.pub@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPassphrase!"));
        var token = await db.CompanyRegistrations.Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken).SingleAsync();
        var activated = await registration.ActivateAsync(token!);
        var companyId = activated.BranchCompanyId!.Value;

        // Extra tokens so welcome (1) + grant cover two publishes.
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Amount = 5m,
            Kind = TokenTransactionKind.Grant,
            OldBalance = 0,
            NewBalance = 5,
            CreatedAt = DateTime.UtcNow,
            Note = "test"
        });

        var older = await SeedDraftAsync(db, companyId, "Older klaar");
        older.PublishOnVerification = true;
        older.ReadyMarkedAtUtc = DateTime.UtcNow.AddHours(-2);
        older.ReadyMarkedByUserId = activated.UserId;
        var newer = await SeedDraftAsync(db, companyId, "Newer klaar");
        newer.PublishOnVerification = true;
        newer.ReadyMarkedAtUtc = DateTime.UtcNow.AddHours(-1);
        newer.ReadyMarkedByUserId = activated.UserId;
        await db.SaveChangesAsync();

        var sut = CreateVerification(db, registration);
        await sut.MarkVerifiedAsync(companyId, CompanyVerificationMethod.BusinessEmail, activated.UserId, null);

        await db.Entry(older).ReloadAsync();
        await db.Entry(newer).ReloadAsync();
        Assert.Equal(VacancyStatus.Active, older.Status);
        Assert.Equal(VacancyStatus.Active, newer.Status);
        Assert.False(older.PublishOnVerification);
        Assert.False(newer.PublishOnVerification);
    }

    private static CompanyVerificationService CreateVerification(
        JobsyDbContext db, CompanyRegistrationService registration)
    {
        var features = CreateFeatures(db);
        var ledger = new TokenLedgerService(db);
        var products = new VacancyProductService(
            db,
            ledger,
            new SalesCommercialService(db, ledger),
            new VacancyCategoryService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            new UserNotificationService(db),
            new CandidateActionTokenService(db),
            NullLogger<VacancyProductService>.Instance);
        return new CompanyVerificationService(
            db,
            registration,
            products,
            discovery: null,
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new UserNotificationService(db),
            features,
            NullLogger<CompanyVerificationService>.Instance);
    }

    private static CompanyRegistrationService CreateRegistration(JobsyDbContext db)
        => new(
            db,
            new TestKvkCatalog(),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            CreateFeatures(db),
            NullLogger<CompanyRegistrationService>.Instance);

    private static PlatformFeatureService CreateFeatures(JobsyDbContext db)
        => new(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PublicWebBaseUrl"] = "https://lobsy.nl"
                })
                .Build());

    private static void SeedFeatures(JobsyDbContext db, DateOnly? freeUntil)
    {
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            VacancyContentModerationEnabled = false,
            ExposeRegistrationActivationLinks = true,
            FreePublishUntil = freeUntil,
            PublicWebBaseUrl = "https://lobsy.nl",
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static void SeedSpendCosts(JobsyDbContext db)
    {
        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = 2m, IsActive = true });
        // Ensure category exists for pricing
        if (!db.VacancyCategories.Any())
        {
            foreach (var cat in VacancyCategoryDefaults.All)
            {
                db.VacancyCategories.Add(new VacancyCategory
                {
                    Id = cat.Id,
                    Slug = cat.Slug,
                    Name = cat.Name,
                    ColorHex = cat.ColorHex,
                    PublishCostTokens = cat.PublishCostTokens,
                    HighlightAvailable = cat.HighlightAvailable,
                    HighlightCostTokens = cat.HighlightCostTokens,
                    PushBomAvailable = cat.PushBomAvailable,
                    PushBomCostTokens = cat.PushBomCostTokens,
                    IsAlwaysFree = cat.IsAlwaysFree,
                    PlacementKind = cat.PlacementKind,
                    SortOrder = cat.SortOrder,
                    IsActive = true
                });
            }
        }

        db.SaveChanges();
    }

    private static async Task<Vacancy> SeedDraftAsync(JobsyDbContext db, Guid companyId, string title)
    {
        var v = new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Demo description long enough for publish validation.",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Status = VacancyStatus.Draft,
            CompanyId = companyId,
            Location = new GeoPoint(51.98, 4.22),
            RequiredTransport = TransportMode.Bike,
            CategoryId = VacancyCategoryDefaults.RegulierId,
            ContentModerationPassed = true,
            MinHoursPerWeek = 8,
            MaxHoursPerWeek = 24,
            WorkTypeLabels = "Horeca"
        };
        db.Vacancies.Add(v);
        await db.SaveChangesAsync();
        return await db.Vacancies.Include(x => x.Company).Include(x => x.Category).SingleAsync(x => x.Id == v.Id);
    }

    private static Task<decimal> BalanceAsync(JobsyDbContext db, Guid companyId)
        => db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => (decimal?)t.Amount)
            .ContinueWith(t => t.Result ?? 0m);

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class TestKvkCatalog : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
        {
            var n = kvkNumber.Trim();
            return Task.FromResult<KvkCompanyResult?>(new KvkCompanyResult(n, "Test Co", "Adres 1", []));
        }

        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
        {
            var n = kvkNumber.Trim();
            IReadOnlyList<KvkEstablishmentResult> items =
            [
                new(n, "0001", $"{n}_0001", "Test Vestiging", "Adres 1", 52, 4, false, [])
            ];
            return Task.FromResult(items);
        }

        public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => KvkEstablishmentsLookup.Ok(await GetEstablishmentsAsync(kvkNumber, cancellationToken));
    }
}

public class UnverifiedCompanyJobsTests
{
    [Fact]
    public async Task Reminder_sends_day7_and_day21_once_each()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var activated = DateTime.UtcNow.AddDays(-22);
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Reminder Co",
            KvkNumber = "11112222",
            Address = "a",
            Location = new GeoPoint(52, 5),
            VerificationStatus = CompanyVerificationStatus.Unverified,
            VerificationMethod = CompanyVerificationMethod.None,
            VerificationUpdatedAtUtc = activated
        });
        db.Users.Add(new User
        {
            Id = userId,
            Email = "bm@jobsy.local",
            FullName = "BM",
            Role = UserRole.EnterpriseManager,
            CompanyId = companyId,
            IsActive = true
        });
        db.CompanyRegistrations.Add(new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            KvkNumber = "11112222",
            KvkEstablishmentId = "11112222_0001",
            EstablishmentName = "Reminder Co",
            EstablishmentAddress = "a",
            ContactName = "BM",
            ContactEmail = "bm@jobsy.local",
            ActivationToken = "",
            Status = CompanyRegistrationStatus.Activated,
            ActivatedAt = activated,
            CreatedAt = activated,
            CreatedUserId = userId,
            CreatedBranchCompanyId = companyId
        });
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.NewGuid(),
            PublicWebBaseUrl = "https://lobsy.nl",
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var email = new CapturingEmail();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IEmailService>(email);
        services.AddSingleton<ITransactionalMailer>(email);
        services.AddSingleton<IPlatformFeatureService>(new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "https://lobsy.nl"
            }).Build()));
        await using var sp = services.BuildServiceProvider();
        var job = new UnverifiedCompanyReminderHostedService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<UnverifiedCompanyReminderHostedService>.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        await job.RunAsync(CancellationToken.None);
        Assert.Equal(2, email.Sent.Count);
        var reg = await db.CompanyRegistrations.SingleAsync();
        Assert.NotNull(reg.ReminderSentDay7AtUtc);
        Assert.NotNull(reg.ReminderSentDay21AtUtc);

        email.Sent.Clear();
        await job.RunAsync(CancellationToken.None);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Cleanup_deletes_unverified_at_day_60_and_skips_pending_manual()
    {
        await using var db = CreateDb();
        var email = new CapturingEmail();
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.NewGuid(),
            PublicWebBaseUrl = "https://lobsy.nl",
            UpdatedAtUtc = DateTime.UtcNow
        });

        var delCompany = Guid.NewGuid();
        var delUser = Guid.NewGuid();
        SeedUnverifiedRegistration(db, delCompany, delUser, "del@jobsy.local", "22223333",
            DateTime.UtcNow.AddDays(-61), CompanyVerificationStatus.Unverified);

        var pendCompany = Guid.NewGuid();
        var pendUser = Guid.NewGuid();
        SeedUnverifiedRegistration(db, pendCompany, pendUser, "pend@jobsy.local", "22224444",
            DateTime.UtcNow.AddDays(-61), CompanyVerificationStatus.Pending);

        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IEmailService>(email);
        services.AddSingleton<ITransactionalMailer>(email);
        services.AddSingleton<IPlatformFeatureService>(new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "https://lobsy.nl"
            }).Build()));
        await using var sp = services.BuildServiceProvider();
        var job = new UnverifiedCompanyCleanupHostedService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<UnverifiedCompanyCleanupHostedService>.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var deleted = await job.RunAsync(CancellationToken.None);
        Assert.Equal(1, deleted);
        Assert.False(await db.Companies.AnyAsync(c => c.Id == delCompany));
        Assert.True(await db.Companies.AnyAsync(c => c.Id == pendCompany));
        Assert.Contains(email.Sent, m => m.Category == "CompanyUnverifiedDeleted");
    }

    private static void SeedUnverifiedRegistration(
        JobsyDbContext db,
        Guid companyId,
        Guid userId,
        string email,
        string kvk,
        DateTime activated,
        CompanyVerificationStatus status)
    {
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = $"Co {kvk}",
            KvkNumber = kvk,
            KvkEstablishmentId = $"{kvk}_0001",
            Address = "a",
            Location = new GeoPoint(52, 5),
            VerificationStatus = status,
            VerificationMethod = status == CompanyVerificationStatus.Pending
                ? CompanyVerificationMethod.Manual
                : CompanyVerificationMethod.None,
            VerificationUpdatedAtUtc = activated,
            ManualVerificationOpenedAtUtc = status == CompanyVerificationStatus.Pending ? activated : null
        });
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "U",
            Role = UserRole.EnterpriseManager,
            CompanyId = companyId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany
        {
            UserId = userId,
            CompanyId = companyId
        });
        db.CompanyRegistrations.Add(new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            KvkNumber = kvk,
            KvkEstablishmentId = $"{kvk}_0001",
            EstablishmentName = $"Co {kvk}",
            EstablishmentAddress = "a",
            ContactName = "U",
            ContactEmail = email,
            ActivationToken = "",
            Status = CompanyRegistrationStatus.Activated,
            ActivatedAt = activated,
            CreatedAt = activated,
            CreatedUserId = userId,
            CreatedBranchCompanyId = companyId
        });
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
