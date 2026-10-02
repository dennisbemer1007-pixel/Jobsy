using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.LenderRegistration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class LenderRegistration10Tests
{
    [Fact]
    public void KvkSbiClassification_mixed_and_main_activity()
    {
        Assert.True(KvkSbiClassification.IsMixedIntermediary(["7820", "4711"]));
        Assert.False(KvkSbiClassification.IsMixedIntermediary(["7820"]));
        Assert.True(KvkSbiClassification.IsMainActivityIntermediary(["7820", "4711"]));
        Assert.False(KvkSbiClassification.IsMainActivityIntermediary(["4711", "7820"]));
        Assert.True(KvkSbiClassification.HasNonIntermediary(["7820", "4711"]));
    }

    [Fact]
    public async Task StartForNewBureauAsync_called_once_on_sbi78_activation_never_for_employer()
    {
        await using var db = CreateDb();
        var lender = CreateLender(db);
        var sut = CreateRegistration(db, lender);

        var flex = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990078", "99990078_0001", RegistrationScope.BranchOnly,
            "Flex Owner", "flex.owner10@jobsy.local",
            AcceptedTerms: true, Password: "IntermedPass1!"));
        var flexToken = await db.CompanyRegistrations
            .Where(r => r.Id == flex.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var activated = await sut.ActivateAsync(flexToken);
        Assert.Equal("Intermediary", activated.Role);

        var rows = await db.LenderRegistrations
            .Where(r => r.CompanyId == activated.BranchCompanyId)
            .ToListAsync();
        Assert.Single(rows);
        Assert.Equal(LenderRegistrationStatuses.Pending, rows[0].Status);

        await lender.StartForNewBureauAsync(activated.BranchCompanyId!.Value, "99990078");
        Assert.Equal(1, await db.LenderRegistrations.CountAsync(r => r.CompanyId == activated.BranchCompanyId));

        var employer = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990002", "99990002_0001", RegistrationScope.Organization,
            "Org Boss", "org.boss10@jobsy.local",
            AcceptedTerms: true, Password: "BedrijfPass1!"));
        var empToken = await db.CompanyRegistrations
            .Where(r => r.Id == employer.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var empActivated = await sut.ActivateAsync(empToken);
        Assert.Equal("EnterpriseManager", empActivated.Role);
        Assert.Equal(0, await db.LenderRegistrations.CountAsync(r =>
            r.CompanyId == empActivated.BranchCompanyId || r.CompanyId == empActivated.OrganizationCompanyId));
    }

    [Fact]
    public async Task Mixed_sbi_honours_employer_choice()
    {
        await using var db = CreateDb();
        var sut = CreateRegistration(db, CreateLender(db), mixedSbiKvk: true);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990079", "99990079_0001", RegistrationScope.Organization,
            "Mixed Boss", "mixed.employer10@jobsy.local",
            AcceptedTerms: true, Password: "MixedPass12!",
            ManualIsIntermediarySbi: false));

        var reg = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.False(reg.IsIntermediarySbi);
        Assert.Equal(RegistrationScope.Organization, reg.Scope);
    }

    [Fact]
    public async Task Mixed_sbi_honours_intermediary_choice()
    {
        await using var db = CreateDb();
        var sut = CreateRegistration(db, CreateLender(db), mixedSbiKvk: true);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990079", "99990079_0001", RegistrationScope.BranchOnly,
            "Mixed Flex", "mixed.flex10@jobsy.local",
            AcceptedTerms: true, Password: "MixedPass12!",
            ManualIsIntermediarySbi: true));

        var reg = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.True(reg.IsIntermediarySbi);
        Assert.Equal(RegistrationScope.BranchOnly, reg.Scope);
    }

    [Fact]
    public async Task Publish_gate_blocks_before_token_spend_and_allows_drafts()
    {
        await using var db = CreateDb();
        EnsurePaidPublish(db);
        var lender = CreateLender(db);
        var (bureauId, clientId, vacancyId) = await SeedIntermediaryDraftAsync(db, tokenBalance: 5m);
        await lender.StartForNewBureauAsync(bureauId, "99990078");

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        Assert.Equal(VacancyStatus.Draft, vacancy.Status);

        var products = CreateProducts(db, lender);
        var blocked = await products.PublishAsync(
            vacancy,
            new VacancyPublishOptions(),
            actorUserId: null,
            allowPendingApproval: false);

        Assert.False(blocked.Succeeded);
        Assert.Equal(LenderRegistrationRules.PendingErrorCode, blocked.ErrorCode);
        Assert.False(blocked.InsufficientTokens);
        Assert.Equal(VacancyStatus.Draft, vacancy.Status);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
        Assert.Equal(5m, await new TokenLedgerService(db).GetBalanceAsync(clientId));

        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId,
            Email = "admin.lender@jobsy.local",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true,
        });
        await db.SaveChangesAsync();
        await lender.RecordDecisionAsync(
            bureauId,
            new LenderRegistrationDecision(true, LenderRegistrationSources.WaadiKvk, "WAADI-1", null, "ok"),
            adminId);

        var again = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var published = await products.PublishAsync(
            again,
            new VacancyPublishOptions(),
            actorUserId: null,
            allowPendingApproval: false);
        Assert.True(published.Succeeded, published.ErrorMessage);
        Assert.Equal(VacancyStatus.Active, again.Status);
        Assert.Equal(1, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
    }

    [Fact]
    public async Task ValidUntil_expiry_blocks_publish()
    {
        await using var db = CreateDb();
        EnsurePaidPublish(db);
        var lender = CreateLender(db);
        var (bureauId, _, vacancyId) = await SeedIntermediaryDraftAsync(db, tokenBalance: 5m);
        await lender.StartForNewBureauAsync(bureauId, "99990078");

        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId,
            Email = "admin.expiry@jobsy.local",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true,
        });
        await db.SaveChangesAsync();
        await lender.RecordDecisionAsync(
            bureauId,
            new LenderRegistrationDecision(
                true,
                LenderRegistrationSources.AdminManual,
                "X",
                DateTime.UtcNow.AddDays(-1),
                "expired"),
            adminId);

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var result = await CreateProducts(db, lender).PublishAsync(
            vacancy, new VacancyPublishOptions(), null, allowPendingApproval: false);
        Assert.False(result.Succeeded);
        Assert.Equal(LenderRegistrationRules.PendingErrorCode, result.ErrorCode);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
    }

    [Fact]
    public async Task Admin_decision_writes_audit_and_notification()
    {
        await using var db = CreateDb();
        var lender = CreateLender(db);
        var bureauId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = bureauId,
            Name = "Bureau",
            KvkNumber = "99990078",
            KvkEstablishmentId = "99990078_0001",
            Address = "A",
            Location = new GeoPoint(52.1, 5.1),
            Type = CompanyType.Intermediary,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Manual,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.Users.Add(new User
        {
            Id = userId,
            Email = "bureau.user@jobsy.local",
            FullName = "Bureau User",
            Role = UserRole.Intermediary,
            CompanyId = bureauId,
            IsActive = true,
        });
        await db.SaveChangesAsync();
        await lender.StartForNewBureauAsync(bureauId, "99990078");

        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId,
            Email = "admin2@jobsy.local",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        await lender.RecordDecisionAsync(
            bureauId,
            new LenderRegistrationDecision(false, LenderRegistrationSources.AdminManual, null, null, "niet in Waadi"),
            adminId);

        Assert.True(await db.PlatformLogs.AnyAsync(l => l.Category == "admin.lender_registration.reject"));
        Assert.True(await db.UserNotifications.AnyAsync(n => n.UserId == userId && n.Category == "lender_registration"));
        var state = await lender.GetStateAsync(bureauId);
        Assert.Equal(LenderRegistrationStatuses.Rejected, state.Status);
        Assert.False(lender.CanPublish(state));
    }

    [Fact]
    public void Registration_never_touches_IntermediaryClient_type_G_absent()
    {
        var entity = typeof(Company).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IntermediaryClient");
        Assert.Null(entity);
        Assert.NotNull(typeof(ILenderRegistrationCheck));
    }

    private static async Task<(Guid BureauId, Guid ClientId, Guid VacancyId)> SeedIntermediaryDraftAsync(
        JobsyDbContext db,
        decimal tokenBalance)
    {
        var bureauId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = bureauId,
            Name = "Flex Bureau",
            KvkNumber = "99990078",
            KvkEstablishmentId = "99990078_0001",
            Address = "B",
            Location = new GeoPoint(52.1, 5.1),
            Type = CompanyType.Intermediary,
            KvkVerificationStatus = KvkVerificationStatus.Verified,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Manual,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.Companies.Add(new Company
        {
            Id = clientId,
            Name = "Client BV",
            KvkNumber = "99990002",
            KvkEstablishmentId = "99990002_0001",
            Address = "C",
            Location = new GeoPoint(52.2, 5.2),
            Type = CompanyType.Employer,
            KvkVerificationStatus = KvkVerificationStatus.Verified,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Manual,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.VacancyCategories.Add(new VacancyCategory
        {
            Id = categoryId,
            Slug = "operationeel-lender",
            Name = "Operationeel",
            ColorHex = "#F54A1B",
            PublishCostTokens = 1m,
            HighlightAvailable = true,
            HighlightCostTokens = 1m,
            PushBomAvailable = false,
            PlacementKind = VacancyKind.Regular,
            SortOrder = 1,
            IsActive = true
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = clientId,
            IntermediaryCompanyId = bureauId,
            Title = "Operator",
            Description = "Demo",
            Status = VacancyStatus.Draft,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Location = new GeoPoint(52.2, 5.2),
            RequiredTransport = TransportMode.Bike,
            Kind = VacancyKind.Regular,
            CategoryId = categoryId
        });
        if (tokenBalance > 0)
        {
            db.TokenTransactions.Add(new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = clientId,
                Amount = tokenBalance,
                Kind = TokenTransactionKind.Grant,
                OldBalance = 0,
                NewBalance = tokenBalance,
            });
        }

        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true });
        await db.SaveChangesAsync();
        return (bureauId, clientId, vacancyId);
    }

    private static void EnsurePaidPublish(JobsyDbContext db)
    {
        var existing = db.PlatformFeatureSettings.Local.FirstOrDefault()
                       ?? db.PlatformFeatureSettings.FirstOrDefault();
        if (existing is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                FreePublishUntil = null,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.FreePublishUntil = null;
        }

        db.SaveChanges();
    }

    private static LenderRegistrationCheckService CreateLender(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LenderRegistration:WttaNau:Enabled"] = "false"
        }).Build();
        ILenderRegistrationProvider[] providers =
        [
            new WaadiKvkProvider(config),
            new WttaNauProvider(config),
            new AdminManualLenderProvider()
        ];
        return new LenderRegistrationCheckService(
            db,
            providers,
            new UserNotificationService(db),
            NullLogger<LenderRegistrationCheckService>.Instance);
    }

    private static CompanyRegistrationService CreateRegistration(
        JobsyDbContext db,
        ILenderRegistrationCheck lender,
        bool mixedSbiKvk = false)
    {
        EnsurePaidPublish(db);
        var config = new ConfigurationBuilder().Build();
        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            config);
        var ledger = new TokenLedgerService(db);
        var partners = new PartnerAffiliateService(db, ledger, features);
        return new CompanyRegistrationService(
            db,
            mixedSbiKvk ? new MixedSbiKvk() : new FlexKvk(),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            ledger,
            features,
            partners,
            null,
            null,
            lender,
            NullLogger<CompanyRegistrationService>.Instance);
    }

    private static VacancyProductService CreateProducts(JobsyDbContext db, ILenderRegistrationCheck lender)
    {
        EnsurePaidPublish(db);
        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());
        var ledger = new TokenLedgerService(db);
        return new VacancyProductService(
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
            NullLogger<VacancyProductService>.Instance,
            discoveryIndex: null,
            lenderRegistration: lender);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("lender10-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FlexKvk : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
        {
            var sbi = kvkNumber == "99990078" ? new[] { "7820" } : new[] { "5229" };
            var name = kvkNumber == "99990078" ? "Flex Agency" : "Org HQ";
            return Task.FromResult<KvkCompanyResult?>(new KvkCompanyResult(kvkNumber, name, "Straat", sbi));
        }

        public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => (await LookupEstablishmentsAsync(kvkNumber, cancellationToken)).Establishments;

        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
        {
            var est = new KvkEstablishmentResult(
                kvkNumber, "0001", $"{kvkNumber}_0001", "Est", "Straat", 52.1, 5.1, false,
                SbiCodes: kvkNumber == "99990078" ? ["7820"] : ["5229"]);
            return Task.FromResult(KvkEstablishmentsLookup.Ok([est]));
        }
    }

    private sealed class MixedSbiKvk : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(
                new KvkCompanyResult(kvkNumber, "Mixed BV", "Straat", ["7820", "4711"]));

        public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => (await LookupEstablishmentsAsync(kvkNumber, cancellationToken)).Establishments;

        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
        {
            var est = new KvkEstablishmentResult(
                kvkNumber, "0001", $"{kvkNumber}_0001", "Mixed", "Straat", 52.1, 5.1, false,
                SbiCodes: ["7820", "4711"]);
            return Task.FromResult(KvkEstablishmentsLookup.Ok([est]));
        }
    }
}
