using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class CandidateInsightsLockedJsonTests : IClassFixture<CandidateInsightsUnlockFactory>
{
    private readonly CandidateInsightsUnlockFactory _factory;

    public CandidateInsightsLockedJsonTests(CandidateInsightsUnlockFactory factory) => _factory = factory;

    [Fact]
    public async Task Locked_json_omits_premium_values_for_bm_rm_vm()
    {
        foreach (var userId in new[] { _factory.EnterpriseUserId, _factory.RegionalUserId, _factory.BranchUserId })
        {
            using var client = Authed(userId);
            using var response = await client.GetAsync(
                $"api/employer/candidate-insights?branchId={_factory.BranchId}&radiusKm=20&period=90");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            Assert.False(root.GetProperty("scope").GetProperty("isFullAccess").GetBoolean());

            foreach (var key in InsightsLockedKeys.All)
            {
                var camel = char.ToLowerInvariant(key[0]) + key[1..];
                if (root.TryGetProperty("kpis", out var kpis) && kpis.TryGetProperty(camel, out var kpiVal))
                {
                    Assert.Equal(JsonValueKind.Null, kpiVal.ValueKind);
                }
                else if (root.TryGetProperty(camel, out var section))
                {
                    if (section.ValueKind == JsonValueKind.Array)
                    {
                        Assert.Empty(section.EnumerateArray());
                    }
                    else
                    {
                        Assert.Equal(JsonValueKind.Null, section.ValueKind);
                    }
                }
            }

            var locked = root.GetProperty("lockedSections").EnumerateArray().Select(e => e.GetString()).ToHashSet();
            foreach (var key in InsightsLockedKeys.All)
            {
                Assert.Contains(key, locked);
            }
        }
    }

    [Fact]
    public async Task Unlocked_json_includes_premium_sections()
    {
        await _factory.SeedCompanyUnlockAsync();
        using var client = Authed(_factory.EnterpriseUserId);
        using var response = await client.GetAsync(
            $"api/employer/candidate-insights?branchId={_factory.BranchId}&radiusKm=20&period=90");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("scope").GetProperty("isFullAccess").GetBoolean());
        Assert.Empty(doc.RootElement.GetProperty("lockedSections").EnumerateArray());
        Assert.NotEqual(JsonValueKind.Null, doc.RootElement.GetProperty("kpis").GetProperty("matchingYourVacancies").ValueKind);
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}

public class CandidateInsightsAccessTests
{
    [Fact]
    public void Company_unlock_covers_all_branches()
    {
        var org = new Company { Id = Guid.NewGuid(), Name = "Org" };
        var b1 = new Company { Id = Guid.NewGuid(), Name = "B1", ParentCompanyId = org.Id, TokensManagedByEnterprise = true };
        var b2 = new Company { Id = Guid.NewGuid(), Name = "B2", ParentCompanyId = org.Id, TokensManagedByEnterprise = true };
        var now = DateTime.UtcNow;
        var unlocks = new[]
        {
            new CandidateInsightsUnlock
            {
                WalletCompanyId = org.Id,
                ScopeKind = CandidateInsightsUnlockScopeKind.Company,
                ScopeCompanyId = org.Id,
                ExpiresAtUtc = now.AddDays(30)
            }
        };
        var coverage = CandidateInsightsAccess.GetCoverage([b1, b2], unlocks, now);
        Assert.True(coverage.IsFull);
        Assert.Equal(2, coverage.CoveredCount);
        Assert.False(coverage.CanRenew);
    }

    [Fact]
    public void Partial_branch_coverage_is_not_full()
    {
        var org = new Company { Id = Guid.NewGuid(), Name = "Org" };
        var b1 = new Company { Id = Guid.NewGuid(), Name = "B1", ParentCompanyId = org.Id };
        var b2 = new Company { Id = Guid.NewGuid(), Name = "B2", ParentCompanyId = org.Id };
        var now = DateTime.UtcNow;
        var unlocks = new[]
        {
            new CandidateInsightsUnlock
            {
                WalletCompanyId = b1.Id,
                ScopeKind = CandidateInsightsUnlockScopeKind.Branch,
                ScopeCompanyId = b1.Id,
                ExpiresAtUtc = now.AddDays(10)
            }
        };
        var coverage = CandidateInsightsAccess.GetCoverage([b1, b2], unlocks, now);
        Assert.False(coverage.IsFull);
        Assert.Equal(1, coverage.CoveredCount);
        Assert.Equal(2, coverage.TotalCount);
        Assert.True(coverage.CanRenew);
    }

    [Fact]
    public void Exactly_at_expiry_is_locked()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "C" };
        var exp = DateTime.UtcNow;
        var unlocks = new[]
        {
            new CandidateInsightsUnlock
            {
                WalletCompanyId = company.Id,
                ScopeKind = CandidateInsightsUnlockScopeKind.Company,
                ScopeCompanyId = company.Id,
                ExpiresAtUtc = exp
            }
        };
        var coverage = CandidateInsightsAccess.GetCoverage([company], unlocks, exp);
        Assert.False(coverage.IsFull);
    }

    [Fact]
    public void Branch_unlock_stays_valid_when_settings_switch_to_company()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "C" };
        var now = DateTime.UtcNow;
        var unlocks = new[]
        {
            new CandidateInsightsUnlock
            {
                WalletCompanyId = company.Id,
                ScopeKind = CandidateInsightsUnlockScopeKind.Branch,
                ScopeCompanyId = company.Id,
                ExpiresAtUtc = now.AddDays(40)
            }
        };
        var coverage = CandidateInsightsAccess.GetCoverage([company], unlocks, now);
        Assert.True(coverage.IsFull);
    }

    [Fact]
    public void CanRenew_within_14_days()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "C" };
        var now = DateTime.UtcNow;
        var unlocks = new[]
        {
            new CandidateInsightsUnlock
            {
                WalletCompanyId = company.Id,
                ScopeKind = CandidateInsightsUnlockScopeKind.Company,
                ScopeCompanyId = company.Id,
                ExpiresAtUtc = now.AddDays(14)
            }
        };
        Assert.True(CandidateInsightsAccess.GetCoverage([company], unlocks, now).CanRenew);
        Assert.False(CandidateInsightsAccess.GetCoverage([company], unlocks, now.AddDays(-1)).CanRenew);
    }
}

public class CandidateInsightsUnlockApiTests : IClassFixture<CandidateInsightsUnlockFactory>
{
    private readonly CandidateInsightsUnlockFactory _factory;

    public CandidateInsightsUnlockApiTests(CandidateInsightsUnlockFactory factory) => _factory = factory;

    [Fact]
    public async Task Bm_unlock_spends_once_and_is_idempotent()
    {
        await _factory.ResetUnlocksAsync();
        await _factory.GrantTokensAsync(_factory.OrgId, 50);
        await _factory.EnsureSpendCostAsync();

        using var client = Authed(_factory.EnterpriseUserId);
        var key = Guid.NewGuid().ToString("N");
        using var req1 = new HttpRequestMessage(HttpMethod.Post, "api/employer/candidate-insights/unlock");
        req1.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        req1.Content = JsonContent.Create(new { scope = "company", branchId = (Guid?)null });
        using var res1 = await client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        using var req2 = new HttpRequestMessage(HttpMethod.Post, "api/employer/candidate-insights/unlock");
        req2.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        req2.Content = JsonContent.Create(new { scope = "company", branchId = (Guid?)null });
        using var res2 = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.Equal(1, await db.CandidateInsightsUnlocks.CountAsync());
        Assert.Equal(1, await db.TokenTransactions.CountAsync(t => t.Reason == TokenSpendReason.InsightsUnlock));
    }

    [Fact]
    public async Task Insufficient_balance_returns_402()
    {
        await _factory.ResetUnlocksAsync();
        await _factory.GrantTokensAsync(_factory.OrgId, 0);
        await _factory.EnsureSpendCostAsync();
        using var client = Authed(_factory.EnterpriseUserId);
        using var req = new HttpRequestMessage(HttpMethod.Post, "api/employer/candidate-insights/unlock");
        req.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString("N"));
        req.Content = JsonContent.Create(new { scope = "company" });
        using var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.PaymentRequired, res.StatusCode);
    }

    [Fact]
    public async Task Rm_unlock_forbidden()
    {
        await _factory.ResetUnlocksAsync();
        await _factory.GrantTokensAsync(_factory.OrgId, 50);
        await _factory.EnsureSpendCostAsync();
        using var client = Authed(_factory.RegionalUserId);
        using var req = new HttpRequestMessage(HttpMethod.Post, "api/employer/candidate-insights/unlock");
        req.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString("N"));
        req.Content = JsonContent.Create(new { scope = "company" });
        using var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Export_locked_is_403_full_is_csv()
    {
        await _factory.ResetUnlocksAsync();
        using var lockedClient = Authed(_factory.EnterpriseUserId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await lockedClient.GetAsync($"api/employer/candidate-insights/export.csv?branchId={_factory.BranchId}&radiusKm=20&period=90")).StatusCode);

        await _factory.SeedCompanyUnlockAsync();
        using var fullClient = Authed(_factory.EnterpriseUserId);
        using var csv = await fullClient.GetAsync($"api/employer/candidate-insights/export.csv?branchId={_factory.BranchId}&radiusKm=20&period=90");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Equal("text/csv", csv.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Feature_off_returns_404()
    {
        await _factory.SetInsightsEnabledAsync(false);
        try
        {
            using var client = Authed(_factory.EnterpriseUserId);
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchId}&radiusKm=20&period=90")).StatusCode);
        }
        finally
        {
            await _factory.SetInsightsEnabledAsync(true);
        }
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}

public class CandidateInsightsUnlockRequestTests : IClassFixture<CandidateInsightsUnlockFactory>
{
    private readonly CandidateInsightsUnlockFactory _factory;

    public CandidateInsightsUnlockRequestTests(CandidateInsightsUnlockFactory factory) => _factory = factory;

    [Fact]
    public async Task Vm_creates_request_and_max_one_open()
    {
        await _factory.ResetUnlocksAsync();
        using var client = Authed(_factory.BranchUserId);
        using var first = await client.PostAsJsonAsync(
            "api/employer/candidate-insights/unlock-request",
            new { branchId = _factory.BranchId });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var second = await client.PostAsJsonAsync(
            "api/employer/candidate-insights/unlock-request",
            new { branchId = _factory.BranchId });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}

public sealed class CandidateInsightsUnlockFactory : WebApplicationFactory<Program>
{
    public Guid OrgId { get; } = Guid.Parse("f1000000-0000-0000-0000-000000000001");
    public Guid BranchId { get; } = Guid.Parse("f1000000-0000-0000-0000-000000000011");
    public Guid EnterpriseUserId { get; } = Guid.Parse("f1000000-0000-0000-0000-000000000051");
    public Guid BranchUserId { get; } = Guid.Parse("f1000000-0000-0000-0000-000000000052");
    public Guid RegionalUserId { get; } = Guid.Parse("f1000000-0000-0000-0000-000000000054");

    private readonly string _dbName = "CandInsightsUnlock-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(JobsyDbContext)
                    || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContext", StringComparison.Ordinal))
                    || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                    || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                        && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                .ToList();
            foreach (var d in efDescriptors)
            {
                services.Remove(d);
            }

            foreach (var d in services.Where(d =>
                         d.ServiceType.IsGenericType
                         && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                         && d.ServiceType.GenericTypeArguments[0] == typeof(JobsyDbContext)).ToList())
            {
                services.Remove(d);
            }

            services.AddDbContext<JobsyDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    private void EnsureSeeded()
    {
        if (_seeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (db.Users.Any())
        {
            _seeded = true;
            return;
        }

        db.Companies.AddRange(
            new Company { Id = OrgId, Name = "Org", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4.2) },
            new Company { Id = BranchId, Name = "Branch", KvkNumber = "2", Address = "b", ParentCompanyId = OrgId, TokensManagedByEnterprise = true, Location = new GeoPoint(52.01, 4.21) });
        db.Users.AddRange(
            new User { Id = EnterpriseUserId, Email = "em@unlock.local", FullName = "EM", Role = UserRole.EnterpriseManager, IsActive = true, CompanyId = OrgId },
            new User { Id = BranchUserId, Email = "bm@unlock.local", FullName = "BM", Role = UserRole.BranchManager, IsActive = true, CompanyId = BranchId },
            new User { Id = RegionalUserId, Email = "rm@unlock.local", FullName = "RM", Role = UserRole.RegionalManager, IsActive = true, CompanyId = BranchId });
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            CandidateInsightsEnabled = true,
            CandidateInsightsUnlockDays = 90,
            CandidateInsightsUnlockPerBranch = false
        });
        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.InsightsUnlock,
            CostTokens = 12m,
            IsActive = true
        });
        db.SaveChanges();
        _seeded = true;
    }

    public async Task ResetUnlocksAsync()
    {
        EnsureSeeded();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.CandidateInsightsUnlocks.RemoveRange(db.CandidateInsightsUnlocks);
        db.CandidateInsightsUnlockRequests.RemoveRange(db.CandidateInsightsUnlockRequests);
        db.TokenTransactions.RemoveRange(db.TokenTransactions.Where(t => t.Reason == TokenSpendReason.InsightsUnlock));
        await db.SaveChangesAsync();
    }

    public async Task GrantTokensAsync(Guid companyId, decimal amount)
    {
        EnsureSeeded();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.TokenTransactions.RemoveRange(db.TokenTransactions.Where(t => t.CompanyId == companyId));
        if (amount > 0)
        {
            db.TokenTransactions.Add(new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Amount = amount,
                Kind = TokenTransactionKind.Grant,
                Reason = TokenSpendReason.None,
                OldBalance = 0,
                NewBalance = amount,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task EnsureSpendCostAsync()
    {
        EnsureSeeded();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (!await db.TokenSpendCosts.AnyAsync(c => c.Reason == TokenSpendReason.InsightsUnlock))
        {
            db.TokenSpendCosts.Add(new TokenSpendCost
            {
                Id = Guid.NewGuid(),
                Reason = TokenSpendReason.InsightsUnlock,
                CostTokens = 12m,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task SeedCompanyUnlockAsync()
    {
        await ResetUnlocksAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var tx = new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = OrgId,
            Amount = -12,
            Kind = TokenTransactionKind.Spend,
            Reason = TokenSpendReason.InsightsUnlock,
            OldBalance = 50,
            NewBalance = 38,
            ActorUserId = EnterpriseUserId,
            CreatedAt = DateTime.UtcNow
        };
        db.TokenTransactions.Add(tx);
        db.CandidateInsightsUnlocks.Add(new CandidateInsightsUnlock
        {
            Id = Guid.NewGuid(),
            WalletCompanyId = OrgId,
            ScopeKind = CandidateInsightsUnlockScopeKind.Company,
            ScopeCompanyId = OrgId,
            UnlockedAtUtc = DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(60),
            PriceTokens = 12,
            DurationDays = 90,
            ActorUserId = EnterpriseUserId,
            TokenTransactionId = tx.Id,
            IdempotencyKey = "seed-" + Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
    }

    public async Task SetInsightsEnabledAsync(bool enabled)
    {
        EnsureSeeded();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstAsync();
        row.CandidateInsightsEnabled = enabled;
        await db.SaveChangesAsync();
    }
}
