using System.Net;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.Werkgever;

public class WerkgeverDashboardApiTests : IClassFixture<WerkgeverDashboardApiFactory>
{
    private readonly WerkgeverDashboardApiFactory _factory;

    public WerkgeverDashboardApiTests(WerkgeverDashboardApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_unauthorized()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync($"api/werkgever/dashboard?period=30d&companyIds={_factory.BranchA1Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync($"api/werkgever/te-doen?companyIds={_factory.BranchA1Id}")).StatusCode);
    }

    [Fact]
    public async Task Candidate_forbidden()
    {
        using var client = Authed(_factory.CandidateUserId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync($"api/werkgever/dashboard?period=30d&companyIds={_factory.BranchA1Id}")).StatusCode);
    }

    [Fact]
    public async Task Empty_companyIds_forbidden()
    {
        using var client = Authed(_factory.EnterpriseAId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("api/werkgever/dashboard?period=30d")).StatusCode);
    }

    [Fact]
    public async Task Foreign_companyIds_forbidden_for_bm_vm_rm()
    {
        using var bm = Authed(_factory.EnterpriseAId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await bm.GetAsync($"api/werkgever/dashboard?period=30d&companyIds={_factory.BranchBId}")).StatusCode);

        using var vm = Authed(_factory.BranchA1UserId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await vm.GetAsync($"api/werkgever/dashboard?period=30d&companyIds={_factory.BranchA2Id}")).StatusCode);

        using var rm = Authed(_factory.RegionalA1UserId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await rm.GetAsync($"api/werkgever/dashboard?period=30d&companyIds={_factory.BranchA2Id}")).StatusCode);
    }

    [Fact]
    public async Task Own_scope_ok()
    {
        using var bm = Authed(_factory.EnterpriseAId);
        var res = await bm.GetAsync(
            $"api/werkgever/dashboard?period=30d&companyIds={_factory.BranchA1Id}&companyIds={_factory.BranchA2Id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var todo = Authed(_factory.EnterpriseAId);
        Assert.Equal(HttpStatusCode.OK,
            (await todo.GetAsync($"api/werkgever/te-doen?companyIds={_factory.BranchA1Id}")).StatusCode);
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}

public sealed class WerkgeverDashboardApiFactory : WebApplicationFactory<Program>
{
    public Guid OrgAId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000001");
    public Guid BranchA1Id { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000011");
    public Guid BranchA2Id { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000012");
    public Guid BranchBId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000021");
    public Guid EnterpriseAId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000051");
    public Guid BranchA1UserId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000052");
    public Guid RegionalA1UserId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000054");
    public Guid CandidateUserId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000063");

    private readonly string _dbName = "WgDashApi-" + Guid.NewGuid();
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
            services.RemoveAll<IVacancyContentModerationService>();
            services.AddSingleton<IVacancyContentModerationService>(new AllowAllModeration());
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
            new Company { Id = OrgAId, Name = "OrgA", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4.2) },
            new Company { Id = BranchA1Id, Name = "A1", KvkNumber = "2", Address = "a1", ParentCompanyId = OrgAId, Location = new GeoPoint(52.01, 4.21) },
            new Company { Id = BranchA2Id, Name = "A2", KvkNumber = "3", Address = "a2", ParentCompanyId = OrgAId, Location = new GeoPoint(52.02, 4.22) },
            new Company { Id = BranchBId, Name = "B", KvkNumber = "4", Address = "b", Location = new GeoPoint(51.9, 4.3) });

        db.Users.AddRange(
            new User { Id = EnterpriseAId, Email = "em-a@wg.local", FullName = "EM", Role = UserRole.EnterpriseManager, IsActive = true, CompanyId = OrgAId },
            new User { Id = BranchA1UserId, Email = "bm-a1@wg.local", FullName = "BM1", Role = UserRole.BranchManager, IsActive = true, CompanyId = BranchA1Id },
            new User { Id = RegionalA1UserId, Email = "rm-a1@wg.local", FullName = "RM", Role = UserRole.RegionalManager, IsActive = true, CompanyId = BranchA1Id },
            new User { Id = CandidateUserId, Email = "cand@wg.local", FullName = "Cand", Role = UserRole.Candidate, IsActive = true });

        db.UserCompanies.Add(new UserCompany { UserId = RegionalA1UserId, CompanyId = BranchA1Id });
        db.SaveChanges();
        _seeded = true;
    }

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyContentModerationResult(true, null));
    }
}
