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
            AcceptedTerms: true));

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
        Assert.False(string.IsNullOrWhiteSpace(activated.TemporaryPassword));
        Assert.Equal(1, await db.Companies.CountAsync(c => c.KvkEstablishmentId == "99990001_0001"));
        Assert.True(await db.LocalAuthCredentials.AnyAsync(c => c.Email == "nova.branch@jobsy.local"));

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
            AcceptedTerms: true));

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
            "A", "hidden.url@jobsy.local", null, AcceptedTerms: true));

        Assert.Null(submit.ActivationUrl);
        Assert.False(string.IsNullOrEmpty(
            await db.CompanyRegistrations.Where(r => r.Id == submit.RegistrationId)
                .Select(r => r.ActivationToken).SingleAsync()));
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
            AcceptedTerms: true));

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
            "Sibling Pending", "sibling.pending@jobsy.local", null, AcceptedTerms: true));

        var orgSubmit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990002", "99990002_0001", RegistrationScope.Organization,
            "Org Manager", "org.skip@jobsy.local", null, AcceptedTerms: true));

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
            "A", "a@jobsy.local", null, AcceptedTerms: true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SubmitAsync(
            new RegistrationSubmitRequest(
                "99990004", "99990004_0001", RegistrationScope.BranchOnly,
                "B", "b2@jobsy.local", null, AcceptedTerms: true)));
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
            AcceptedTerms: true));

        Assert.True(submit.RequiresTakeover);
        Assert.Equal(CompanyRegistrationStatus.TakeoverPending, submit.Status);

        var takeoverId = await db.EstablishmentTakeoverRequests
            .Where(t => t.RegistrationId == submit.RegistrationId)
            .Select(t => t.Id)
            .SingleAsync();

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
            "Req", "req.org@jobsy.local", null, AcceptedTerms: true));
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
            "A", "a@jobsy.local", null, AcceptedTerms: true));
        var token = await db.CompanyRegistrations.Where(r => r.Id == first.RegistrationId)
            .Select(r => r.ActivationToken).SingleAsync();
        await sut.ActivateAsync(token);

        var second = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990004", "99990004_0001", RegistrationScope.BranchOnly,
            "B", "b@jobsy.local", null, AcceptedTerms: true));
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
            "New EM", "new.em@jobsy.local", null, AcceptedTerms: true));
        var takeoverId = await db.EstablishmentTakeoverRequests
            .Where(t => t.RegistrationId == submit.RegistrationId).Select(t => t.Id).SingleAsync();

        var decision = await sut.ApproveTakeoverAsync(
            takeoverId, emId, UserRole.EnterpriseManager, [parentId, branchId], false);

        Assert.Equal(parentId, decision.OrganizationCompanyId);
        Assert.Equal(1, await db.Companies.CountAsync(c =>
            c.KvkNumber == "99990006" && c.KvkEstablishmentId == null));
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

        private static readonly KvkEstablishmentResult[] Catalog =
        [
            new("99990001", "0001", "99990001_0001", "Nova Branch", "Straat 1", 52, 4, false),
            new("99990002", "0001", "99990002_0001", "Org HQ", "Straat 2", 52.1, 4.1, false),
            new("99990002", "0002", "99990002_0002", "Org Sibling", "Straat 3", 52.2, 4.2, false),
            new("99990003", "0001", "99990003_0001", "Conflict Branch", "Straat 4", 52.3, 4.3, false),
            new("99990004", "0001", "99990004_0001", "Dup Branch", "Straat 5", 52.4, 4.4, false),
            new("99990005", "0001", "99990005_0001", "BM Branch", "Straat 6", 52.5, 4.5, false),
            new("99990006", "0001", "99990006_0001", "Child", "Straat 7", 52.6, 4.6, false)
        ];

        public TestKvkService(JobsyDbContext db) => _db = db;

        public Task<KvkCompanyResult?> GetByKvkNumberAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var match = Catalog.FirstOrDefault(c => c.KvkNumber == kvkNumber);
            return Task.FromResult(match is null
                ? null
                : new KvkCompanyResult(match.KvkNumber, match.Name, match.Address));
        }

        public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var inUse = await _db.Companies.AsNoTracking()
                .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);

            return Catalog
                .Where(c => c.KvkNumber == kvkNumber)
                .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId) })
                .ToList();
        }
    }
}
