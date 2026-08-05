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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class FreePublishRulesTests
{
    [Theory]
    [InlineData("2026-11-18", "2026-11-18", true)]
    [InlineData("2026-11-18", "2026-11-17", true)]
    [InlineData("2026-11-18", "2026-11-19", false)]
    [InlineData(null, "2026-11-01", false)]
    public void IsActive_respects_inclusive_end_date(string? untilRaw, string nowRaw, bool expected)
    {
        DateOnly? until = untilRaw is null ? null : DateOnly.Parse(untilRaw);
        var now = DateTime.Parse(nowRaw + "T12:00:00Z").ToUniversalTime();
        Assert.Equal(expected, FreePublishRules.IsActive(until, now));
    }

    [Fact]
    public void EffectivePublishCost_zeros_during_promo_only()
    {
        Assert.Equal(0m, FreePublishRules.EffectivePublishCost(1m, FreePublishRules.DefaultUntil, new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(1m, FreePublishRules.EffectivePublishCost(1m, FreePublishRules.DefaultUntil, new DateTime(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(2m, FreePublishRules.EffectivePublishCost(2m, null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }
}

public class FreePublishProductTests
{
    [Fact]
    public async Task Publish_is_free_during_promo_but_highlight_still_costs()
    {
        await using var db = CreateDb();
        SeedFreePublish(db, FreePublishRules.DefaultUntil);
        SeedSpendCosts(db);
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 2m);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);

        var sut = CreateProducts(db);
        var result = await sut.PublishAsync(
            vacancy,
            new VacancyPublishOptions(Highlight: true),
            actorUserId: null);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
        Assert.True(vacancy.IsHighlighted);
        // Publish 0 + Highlight 2 = 2 → balance 0
        Assert.Equal(0m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task Publish_costs_again_after_promo_ends()
    {
        await using var db = CreateDb();
        SeedFreePublish(db, new DateOnly(2020, 1, 1));
        SeedSpendCosts(db);
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 1m);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);

        var sut = CreateProducts(db);
        var result = await sut.PublishAsync(vacancy, new VacancyPublishOptions(), actorUserId: null);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(0m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task Welcome_token_skipped_during_free_publish_period()
    {
        await using var db = CreateDb();
        SeedFreePublish(db, FreePublishRules.DefaultUntil);
        var sut = CreateRegistration(db);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990011",
            "99990011_0001",
            RegistrationScope.BranchOnly,
            "Promo Manager",
            "promo.free@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "TestPass1!"));

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();

        var activated = await sut.ActivateAsync(token!);
        Assert.False(activated.WelcomeTokenGranted);
        Assert.Equal(FreePublishRules.DefaultUntil, activated.FreePublishUntil);

        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.True(branch.HasReceivedWelcomeToken);
        Assert.Equal(0m, await db.TokenTransactions.Where(t => t.CompanyId == branch.Id).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task Welcome_token_granted_when_promo_disabled()
    {
        await using var db = CreateDb();
        SeedFreePublish(db, freeUntil: null);
        var sut = CreateRegistration(db);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990012",
            "99990012_0001",
            RegistrationScope.BranchOnly,
            "Paid Manager",
            "promo.paid@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "TestPass1!"));

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();

        var activated = await sut.ActivateAsync(token!);
        Assert.True(activated.WelcomeTokenGranted);
        Assert.Null(activated.FreePublishUntil);

        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.Equal(1m, await db.TokenTransactions.Where(t => t.CompanyId == branch.Id).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task Admin_can_update_free_publish_until()
    {
        await using var db = CreateDb();
        var sut = CreateFeatures(db);
        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            FreePublishUntil: new DateOnly(2026, 12, 31)));

        Assert.Equal(new DateOnly(2026, 12, 31), updated.FreePublishUntil);
        var cleared = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            FreePublishUntil: null));
        Assert.Null(cleared.FreePublishUntil);
    }

    private static void SeedFreePublish(JobsyDbContext db, DateOnly? freeUntil)
    {
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            VacancyContentModerationEnabled = true,
            FreePublishUntil = freeUntil,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static void SeedSpendCosts(JobsyDbContext db)
    {
        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = VacancyProductRules.DefaultHighlightCostTokens, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true });
        db.SaveChanges();
    }

    private static async Task<(Guid CompanyId, Guid VacancyId)> SeedDraftVacancyAsync(JobsyDbContext db, decimal tokenBalance)
    {
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Promo Co",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(51.98, 4.22),
            KvkVerificationStatus = KvkVerificationStatus.Verified
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "Demo",
            Description = "Demo",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Status = VacancyStatus.Draft,
            CompanyId = companyId,
            Location = new GeoPoint(51.98, 4.22),
            RequiredTransport = TransportMode.Bike,
            CategoryId = VacancyCategoryDefaults.RegulierId
        });
        if (tokenBalance > 0)
        {
            db.TokenTransactions.Add(new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Amount = tokenBalance,
                Kind = TokenTransactionKind.Grant,
                OldBalance = 0,
                NewBalance = tokenBalance,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return (companyId, vacancyId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static PlatformFeatureService CreateFeatures(JobsyDbContext db)
        => new(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PublicWebBaseUrl"] = "http://localhost:5201"
                })
                .Build());

    private static IVacancyProductService CreateProducts(JobsyDbContext db)
        => new VacancyProductService(
            db,
            new TokenLedgerService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new VacancyCategoryService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            CreateFeatures(db),
            new MockRoutingService(),
            NullLogger<VacancyProductService>.Instance);

    private static CompanyRegistrationService CreateRegistration(JobsyDbContext db)
        => new(
            db,
            new TestKvkCatalog(),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            CreateFeatures(db),
            NullLogger<CompanyRegistrationService>.Instance);

    private sealed class TestKvkCatalog : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
        {
            var n = kvkNumber.Trim();
            return Task.FromResult<KvkCompanyResult?>(new KvkCompanyResult(n, "Test Co", "Adres 1", []));
        }

        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var n = kvkNumber.Trim();
            IReadOnlyList<KvkEstablishmentResult> items =
            [
                new(n, "0001", $"{n}_0001", "Test Vestiging", "Adres 1", 52, 4, false, [])
            ];
            return Task.FromResult(items);
        }

        public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
            => KvkEstablishmentsLookup.Ok(await GetEstablishmentsAsync(kvkNumber, cancellationToken));
    }
}
