using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class Sprint7RegistrationTests
{
    [Fact]
    public async Task Submit_and_activate_branch_only_creates_unique_establishment()
    {
        await using var db = CreateDb();
        var sut = CreateService(db, exposeActivationLinks: true);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990001",
            "99990001_0001",
            RegistrationScope.BranchOnly,
            "Nova Manager",
            "nova.branch@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "TestPass1!"));

        Assert.False(submit.RequiresTakeover);
        Assert.Equal(CompanyRegistrationStatus.PendingActivation, submit.Status);
        Assert.False(string.IsNullOrWhiteSpace(submit.ActivationUrl));

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();

        var activated = await sut.ActivateAsync(token);

        Assert.Equal("BranchManager", activated.Role);
        Assert.NotNull(activated.BranchCompanyId);
        Assert.Null(activated.OrganizationCompanyId);
        Assert.True(activated.UsedChosenPassword);
        Assert.Equal(string.Empty, activated.TemporaryPassword);
        Assert.Equal(1, await db.Companies.CountAsync(c => c.KvkEstablishmentId == "99990001_0001"));
        var credential = await db.LocalAuthCredentials.SingleAsync(c => c.Email == "nova.branch@jobsy.local");
        Assert.True(Jobsy.Infrastructure.Security.JobsyPasswordHasher.Verify("TestPass1!", credential.PasswordHash));
        Assert.Null(await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.PasswordHash)
            .SingleAsync());
        Assert.True(await db.PlatformLogs.AnyAsync(l =>
            l.Category == "RegistrationCredentials"
            || (l.Category == "Email" && l.Message.Contains("Je Jobsy-account is actief"))));

        // Welcome token: 1 credit on the vestiging, marked on the company row.
        Assert.NotNull(activated.BranchCompanyId);
        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.True(branch.HasReceivedWelcomeToken);
        Assert.Equal(
            CompanyRegistrationService.WelcomeTokenAmount,
            await db.TokenTransactions.Where(t => t.CompanyId == branch.Id).SumAsync(t => (decimal?)t.Amount) ?? 0m);
        Assert.True(await db.TokenTransactions.AnyAsync(t =>
            t.CompanyId == branch.Id
            && t.Kind == TokenTransactionKind.Grant
            && t.Note == CompanyRegistrationService.WelcomeTokenNote
            && t.Amount == CompanyRegistrationService.WelcomeTokenAmount));

        // Token consumed — replay must fail without leaking password.
        var consumed = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.Equal(string.Empty, consumed.ActivationToken);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.ActivateAsync(token));
    }

    [Fact]
    public async Task Activate_organization_grants_welcome_token_to_primary_branch()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990002",
            "99990002_0001",
            RegistrationScope.Organization,
            "Org Manager",
            "welcome.org@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "TestPass1!"));

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();

        var activated = await sut.ActivateAsync(token);

        Assert.NotNull(activated.BranchCompanyId);
        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.True(branch.HasReceivedWelcomeToken);
        Assert.Equal(1m, await db.TokenTransactions
            .Where(t => t.CompanyId == branch.Id)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m);

        // Sibling vestigingen (claimed under org) do not get a welcome grant.
        var siblings = await db.Companies
            .Where(c => c.ParentCompanyId == activated.OrganizationCompanyId
                        && c.Id != activated.BranchCompanyId)
            .ToListAsync();
        Assert.All(siblings, s => Assert.False(s.HasReceivedWelcomeToken));
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t =>
            siblings.Select(s => s.Id).Contains(t.CompanyId)));
    }

    [Fact]
    public async Task Submit_hides_activation_url_when_flag_off()
    {
        await using var db = CreateDb();
        var sut = CreateService(db, exposeActivationLinks: false);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990001", "99990001_0001", RegistrationScope.BranchOnly,
            "A", "hidden.url@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));

        Assert.Null(submit.ActivationUrl);
        Assert.False(string.IsNullOrEmpty(
            await db.CompanyRegistrations.Where(r => r.Id == submit.RegistrationId)
                .Select(r => r.ActivationToken).SingleAsync()));
    }

    [Fact]
    public async Task Submit_requires_password()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.SubmitAsync(
            new RegistrationSubmitRequest(
                "99990001", "99990001_0001", RegistrationScope.BranchOnly,
                "A", "nopass@jobsy.local", null, AcceptedTerms: true)));
        Assert.Contains("Wachtwoord", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Expired_activation_clears_pending_password_hash()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990001", "99990001_0001", RegistrationScope.BranchOnly,
            "A", "expired.pass@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));

        var registration = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.False(string.IsNullOrWhiteSpace(registration.PasswordHash));
        var token = registration.ActivationToken;
        registration.CreatedAt = DateTime.UtcNow - CompanyRegistrationService.ActivationTokenTtl - TimeSpan.FromMinutes(1);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ActivateAsync(token));

        var expired = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.Equal(CompanyRegistrationStatus.Cancelled, expired.Status);
        Assert.Null(expired.PasswordHash);
        Assert.Equal(string.Empty, expired.ActivationToken);
    }

    [Fact]
    public async Task Activate_sbi_78_assigns_intermediary_role_and_company_type()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990078",
            "99990078_0001",
            RegistrationScope.BranchOnly, // ignored for SBI 78
            "Flex Owner",
            "flex.owner@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "Intermed1!"));

        Assert.Contains("Intermediair", submit.Message, StringComparison.OrdinalIgnoreCase);

        var pending = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.True(pending.IsIntermediarySbi);
        Assert.Equal("7820", pending.PrimarySbiCode);
        Assert.Equal(RegistrationScope.BranchOnly, pending.Scope);

        var activated = await sut.ActivateAsync(pending.ActivationToken);
        Assert.Equal("Intermediary", activated.Role);
        Assert.True(activated.UsedChosenPassword);

        var company = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.Equal(CompanyType.Intermediary, company.Type);
        Assert.Null(company.ParentCompanyId);
        Assert.Equal(0, await db.Companies.CountAsync(c => c.ParentCompanyId == company.Id));
    }

    [Fact]
    public async Task Activate_non_sbi_78_organization_assigns_enterprise_manager()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990002",
            "99990002_0001",
            RegistrationScope.Organization,
            "Org Boss",
            "org.boss@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "Bedrijf1!"));

        Assert.Contains("Bedrijfsmanager", submit.Message, StringComparison.OrdinalIgnoreCase);

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var activated = await sut.ActivateAsync(token);

        Assert.Equal("EnterpriseManager", activated.Role);
        var org = await db.Companies.SingleAsync(c => c.Id == activated.OrganizationCompanyId);
        Assert.Equal(CompanyType.Employer, org.Type);
        var reg = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.Equal("5229", reg.PrimarySbiCode);
        Assert.False(reg.IsIntermediarySbi);
    }

    [Fact]
    public async Task Submit_organization_claims_sibling_establishments()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990002",
            "99990002_0001",
            RegistrationScope.Organization,
            "Org Manager",
            "nova.org@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "TestPass1!"));

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();

        var activated = await sut.ActivateAsync(token);

        Assert.Equal("EnterpriseManager", activated.Role);
        Assert.NotNull(activated.OrganizationCompanyId);
        Assert.Equal(2, await db.Companies.CountAsync(c =>
            c.ParentCompanyId == activated.OrganizationCompanyId));
    }

    [Fact]
    public async Task Organization_skips_siblings_with_pending_registration()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990002", "99990002_0002", RegistrationScope.BranchOnly,
            "Sibling Pending", "sibling.pending@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));

        var orgSubmit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990002", "99990002_0001", RegistrationScope.Organization,
            "Org Manager", "org.skip@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));

        var token = await db.CompanyRegistrations.Where(r => r.Id == orgSubmit.RegistrationId)
            .Select(r => r.ActivationToken).SingleAsync();
        var activated = await sut.ActivateAsync(token);

        Assert.Equal(1, await db.Companies.CountAsync(c =>
            c.ParentCompanyId == activated.OrganizationCompanyId));
        Assert.False(await db.Companies.AnyAsync(c => c.KvkEstablishmentId == "99990002_0002"));
    }

    [Fact]
    public async Task Duplicate_pending_registration_is_rejected()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990004", "99990004_0001", RegistrationScope.BranchOnly,
            "A", "a@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SubmitAsync(
            new RegistrationSubmitRequest(
                "99990004", "99990004_0001", RegistrationScope.BranchOnly,
                "B", "b2@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!")));
    }

    [Fact]
    public async Task Conflict_creates_takeover_and_approve_merges_tokens_to_org()
    {
        await using var db = CreateDb();
        var existingId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = existingId,
            Name = "Bestaande Vestiging",
            KvkNumber = "99990003",
            KvkEstablishmentId = "99990003_0001",
            Address = "Teststraat 1",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = existingId,
            Amount = 12m,
            Kind = TokenTransactionKind.Grant,
            OldBalance = 0,
            NewBalance = 12m,
            CreatedAt = DateTime.UtcNow
        });
        var ownerId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = ownerId,
            Email = "owner@jobsy.local",
            FullName = "Owner",
            Role = UserRole.EnterpriseManager,
            CompanyId = existingId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = ownerId, CompanyId = existingId });
        await db.SaveChangesAsync();

        var sut = CreateService(db);
        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990003",
            "99990003_0001",
            RegistrationScope.Organization,
            "Requester",
            "requester@jobsy.local",
            null,
            AcceptedTerms: true,
            Password: "TestPass1!"));

        Assert.True(submit.RequiresTakeover);
        Assert.Equal(CompanyRegistrationStatus.TakeoverPending, submit.Status);
        Assert.False(string.IsNullOrWhiteSpace(submit.ActivationUrl));

        // Unverified takeover must not be approvable / listed.
        var takeoverId = await db.EstablishmentTakeoverRequests
            .Where(t => t.RegistrationId == submit.RegistrationId)
            .Select(t => t.Id)
            .SingleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApproveTakeoverAsync(
            takeoverId, ownerId, UserRole.EnterpriseManager, [existingId], isAdmin: false));
        Assert.Empty(await sut.ListPendingTakeoversAsync([existingId], isAdmin: false));

        await VerifyTakeoverEmailAsync(db, sut, submit.RegistrationId);
        Assert.Single(await sut.ListPendingTakeoversAsync([existingId], isAdmin: false));

        var decision = await sut.ApproveTakeoverAsync(
            takeoverId,
            ownerId,
            UserRole.EnterpriseManager,
            accessibleCompanyIds: [existingId],
            isAdmin: false);

        Assert.Equal(TakeoverRequestStatus.Approved, decision.Status);
        Assert.NotNull(decision.OrganizationCompanyId);

        var ledger = new TokenLedgerService(db);
        Assert.Equal(0m, await ledger.GetBalanceAsync(existingId));
        Assert.Equal(12m, await ledger.GetBalanceAsync(decision.OrganizationCompanyId!.Value));

        var branch = await db.Companies.SingleAsync(c => c.Id == existingId);
        Assert.Equal(decision.OrganizationCompanyId, branch.ParentCompanyId);
        Assert.True(await db.LocalAuthCredentials.AnyAsync(c => c.Email == "requester@jobsy.local"));

        // Prior owner loses access / is deactivated when no remaining companies.
        var prior = await db.Users.Include(u => u.CompanyMemberships).SingleAsync(u => u.Id == ownerId);
        Assert.False(prior.IsActive);
        Assert.DoesNotContain(prior.CompanyMemberships, m => m.CompanyId == existingId);
    }

    [Fact]
    public async Task Branch_manager_cannot_approve_organization_takeover()
    {
        await using var db = CreateDb();
        var existingId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = existingId,
            Name = "BM Branch",
            KvkNumber = "99990005",
            KvkEstablishmentId = "99990005_0001",
            Address = "X",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        var bmId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = bmId,
            Email = "bm@jobsy.local",
            FullName = "BM",
            Role = UserRole.BranchManager,
            CompanyId = existingId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = CreateService(db);
        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990005", "99990005_0001", RegistrationScope.Organization,
            "Req", "req.org@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));
        await VerifyTakeoverEmailAsync(db, sut, submit.RegistrationId);
        var takeoverId = await db.EstablishmentTakeoverRequests
            .Where(t => t.RegistrationId == submit.RegistrationId).Select(t => t.Id).SingleAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.ApproveTakeoverAsync(
            takeoverId, bmId, UserRole.BranchManager, [existingId], isAdmin: false));
    }

    [Fact]
    public async Task Duplicate_kvk_establishment_after_activation_becomes_takeover()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        var first = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990004", "99990004_0001", RegistrationScope.BranchOnly,
            "A", "a@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));
        var token = await db.CompanyRegistrations.Where(r => r.Id == first.RegistrationId)
            .Select(r => r.ActivationToken).SingleAsync();
        await sut.ActivateAsync(token);

        var second = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990004", "99990004_0001", RegistrationScope.BranchOnly,
            "B", "b@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));
        Assert.True(second.RequiresTakeover);
    }

    [Fact]
    public async Task Org_takeover_reuses_existing_parent()
    {
        await using var db = CreateDb();
        var parentId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        db.Companies.AddRange(
            new Company
            {
                Id = parentId,
                Name = "Existing Org",
                KvkNumber = "99990006",
                Address = "HQ",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Employer
            },
            new Company
            {
                Id = branchId,
                Name = "Child",
                KvkNumber = "99990006",
                KvkEstablishmentId = "99990006_0001",
                Address = "Branch",
                Location = new GeoPoint(52.1, 4.1),
                Type = CompanyType.Employer,
                ParentCompanyId = parentId
            });
        var emId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = emId,
            Email = "em@jobsy.local",
            FullName = "EM",
            Role = UserRole.EnterpriseManager,
            CompanyId = parentId,
            IsActive = true
        });
        db.UserCompanies.AddRange(
            new UserCompany { UserId = emId, CompanyId = parentId },
            new UserCompany { UserId = emId, CompanyId = branchId });
        await db.SaveChangesAsync();

        var sut = CreateService(db);
        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990006", "99990006_0001", RegistrationScope.Organization,
            "New EM", "new.em@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPass1!"));
        await VerifyTakeoverEmailAsync(db, sut, submit.RegistrationId);
        var takeoverId = await db.EstablishmentTakeoverRequests
            .Where(t => t.RegistrationId == submit.RegistrationId).Select(t => t.Id).SingleAsync();

        var decision = await sut.ApproveTakeoverAsync(
            takeoverId, emId, UserRole.EnterpriseManager, [parentId, branchId], false);

        Assert.Equal(parentId, decision.OrganizationCompanyId);
        Assert.Equal(1, await db.Companies.CountAsync(c =>
            c.KvkNumber == "99990006" && c.KvkEstablishmentId == null));
    }

    [Fact]
    public async Task Intermediary_takeover_detaches_from_employer_org_and_assigns_intermediary()
    {
        await using var db = CreateDb();
        var parentId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        db.Companies.AddRange(
            new Company
            {
                Id = parentId,
                Name = "Employer Org",
                KvkNumber = "99990078",
                Address = "HQ",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Employer
            },
            new Company
            {
                Id = branchId,
                Name = "Employer Branch",
                KvkNumber = "99990078",
                KvkEstablishmentId = "99990078_0001",
                Address = "Branch",
                Location = new GeoPoint(52.1, 4.1),
                Type = CompanyType.Employer,
                ParentCompanyId = parentId
            });
        var bmId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = bmId,
            Email = "bm.flex@jobsy.local",
            FullName = "BM",
            Role = UserRole.BranchManager,
            CompanyId = branchId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = bmId, CompanyId = branchId });
        await db.SaveChangesAsync();

        var sut = CreateService(db);
        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990078", "99990078_0001", RegistrationScope.Organization,
            "Flex Boss", "flex.takeover@jobsy.local", null, AcceptedTerms: true,
            Password: "Intermed1!"));
        Assert.True(submit.RequiresTakeover);

        await VerifyTakeoverEmailAsync(db, sut, submit.RegistrationId);
        var takeoverId = await db.EstablishmentTakeoverRequests
            .Where(t => t.RegistrationId == submit.RegistrationId)
            .Select(t => t.Id)
            .SingleAsync();

        // Branch manager can approve (scope is BranchOnly for SBI 78).
        var decision = await sut.ApproveTakeoverAsync(
            takeoverId, bmId, UserRole.BranchManager, [branchId], isAdmin: false);
        Assert.Equal(TakeoverRequestStatus.Approved, decision.Status);

        var company = await db.Companies.SingleAsync(c => c.Id == branchId);
        Assert.Equal(CompanyType.Intermediary, company.Type);
        Assert.Null(company.ParentCompanyId);

        var user = await db.Users.SingleAsync(u => u.Email == "flex.takeover@jobsy.local");
        Assert.Equal(UserRole.Intermediary, user.Role);
        Assert.Equal(branchId, user.CompanyId);
    }

    private static async Task VerifyTakeoverEmailAsync(
        JobsyDbContext db,
        CompanyRegistrationService sut,
        Guid registrationId)
    {
        var token = await db.CompanyRegistrations
            .Where(r => r.Id == registrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var verified = await sut.ActivateAsync(token);
        Assert.True(verified.EmailVerifiedAwaitingTakeover);
        Assert.NotNull(await db.CompanyRegistrations
            .Where(r => r.Id == registrationId)
            .Select(r => r.ContactEmailVerifiedAt)
            .SingleAsync());
    }

    private static CompanyRegistrationService CreateService(
        JobsyDbContext db,
        bool exposeActivationLinks = true)
    {
        var config = new ConfigurationBuilder().Build();
        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions
            {
                ExposeRegistrationActivationLinks = exposeActivationLinks
            }),
            config);

        return new CompanyRegistrationService(
            db,
            new TestKvkService(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            features,
            NullLogger<CompanyRegistrationService>.Instance);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class TestKvkService : IKvkService
    {
        private readonly JobsyDbContext _db;

        private static readonly Dictionary<string, string[]> SbiByKvk = new(StringComparer.Ordinal)
        {
            ["99990001"] = ["4711"],
            ["99990002"] = ["5229"],
            ["99990003"] = ["5610"],
            ["99990004"] = ["4120"],
            ["99990005"] = ["8710"],
            ["99990006"] = ["6201"],
            ["99990078"] = ["7820"]
        };

        private static readonly KvkEstablishmentResult[] Catalog =
        [
            new("99990001", "0001", "99990001_0001", "Nova Branch", "Straat 1", 52, 4, false),
            new("99990002", "0001", "99990002_0001", "Org HQ", "Straat 2", 52.1, 4.1, false),
            new("99990002", "0002", "99990002_0002", "Org Sibling", "Straat 3", 52.2, 4.2, false),
            new("99990003", "0001", "99990003_0001", "Conflict Branch", "Straat 4", 52.3, 4.3, false),
            new("99990004", "0001", "99990004_0001", "Dup Branch", "Straat 5", 52.4, 4.4, false),
            new("99990005", "0001", "99990005_0001", "BM Branch", "Straat 6", 52.5, 4.5, false),
            new("99990006", "0001", "99990006_0001", "Child", "Straat 7", 52.6, 4.6, false),
            new("99990078", "0001", "99990078_0001", "Flex Agency", "Straat 8", 52.7, 4.7, false)
        ];

        public TestKvkService(JobsyDbContext db) => _db = db;

        public Task<KvkCompanyResult?> GetByKvkNumberAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var match = Catalog.FirstOrDefault(c => c.KvkNumber == kvkNumber);
            if (match is null)
            {
                return Task.FromResult<KvkCompanyResult?>(null);
            }

            SbiByKvk.TryGetValue(kvkNumber, out var sbi);
            return Task.FromResult<KvkCompanyResult?>(
                new KvkCompanyResult(match.KvkNumber, match.Name, match.Address, sbi));
        }

        public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var lookup = await LookupEstablishmentsAsync(kvkNumber, cancellationToken);
            return lookup.Establishments;
        }

        public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var inUse = await _db.Companies.AsNoTracking()
                .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);

            SbiByKvk.TryGetValue(kvkNumber, out var sbi);
            var items = Catalog
                .Where(c => c.KvkNumber == kvkNumber)
                .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId), SbiCodes = sbi })
                .ToList();
            return items.Count == 0
                ? KvkEstablishmentsLookup.NotFound()
                : KvkEstablishmentsLookup.Ok(items);
        }
    }
}
