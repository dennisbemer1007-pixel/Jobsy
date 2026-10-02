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

namespace Jobsy.Tests;

public class CandidateInsightsAuthzApiTests : IClassFixture<CandidateInsightsAuthzFactory>
{
    private readonly CandidateInsightsAuthzFactory _factory;

    public CandidateInsightsAuthzApiTests(CandidateInsightsAuthzFactory factory) => _factory = factory;

    [Fact]
    public async Task Enterprise_own_ok_other_company_forbidden()
    {
        using var client = Authed(_factory.EnterpriseAId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=20&period=90")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchBId}&radiusKm=20&period=90")).StatusCode);
    }

    [Fact]
    public async Task Branch_own_ok_sibling_forbidden_omit_uses_own()
    {
        using var client = Authed(_factory.BranchA1UserId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=20&period=90")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA2Id}&radiusKm=20&period=90")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/employer/candidate-insights?radiusKm=20&period=90")).StatusCode);
    }

    [Fact]
    public async Task Regional_region_ok_others_forbidden()
    {
        using var client = Authed(_factory.RegionalA1UserId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=20&period=90")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA2Id}&radiusKm=20&period=90")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchBId}&radiusKm=20&period=90")).StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("intermediary")]
    [InlineData("candidate")]
    public async Task Disallowed_roles_forbidden(string kind)
    {
        var userId = kind switch
        {
            "admin" => _factory.AdminUserId,
            "intermediary" => _factory.IntermediaryUserId,
            _ => _factory.CandidateUserId
        };
        using var client = Authed(userId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/employer/candidate-insights?radiusKm=20&period=90")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_unauthorized()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/employer/candidate-insights?radiusKm=20&period=90")).StatusCode);
    }

    [Fact]
    public async Task Invalid_radius_or_period_bad_request()
    {
        using var client = Authed(_factory.BranchA1UserId);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=15&period=90")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=20&period=45")).StatusCode);
    }

    [Fact]
    public async Task Age_and_id_query_params_bad_request()
    {
        using var client = Authed(_factory.BranchA1UserId);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=20&period=90&minAge=18")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=20&period=90&userId={Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Cached_response_still_forbidden_for_other_tenant()
    {
        using var enterprise = Authed(_factory.EnterpriseAId);
        Assert.Equal(HttpStatusCode.OK, (await enterprise.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=20&period=90")).StatusCode);

        using var branchB = Authed(_factory.BranchBUserId);
        Assert.Equal(HttpStatusCode.Forbidden, (await branchB.GetAsync($"api/employer/candidate-insights?branchId={_factory.BranchA1Id}&radiusKm=20&period=90")).StatusCode);
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}

public sealed class CandidateInsightsAuthzFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    public Guid OrgAId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000001");
    public Guid BranchA1Id { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000011");
    public Guid BranchA2Id { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000012");
    public Guid BranchBId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000021");
    public Guid EnterpriseAId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000051");
    public Guid BranchA1UserId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000052");
    public Guid BranchBUserId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000053");
    public Guid RegionalA1UserId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000054");
    public Guid AdminUserId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000061");
    public Guid IntermediaryUserId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000062");
    public Guid CandidateUserId { get; } = Guid.Parse("e1000000-0000-0000-0000-000000000063");

    private readonly string _dbName = "CandInsightsAuthz-" + Guid.NewGuid();
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
            new Company { Id = OrgAId, Name = "OrgA", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4.2),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        },
            new Company { Id = BranchA1Id, Name = "A1", KvkNumber = "2", Address = "a1", ParentCompanyId = OrgAId, Location = new GeoPoint(52.01, 4.21),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        },
            new Company { Id = BranchA2Id, Name = "A2", KvkNumber = "3", Address = "a2", ParentCompanyId = OrgAId, Location = new GeoPoint(52.02, 4.22),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        },
            new Company { Id = BranchBId, Name = "B", KvkNumber = "4", Address = "b", Location = new GeoPoint(51.9, 4.3),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });

        db.Users.AddRange(
            new User { Id = EnterpriseAId, Email = "em-a@test.local", FullName = "EM", Role = UserRole.EnterpriseManager, IsActive = true, CompanyId = OrgAId },
            new User { Id = BranchA1UserId, Email = "bm-a1@test.local", FullName = "BM1", Role = UserRole.BranchManager, IsActive = true, CompanyId = BranchA1Id },
            new User { Id = BranchBUserId, Email = "bm-b@test.local", FullName = "BMB", Role = UserRole.BranchManager, IsActive = true, CompanyId = BranchBId },
            new User { Id = RegionalA1UserId, Email = "rm-a1@test.local", FullName = "RM", Role = UserRole.RegionalManager, IsActive = true, CompanyId = BranchA1Id },
            new User { Id = AdminUserId, Email = "admin@test.local", FullName = "Admin", Role = UserRole.Admin, IsActive = true },
            new User { Id = IntermediaryUserId, Email = "int@test.local", FullName = "Int", Role = UserRole.Intermediary, IsActive = true, CompanyId = OrgAId },
            new User { Id = CandidateUserId, Email = "cand@test.local", FullName = "Cand", Role = UserRole.Candidate, IsActive = true });

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
