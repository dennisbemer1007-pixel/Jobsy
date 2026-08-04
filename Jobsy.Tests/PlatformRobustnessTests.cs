using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class PlatformRobustnessTests
{
    [Fact]
    public async Task Registration_continues_when_kvk_unavailable_and_marks_pending()
    {
        await using var db = CreateDb();
        var config = new ConfigurationBuilder().Build();
        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions
            {
                ExposeRegistrationActivationLinks = true
            }),
            config);

        var sut = new CompanyRegistrationService(
            db,
            new UnavailableKvkService(),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            features,
            NullLogger<CompanyRegistrationService>.Instance);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "88887777",
            "88887777_0001",
            RegistrationScope.BranchOnly,
            "Pending User",
            "pending-kvk@example.com",
            null,
            AcceptedTerms: true,
            Password: "SecurePass1!",
            AllowPendingKvkVerification: true,
            ManualEstablishmentName: "Handmatig Bedrijf",
            ManualEstablishmentAddress: "Teststraat 1, Den Haag",
            ManualEstablishmentNumber: "0001",
            ManualLatitude: 52.1,
            ManualLongitude: 4.3));

        Assert.Equal(CompanyRegistrationStatus.PendingActivation, submit.Status);
        Assert.Contains("KVK-verificatie in afwachting", submit.Message, StringComparison.OrdinalIgnoreCase);

        var registration = await db.CompanyRegistrations.SingleAsync();
        Assert.Equal(KvkVerificationStatus.Pending, registration.KvkVerificationStatus);

        var activation = await sut.ActivateAsync(registration.ActivationToken);
        var company = await db.Companies.SingleAsync(c => c.Id == activation.BranchCompanyId);
        Assert.Equal(KvkVerificationStatus.Pending, company.KvkVerificationStatus);
        Assert.Equal("Handmatig Bedrijf", company.Name);
    }

    [Fact]
    public async Task Kvk_retry_job_verifies_pending_company()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Handmatig",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_0001",
            Address = "Oud adres",
            Location = new GeoPoint(52, 4),
            KvkVerificationStatus = KvkVerificationStatus.Pending,
            KvkVerificationAttempts = 0
        });
        await db.SaveChangesAsync();

        var registration = new CompanyRegistrationService(
            db,
            new KvkServiceStub(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            new PlatformFeatureService(
                db,
                Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
                new ConfigurationBuilder().Build()),
            NullLogger<CompanyRegistrationService>.Instance);

        var retry = new KvkVerificationRetryService(
            db,
            new KvkServiceStub(db),
            registration,
            NullLogger<KvkVerificationRetryService>.Instance);

        var verified = await retry.RetryPendingAsync();
        Assert.Equal(1, verified);

        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        Assert.Equal(KvkVerificationStatus.Verified, company.KvkVerificationStatus);
        Assert.NotNull(company.KvkVerifiedAtUtc);
        Assert.Contains("Westland", company.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pending_kvk_blocks_publish()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Pending Co",
            KvkNumber = "88887777",
            KvkEstablishmentId = "88887777_0001",
            Address = "A",
            Location = new GeoPoint(52, 4),
            KvkVerificationStatus = KvkVerificationStatus.Pending
        });
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "Draft",
            Description = "d",
            Status = VacancyStatus.Draft,
            HourlyWage = 14,
            StartDate = today,
            EndDate = today.AddMonths(1),
            Location = new GeoPoint(52, 4)
        };
        db.Vacancies.Add(vacancy);
        await db.SaveChangesAsync();

        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());
        var products = new VacancyProductService(
            db,
            new TokenLedgerService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new VacancyCategoryService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            NullLogger<VacancyProductService>.Instance);

        var result = await products.PublishAsync(
            vacancy,
            new VacancyPublishOptions(false, false, false),
            actorUserId: null);

        Assert.False(result.Succeeded);
        Assert.Contains("KVK-verificatie", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Commission_snapshot_survives_admin_rate_change()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db, direct: 0.15m, indirect: 0.03m);
        var smId = Guid.NewGuid();
        var uplineId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = smId, Email = "sm@test.local", FullName = "SM", Role = UserRole.SalesManager, IsActive = true },
            new User { Id = uplineId, Email = "up@test.local", FullName = "Up", Role = UserRole.SalesManager, IsActive = true });
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = smId,
            TrackingCode = "SM-SNAP01",
            ReferredBySalesManagerUserId = uplineId,
            CanRecruitSalesManagers = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AgreementSignedAt = DateTime.UtcNow,
            OnboardingCompletedAt = DateTime.UtcNow
        });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Buyer",
            KvkNumber = "11112222",
            KvkEstablishmentId = "11112222_0001",
            Address = "A",
            Location = new GeoPoint(52, 4),
            ReferredBySalesManagerUserId = smId,
            FirstYearStartedAt = DateTime.UtcNow.AddMonths(-1),
            CommissionIndirectSalesManagerUserId = uplineId,
            CommissionDirectRateSnapshot = 0.15m,
            CommissionIndirectRateSnapshot = 0.03m,
            CommissionDurationDaysSnapshot = 365,
            CommissionTermsSnapshottedAtUtc = DateTime.UtcNow.AddMonths(-1)
        });
        await db.SaveChangesAsync();

        var settings = await db.SalesCommercialSettings.SingleAsync();
        settings.DirectCommissionRate = 0.05m;
        settings.IndirectCommissionRate = 0.01m;
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        var share = new RevenueShareService(
            db, tokens, new CommissionLedgerService(db), new SalesCommercialService(db, tokens));

        var checkoutId = Guid.NewGuid();
        await share.ApplyTokenPurchaseShareAsync(
            checkoutId, companyId, Guid.NewGuid(),
            packSize: 10, purchaseAmountExVatEuro: 100m, smId, DateTime.UtcNow.AddMonths(-1));

        var logs = await db.RevenueShareLogs.Where(l => l.TokenCheckoutId == checkoutId).ToListAsync();
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.SalesManager && l.AmountEuro == 15m);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.IndirectSalesManager && l.AmountEuro == 3m);
    }

    [Fact]
    public async Task Entra_oid_binding_survives_email_change()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Org",
            KvkNumber = "12345678",
            Address = "Straat",
            Location = new GeoPoint(52, 4)
        });
        db.Users.Add(new User
        {
            Id = userId,
            Email = "manager@corp.nl",
            FullName = "Manager",
            Role = UserRole.EnterpriseManager,
            CompanyId = companyId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = CreateAuthController(db, "secret");
        sut.ControllerContext = WithProvisionSecret("secret");

        var first = await sut.EnsureExternal(
            new EnsureExternalUserRequest("manager@corp.nl", "Manager", "entra", "oid-abc-123"),
            CancellationToken.None);
        var firstOk = Assert.IsType<OkObjectResult>(first.Result);
        var firstBody = Assert.IsType<EnsureExternalUserResponse>(firstOk.Value);
        Assert.Equal("EnterpriseManager", firstBody.Role);
        Assert.Equal(1, await db.UserExternalLogins.CountAsync());

        var second = await sut.EnsureExternal(
            new EnsureExternalUserRequest("manager.alias@corp.nl", "Manager", "entra", "oid-abc-123"),
            CancellationToken.None);
        var secondOk = Assert.IsType<OkObjectResult>(second.Result);
        var secondBody = Assert.IsType<EnsureExternalUserResponse>(secondOk.Value);
        Assert.False(secondBody.IsNewUser);
        Assert.Equal("EnterpriseManager", secondBody.Role);
        Assert.Equal("manager@corp.nl", secondBody.Email);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public void Company_tenant_scope_filters_vacancies()
    {
        using var db = CreateDb();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Companies.AddRange(
            new Company { Id = a, Name = "A", KvkNumber = "1", Address = "x", Location = new GeoPoint(52, 4) },
            new Company { Id = b, Name = "B", KvkNumber = "2", Address = "y", Location = new GeoPoint(52, 4) });
        db.Vacancies.AddRange(
            new Vacancy
            {
                Id = Guid.NewGuid(),
                CompanyId = a,
                Title = "A job",
                Description = "d",
                Status = VacancyStatus.Active,
                HourlyWage = 14,
                StartDate = today,
                EndDate = today.AddMonths(3),
                Location = new GeoPoint(52, 4)
            },
            new Vacancy
            {
                Id = Guid.NewGuid(),
                CompanyId = b,
                Title = "B job",
                Description = "d",
                Status = VacancyStatus.Active,
                HourlyWage = 14,
                StartDate = today,
                EndDate = today.AddMonths(3),
                Location = new GeoPoint(52, 4)
            });
        db.SaveChanges();

        Assert.Equal(2, db.Vacancies.Count());

        CompanyTenantScope.Enforce(db, new HashSet<Guid> { a });
        Assert.Equal(1, db.Vacancies.Count());
        Assert.Equal("A job", db.Vacancies.Single().Title);

        CompanyTenantScope.Clear(db);
        Assert.Equal(2, db.Vacancies.Count());
    }

    private static void SeedCommercialSettings(JobsyDbContext db, decimal direct, decimal indirect)
    {
        db.SalesCommercialSettings.Add(new SalesCommercialSettings
        {
            Id = SalesCommercialService.SingletonId,
            BaseTokenValueEuro = 25m,
            HighlightCarouselTokens = 2m,
            HighlightPulseTokens = 1m,
            HighlightCarouselDays = 7,
            StartHighlightBonusTokens = 2m,
            DirectCommissionRate = direct,
            IndirectCommissionRate = indirect,
            CommissionDurationDays = 365,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static AuthController CreateAuthController(JobsyDbContext db, string secret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:ExternalProvisionSecret"] = secret
            })
            .Build();
        return new AuthController(
            db,
            config,
            new IntegrationCredentialService(db, new PassthroughSecretProtector()),
            new AmbassadeurAttributionService(db, new AmbassadeurSettingsService(db), Microsoft.Extensions.Logging.Abstractions.NullLogger<AmbassadeurAttributionService>.Instance),
            new TestHostEnvironment());
    }

    private static ControllerContext WithProvisionSecret(string secret)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Jobsy-Provision-Secret"] = secret;
        return new ControllerContext { HttpContext = http };
    }

    private sealed class UnavailableKvkService : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(null);

        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => throw new Jobsy.Core.Exceptions.KvkServiceUnavailableException();

        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkEstablishmentsLookup.Unavailable());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
