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
    public void IsActive_uses_europe_amsterdam_calendar_day()
    {
        var until = new DateOnly(2026, 11, 18);
        // 22:30 UTC on 18 Nov = 23:30 CET → still 18 Nov Amsterdam → active
        Assert.True(FreePublishRules.IsActive(until, new DateTime(2026, 11, 18, 22, 30, 0, DateTimeKind.Utc)));
        // 23:00 UTC on 18 Nov = 00:00 CET on 19 Nov → promo ended
        Assert.False(FreePublishRules.IsActive(until, new DateTime(2026, 11, 18, 23, 0, 0, DateTimeKind.Utc)));
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
    public async Task Full_flow_register_publish_free_highlight_costs_tokens()
    {
        await using var db = CreateDb();
        SeedFreePublish(db, FreePublishRules.DefaultUntil);
        SeedSpendCosts(db);

        // 1) Aanmelden + activeren tijdens gratis-periode → geen welkomsttoken
        var registration = CreateRegistration(db);
        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990013",
            "99990013_0001",
            RegistrationScope.BranchOnly,
            "Flow Manager",
            "flow.full@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "TestPass1!"));
        Assert.False(submit.RequiresTakeover);
        Assert.Equal(CompanyRegistrationStatus.PendingActivation, submit.Status);

        var activationToken = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var activated = await registration.ActivateAsync(activationToken!);
        Assert.False(activated.WelcomeTokenGranted);
        Assert.Equal(FreePublishRules.DefaultUntil, activated.FreePublishUntil);
        Assert.Equal("EnterpriseManager", activated.Role);

        var companyId = activated.BranchCompanyId!.Value;
        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        Assert.Equal(KvkVerificationStatus.Verified, company.KvkVerificationStatus);
        Assert.Equal(0m, await BalanceAsync(db, companyId));

        var products = CreateProducts(db);

        // 2) Vacature plaatsen zonder extras → gratis (saldo blijft 0)
        var draft1 = await SeedDraftForCompanyAsync(db, companyId, "Gratis basaal");
        var publishFree = await products.PublishAsync(
            draft1,
            new VacancyPublishOptions(),
            actorUserId: activated.UserId);
        Assert.True(publishFree.Succeeded, publishFree.ErrorMessage);
        Assert.Equal(VacancyStatus.Active, draft1.Status);
        Assert.False(draft1.IsHighlighted);
        Assert.Equal(0m, await BalanceAsync(db, companyId));
        Assert.DoesNotContain(
            await db.TokenTransactions.Where(t => t.CompanyId == companyId).ToListAsync(),
            t => t.Kind == TokenTransactionKind.Spend);

        // 3) Highlight bij publiceren zonder saldo → onvoldoende tokens (highlight kost wél)
        var draft2 = await SeedDraftForCompanyAsync(db, companyId, "Wil highlight");
        var blocked = await products.PublishAsync(
            draft2,
            new VacancyPublishOptions(Highlight: true),
            actorUserId: activated.UserId);
        Assert.False(blocked.Succeeded);
        Assert.True(blocked.InsufficientTokens);
        Assert.Equal(VacancyStatus.Draft, draft2.Status);
        Assert.Equal(0m, await BalanceAsync(db, companyId));

        // 4) Tokens kopen/grant → publiceren mét highlight: alleen highlight-tokens (2) afgeschreven
        await GrantTokensAsync(db, companyId, 5m);
        Assert.Equal(5m, await BalanceAsync(db, companyId));

        var withHighlight = await products.PublishAsync(
            draft2,
            new VacancyPublishOptions(Highlight: true),
            actorUserId: activated.UserId);
        Assert.True(withHighlight.Succeeded, withHighlight.ErrorMessage);
        Assert.Equal(VacancyStatus.Active, draft2.Status);
        Assert.True(draft2.IsHighlighted);
        Assert.NotNull(draft2.HighlightedUntil);
        Assert.Equal(3m, await BalanceAsync(db, companyId)); // 5 - 2 highlight

        var spends = await db.TokenTransactions
            .Where(t => t.CompanyId == companyId && t.Kind == TokenTransactionKind.Spend)
            .ToListAsync();
        Assert.DoesNotContain(spends, t => t.Reason == TokenSpendReason.Publish);
        Assert.Contains(spends, t => t.Reason == TokenSpendReason.Highlight && t.Amount == -2m);

        // 5) Losse HighlightAsync op een actieve vacature zonder highlight → kost opnieuw tokens
        var draft3 = await SeedDraftForCompanyAsync(db, companyId, "Later highlighten");
        var published = await products.PublishAsync(
            draft3,
            new VacancyPublishOptions(),
            actorUserId: activated.UserId);
        Assert.True(published.Succeeded, published.ErrorMessage);
        Assert.Equal(3m, await BalanceAsync(db, companyId));

        var highlightOnly = await products.HighlightAsync(draft3, activated.UserId);
        Assert.True(highlightOnly.Succeeded, highlightOnly.ErrorMessage);
        Assert.True(draft3.IsHighlighted);
        Assert.Equal(1m, await BalanceAsync(db, companyId)); // 3 - 2
        Assert.Contains(
            await db.TokenTransactions
                .Where(t => t.CompanyId == companyId && t.Kind == TokenTransactionKind.Spend)
                .ToListAsync(),
            t => t.Reason == TokenSpendReason.Highlight && t.VacancyId == draft3.Id && t.Amount == -2m);
    }

    [Fact]
    public async Task Intermediary_and_organization_publish_also_free_during_promo()
    {
        await using var db = CreateDb();
        SeedFreePublish(db, FreePublishRules.DefaultUntil);
        SeedSpendCosts(db);
        var products = CreateProducts(db);

        // Intermediair-vacature (end-client company)
        var (clientId, clientVacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0m);
        var clientVacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == clientVacancyId);
        clientVacancy.IntermediaryCompanyId = Guid.NewGuid();
        await db.SaveChangesAsync();

        var intermediaryPublish = await products.PublishAsync(
            clientVacancy,
            new VacancyPublishOptions(),
            actorUserId: null);
        Assert.True(intermediaryPublish.Succeeded, intermediaryPublish.ErrorMessage);
        Assert.Equal(0m, await BalanceAsync(db, clientId));

        // Organisatie-scope registratie + gratis publish
        var registration = CreateRegistration(db);
        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990014",
            "99990014_0001",
            RegistrationScope.Organization,
            "Org Manager",
            "flow.org@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "TestPass1!"));
        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var activated = await registration.ActivateAsync(token!);
        Assert.False(activated.WelcomeTokenGranted);
        Assert.NotNull(activated.OrganizationCompanyId);
        Assert.NotNull(activated.BranchCompanyId);

        var orgDraft = await SeedDraftForCompanyAsync(db, activated.BranchCompanyId!.Value, "Org gratis");
        var orgPublish = await products.PublishAsync(
            orgDraft,
            new VacancyPublishOptions(),
            actorUserId: activated.UserId);
        Assert.True(orgPublish.Succeeded, orgPublish.ErrorMessage);
        Assert.Equal(0m, await BalanceAsync(db, activated.BranchCompanyId!.Value));
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

        // Partial update without date or clear flag must preserve the promo end date.
        var preserved = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            SessionInactivityTimeoutMinutes: 15));
        Assert.Equal(new DateOnly(2026, 12, 31), preserved.FreePublishUntil);
        Assert.Equal(15, preserved.SessionInactivityTimeoutMinutes);

        var cleared = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            FreePublishUntil: null,
            ClearFreePublishUntil: true));
        Assert.Null(cleared.FreePublishUntil);
    }

    [Fact]
    public async Task First_platform_feature_insert_defaults_free_publish_until()
    {
        await using var db = CreateDb();
        var sut = CreateFeatures(db);
        var created = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            SessionInactivityTimeoutMinutes: 30));
        Assert.Equal(FreePublishRules.DefaultUntil, created.FreePublishUntil);
    }

    private static void SeedFreePublish(JobsyDbContext db, DateOnly? freeUntil)
    {
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            VacancyContentModerationEnabled = true,
            ExposeRegistrationActivationLinks = true,
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
        var vacancy = await SeedDraftForCompanyAsync(db, companyId, "Demo", createCompany: true);
        if (tokenBalance > 0)
        {
            await GrantTokensAsync(db, companyId, tokenBalance);
        }

        return (companyId, vacancy.Id);
    }

    private static async Task<Vacancy> SeedDraftForCompanyAsync(
        JobsyDbContext db,
        Guid companyId,
        string title,
        bool createCompany = false)
    {
        if (createCompany && !await db.Companies.AnyAsync(c => c.Id == companyId))
        {
            db.Companies.Add(new Company
            {
                Id = companyId,
                Name = "Promo Co",
                KvkNumber = "1",
                Address = "a",
                Location = new GeoPoint(51.98, 4.22),
                KvkVerificationStatus = KvkVerificationStatus.Verified
            });
        }

        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Demo",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Status = VacancyStatus.Draft,
            CompanyId = companyId,
            Location = new GeoPoint(51.98, 4.22),
            RequiredTransport = TransportMode.Bike,
            CategoryId = VacancyCategoryDefaults.RegulierId
        };
        db.Vacancies.Add(vacancy);
        await db.SaveChangesAsync();
        return await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancy.Id);
    }

    private static async Task GrantTokensAsync(JobsyDbContext db, Guid companyId, decimal amount)
    {
        var balance = await BalanceAsync(db, companyId);
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Amount = amount,
            Kind = TokenTransactionKind.Grant,
            OldBalance = balance,
            NewBalance = balance + amount,
            CreatedAt = DateTime.UtcNow,
            Note = "test grant"
        });
        await db.SaveChangesAsync();
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
