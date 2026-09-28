using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class CandidateInsightsServiceTests
{
    private static readonly GeoPoint BranchLoc = new(52.0, 4.3);

    [Fact]
    public async Task Cohort_of_9_returns_insufficient_kpis()
    {
        await using var db = CreateDb();
        var company = SeedCompany(db);
        SeedCandidates(db, company.Location!, count: 9, withConsent: true);
        await db.SaveChangesAsync();

        var svc = CreateService(db, out var tokens);
        tokens.Balance = 10;
        var dto = await svc.GetInsightsAsync(Principal(JobsyRoles.BranchManager, company.Id), company.Id, 20, 90);

        Assert.Equal(CandidateInsightsPrivacy.StatusInsufficient, dto.Kpis.CandidatesInRadius.Status);
        Assert.Null(dto.Kpis.CandidatesInRadius.Value);
        Assert.Empty(dto.DreamJobsTop);
        Assert.Empty(dto.Density);
    }

    [Fact]
    public async Task Cohort_of_10_is_ok_and_dream_keys_under_10_dropped()
    {
        await using var db = CreateDb();
        var company = SeedCompany(db);
        var users = SeedCandidates(db, company.Location!, count: 12, withConsent: true);
        // 10 share dream A, 2 share dream B → B dropped
        for (var i = 0; i < users.Count; i++)
        {
            db.CandidateCareerPlans.Add(new CandidateCareerPlan
            {
                Id = Guid.NewGuid(),
                UserId = users[i].Id,
                DreamKey = i < 10 ? "barista" : "rare",
                DreamTitle = i < 10 ? "Barista" : "Rare Job",
                PlanJson = "[]",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        var svc = CreateService(db, out var tokens);
        tokens.Balance = 5;
        var dto = await svc.GetInsightsAsync(Principal(JobsyRoles.BranchManager, company.Id), company.Id, 20, 90);

        Assert.Equal(CandidateInsightsPrivacy.StatusOk, dto.Kpis.CandidatesInRadius.Status);
        Assert.Equal(10, dto.Kpis.CandidatesInRadius.Value); // 12 → rounded to 10? 12 rounds to 10
        Assert.Single(dto.DreamJobsTop);
        Assert.Equal("Barista", dto.DreamJobsTop[0].Label);
        Assert.DoesNotContain(dto.DreamJobsTop, d => d.Label.Contains("Rare", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Density_cells_under_10_omitted()
    {
        await using var db = CreateDb();
        var company = SeedCompany(db);
        // 12 candidates in same cell near branch
        SeedCandidates(db, company.Location!, count: 12, withConsent: true);
        await db.SaveChangesAsync();

        var svc = CreateService(db, out var tokens);
        tokens.Balance = 1;
        var dto = await svc.GetInsightsAsync(Principal(JobsyRoles.BranchManager, company.Id), company.Id, 20, 90);
        Assert.NotEmpty(dto.Density);
        Assert.All(dto.Density, c => Assert.InRange(c.Band, 1, 3));
    }

    [Fact]
    public async Task Differencing_does_not_reveal_ring_of_7()
    {
        await using var db = CreateDb();
        var company = SeedCompany(db);
        // 12 near origin, 7 at ~15 km
        SeedCandidates(db, company.Location!, count: 12, withConsent: true);
        var far = new GeoPoint(company.Location!.Latitude + (15.0 / 111.32), company.Location.Longitude);
        SeedCandidates(db, far, count: 7, withConsent: true, emailPrefix: "far");
        await db.SaveChangesAsync();

        var svc = CreateService(db, out var tokens);
        tokens.Balance = 1;
        var at10 = await svc.GetInsightsAsync(Principal(JobsyRoles.BranchManager, company.Id), company.Id, 10, 90);
        var at20 = await svc.GetInsightsAsync(Principal(JobsyRoles.BranchManager, company.Id), company.Id, 20, 90);

        Assert.Equal(CandidateInsightsPrivacy.StatusOk, at10.Kpis.CandidatesInRadius.Status);
        Assert.Equal(CandidateInsightsPrivacy.StatusOk, at20.Kpis.CandidatesInRadius.Status);
        // Rounded values: 12→10 and 19→20; difference must not equal 7
        var diff = (at20.Kpis.CandidatesInRadius.Value ?? 0) - (at10.Kpis.CandidatesInRadius.Value ?? 0);
        Assert.NotEqual(7, diff);
    }

    [Fact]
    public async Task Consent_and_inactive_and_minor_excluded()
    {
        await using var db = CreateDb();
        var company = SeedCompany(db);
        SeedCandidates(db, company.Location!, count: 12, withConsent: true);
        // extras that must not count
        db.Users.Add(MakeCandidate("noconsent@t.local", company.Location!, consent: false));
        db.Users.Add(MakeCandidate("inactive@t.local", company.Location!, consent: true, active: false));
        db.Users.Add(MakeCandidate("minor@t.local", company.Location!, consent: true, dob: new DateOnly(2015, 1, 1)));
        await db.SaveChangesAsync();

        var svc = CreateService(db, out var tokens);
        tokens.Balance = 1;
        var dto = await svc.GetInsightsAsync(Principal(JobsyRoles.BranchManager, company.Id), company.Id, 20, 90);
        Assert.Equal(10, dto.Kpis.CandidatesInRadius.Value); // 12 rounded
    }

    [Fact]
    public async Task Gate_balance_zero_locks_sections()
    {
        await using var db = CreateDb();
        var company = SeedCompany(db);
        var gated = SeedCandidates(db, company.Location!, count: 12, withConsent: true);
        for (var i = 0; i < gated.Count; i++)
        {
            db.CandidateCareerPlans.Add(new CandidateCareerPlan
            {
                Id = Guid.NewGuid(),
                UserId = gated[i].Id,
                DreamKey = "job" + (i % 5),
                DreamTitle = "Job " + (i % 5),
                PlanJson = "[]",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        var svc = CreateService(db, out var tokens);
        tokens.Balance = 0;
        var dto = await svc.GetInsightsAsync(Principal(JobsyRoles.BranchManager, company.Id), company.Id, 20, 90);

        Assert.False(dto.Scope.IsFullAccess);
        Assert.Contains(CandidateInsightsService.LockedDreamJobs4To10, dto.LockedSections);
        Assert.Contains(CandidateInsightsService.LockedDna, dto.LockedSections);
        Assert.Contains(CandidateInsightsService.LockedStory5To10, dto.LockedSections);
        Assert.Null(dto.DnaRiasec);
        Assert.Null(dto.Competences);
        Assert.True(dto.DreamJobsTop.Count <= 3);
    }

    [Fact]
    public async Task Gate_enterprise_managed_wallet_uses_parent()
    {
        await using var db = CreateDb();
        var parent = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Parent",
            KvkNumber = "1",
            Address = "x",
            Location = BranchLoc
        };
        var branch = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Child",
            KvkNumber = "2",
            Address = "y",
            ParentCompanyId = parent.Id,
            TokensManagedByEnterprise = true,
            Location = BranchLoc
        };
        db.Companies.AddRange(parent, branch);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "bm@t.local",
            FullName = "BM",
            Role = UserRole.BranchManager,
            IsActive = true,
            CompanyId = branch.Id
        });
        SeedCandidates(db, BranchLoc, 12, true);
        await db.SaveChangesAsync();

        var svc = CreateService(db, out var tokens);
        tokens.Balances[parent.Id] = 3;
        tokens.Balances[branch.Id] = 0;
        var dto = await svc.GetInsightsAsync(Principal(JobsyRoles.BranchManager, branch.Id), branch.Id, 20, 90);
        Assert.True(dto.Scope.IsFullAccess);
    }

    [Fact]
    public async Task Cache_second_call_ignores_db_mutations()
    {
        await using var db = CreateDb();
        var company = SeedCompany(db);
        SeedCandidates(db, company.Location!, 12, true);
        await db.SaveChangesAsync();

        var svc = CreateService(db, out var tokens);
        tokens.Balance = 1;
        var principal = Principal(JobsyRoles.BranchManager, company.Id);
        var first = await svc.GetInsightsAsync(principal, company.Id, 20, 90);

        // Mutate cohort after cache fill — second call must return the cached DTO.
        db.Users.RemoveRange(db.Users.Where(u => u.Role == UserRole.Candidate));
        await db.SaveChangesAsync();
        var second = await svc.GetInsightsAsync(principal, company.Id, 20, 90);
        Assert.Equal(first.Kpis.CandidatesInRadius.Value, second.Kpis.CandidatesInRadius.Value);
        Assert.Equal(first.Scope.GeneratedAtUtc, second.Scope.GeneratedAtUtc);
    }

    [Fact]
    public async Task Service_handles_10_and_200_without_n_plus_one_patterns()
    {
        var src = await File.ReadAllTextAsync(
            Path.Combine(TestRepo.FindRoot(), "Jobsy.Infrastructure", "Services", "CandidateInsightsService.cs"));
        Assert.DoesNotContain("ComputeLiveAsync", src, StringComparison.Ordinal);
        Assert.DoesNotContain("IRoutingService", src, StringComparison.Ordinal);
        // No await inside foreach over cohort users (N+1 smell).
        Assert.DoesNotContain("foreach (var user in cohortUsers)\n        {\n            await", src, StringComparison.Ordinal);

        await using var db10 = CreateDb();
        var c10 = SeedCompany(db10);
        SeedCandidates(db10, c10.Location!, 10, true);
        await db10.SaveChangesAsync();
        var s10 = CreateService(db10, out var t10);
        t10.Balance = 1;
        var d10 = await s10.GetInsightsAsync(Principal(JobsyRoles.BranchManager, c10.Id), c10.Id, 20, 90);
        Assert.Equal(CandidateInsightsPrivacy.StatusOk, d10.Kpis.CandidatesInRadius.Status);

        await using var db200 = CreateDb();
        var c200 = SeedCompany(db200);
        SeedCandidates(db200, c200.Location!, 200, true);
        await db200.SaveChangesAsync();
        var s200 = CreateService(db200, out var t200);
        t200.Balance = 1;
        var d200 = await s200.GetInsightsAsync(Principal(JobsyRoles.BranchManager, c200.Id), c200.Id, 20, 90);
        Assert.Equal(CandidateInsightsPrivacy.StatusOk, d200.Kpis.CandidatesInRadius.Status);
        Assert.True((d200.Kpis.CandidatesInRadius.Value ?? 0) >= (d10.Kpis.CandidatesInRadius.Value ?? 0));
    }

    private static CandidateInsightsService CreateService(JobsyDbContext db, out StubTokens tokens)
    {
        tokens = new StubTokens();
        var authz = new CompanyAuthorizationService(db);
        var users = new UserLookupService(db);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CandidateInsightsService(
            db, authz, users, tokens, cache, NullLogger<CandidateInsightsService>.Instance);
    }

    private static ClaimsPrincipal Principal(string role, Guid companyId)
    {
        var id = new ClaimsIdentity("test");
        id.AddClaim(new Claim(ClaimTypes.Role, role));
        id.AddClaim(new Claim(ClaimTypes.Email, role == JobsyRoles.BranchManager ? "bm@t.local" : "em@t.local"));
        // UserLookup finds by email — seed matching email when needed
        return new ClaimsPrincipal(id);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("insights-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private static Company SeedCompany(JobsyDbContext db)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Vestiging",
            KvkNumber = "123",
            Address = "Straat 1",
            Location = BranchLoc
        };
        db.Companies.Add(company);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "bm@t.local",
            FullName = "Branch",
            Role = UserRole.BranchManager,
            IsActive = true,
            CompanyId = company.Id
        });
        return company;
    }

    private static List<User> SeedCandidates(
        JobsyDbContext db,
        GeoPoint location,
        int count,
        bool withConsent,
        string emailPrefix = "c")
    {
        var list = new List<User>();
        for (var i = 0; i < count; i++)
        {
            var u = MakeCandidate($"{emailPrefix}{i}@t.local", location, withConsent);
            // slight jitter so density still clusters in same cell
            u.HomeLocation = new GeoPoint(
                location.Latitude + (i * 0.00001),
                location.Longitude + (i * 0.00001));
            db.Users.Add(u);
            list.Add(u);
        }

        return list;
    }

    private static User MakeCandidate(
        string email,
        GeoPoint location,
        bool consent,
        bool active = true,
        DateOnly? dob = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = email,
            Role = UserRole.Candidate,
            IsActive = active,
            HomeLocation = location,
            LastLoginAtUtc = DateTime.UtcNow.AddDays(-3),
            DateOfBirth = dob ?? new DateOnly(1995, 5, 5),
            TestAiConsentAt = consent ? DateTime.UtcNow.AddDays(-10) : null,
            TestAiConsentVersion = consent ? PrivacyConstants.CandidateProfilingConsentVersion : null,
            PreferencesJson = """{"Roles":["Winkel"],"MinHoursPerWeek":20,"MaxHoursPerWeek":32,"FlexibleTimes":true,"MaxTravelMinutes":15,"AvailabilityPresets":["parttime"]}"""
        };

    private sealed class StubTokens : ITokenLedgerService
    {
        public decimal Balance { get; set; }
        public Dictionary<Guid, decimal> Balances { get; } = new();

        public Task<decimal> GetBalanceAsync(Guid companyId, CancellationToken cancellationToken = default)
            => Task.FromResult(Balances.TryGetValue(companyId, out var b) ? b : Balance);

        public Task<decimal?> GetCostAsync(TokenSpendReason reason, CancellationToken cancellationToken = default)
            => Task.FromResult<decimal?>(1);

        public Task<IReadOnlyDictionary<TokenSpendReason, decimal>> GetCostsAsync(
            IEnumerable<TokenSpendReason> reasons, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<TokenSpendReason, decimal>>(
                reasons.ToDictionary(r => r, _ => 1m));

        public Task<TokenTransaction> GrantAsync(Guid companyId, decimal amount, Guid? actorUserId = null, string? note = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<TokenTransaction> GrantForCheckoutAsync(Guid companyId, decimal amount, Guid tokenPurchaseCheckoutId, Guid? actorUserId = null, string? note = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<TokenTransaction> GrantGoodwillAsync(Guid companyId, decimal amount, string reason, Guid? actorUserId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<TokenTransaction> RecordPurchaseAsync(Guid companyId, decimal amount, Guid? actorUserId = null, string? note = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<TokenTransaction> RecordPurchaseAsync(Guid companyId, decimal tokenAmount, int amountExVatCents, int vatAmountCents, int totalAmountCents, Guid? checkoutId, Guid? invoiceId, Guid? actorUserId = null, string? note = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<(TokenTransaction From, TokenTransaction To)> AllocateAsync(Guid fromCompanyId, Guid toCompanyId, decimal amount, Guid? actorUserId = null, string? note = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<TokenSpendOutcome> TrySpendAsync(Guid companyId, TokenSpendReason reason, Guid? vacancyId = null, Guid? actorUserId = null, Guid? branchCompanyId = null, string? note = null, Func<CancellationToken, Task>? onSuccessBeforeCommit = null, IReadOnlyDictionary<TokenSpendReason, decimal>? costOverrides = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<TokenMultiSpendOutcome> TrySpendManyAsync(Guid companyId, IReadOnlyList<TokenSpendReason> reasons, Guid? vacancyId = null, Guid? actorUserId = null, Guid? branchCompanyId = null, string? note = null, Func<CancellationToken, Task>? onSuccessBeforeCommit = null, IReadOnlyDictionary<TokenSpendReason, decimal>? costOverrides = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

}
