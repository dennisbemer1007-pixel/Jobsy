using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
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

namespace Jobsy.Tests.Werkgever;

public class CompanyUsersOrgApiTests : IClassFixture<CompanyUsersOrgApiFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly CompanyUsersOrgApiFactory _factory;

    public CompanyUsersOrgApiTests(CompanyUsersOrgApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Rm_and_Vm_get_403_on_invite_and_update()
    {
        foreach (var userId in new[] { _factory.RegionalUserId, _factory.BranchUserId })
        {
            using var client = Authed(userId);
            using var invite = await client.PostAsJsonAsync("api/company-users/invite", new
            {
                email = $"x-{userId:N}@example.com",
                fullName = "X",
                role = "BranchManager",
                primaryCompanyId = _factory.BranchId
            });
            Assert.Equal(HttpStatusCode.Forbidden, invite.StatusCode);

            using var update = await client.PutAsJsonAsync($"api/company-users/{_factory.EnterpriseUserId}", new
            {
                fullName = "Nope",
                role = "EnterpriseManager",
                primaryCompanyId = _factory.OrgId,
                isActive = true
            });
            Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        }
    }

    [Fact]
    public async Task Last_active_enterprise_manager_demote_returns_409()
    {
        using var sole = Authed(_factory.SoleEnterpriseUserId);
        using var demoteSelf = await sole.PutAsJsonAsync($"api/company-users/{_factory.SoleEnterpriseUserId}", new
        {
            fullName = "Sole",
            role = "BranchManager",
            primaryCompanyId = _factory.SoleBranchId,
            membershipCompanyIds = new[] { _factory.SoleBranchId },
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Conflict, demoteSelf.StatusCode);
        var body = await demoteSelf.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("last_enterprise_manager", body.GetProperty("code").GetString());

        using var deactivateSelf = await sole.PutAsJsonAsync($"api/company-users/{_factory.SoleEnterpriseUserId}", new
        {
            fullName = "Sole",
            role = "EnterpriseManager",
            primaryCompanyId = _factory.SoleOrgId,
            isActive = false
        });
        Assert.Equal(HttpStatusCode.Conflict, deactivateSelf.StatusCode);
    }

    [Fact]
    public async Task Rm_can_list_regions_but_not_mutate()
    {
        using var rm = Authed(_factory.RegionalUserId);
        using var list = await rm.GetAsync("api/regions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        using var create = await rm.PostAsJsonAsync("api/regions", new
        {
            name = "Nope",
            organizationCompanyId = _factory.OrgId,
            companyIds = new[] { _factory.BranchId }
        });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}

public sealed class CompanyUsersOrgApiFactory : WebApplicationFactory<Program>
{
    public Guid OrgId { get; } = Guid.Parse("a3000000-0000-0000-0000-000000000001");
    public Guid BranchId { get; } = Guid.Parse("a3000000-0000-0000-0000-000000000011");
    public Guid EnterpriseUserId { get; } = Guid.Parse("a3000000-0000-0000-0000-000000000051");
    public Guid BranchUserId { get; } = Guid.Parse("a3000000-0000-0000-0000-000000000052");
    public Guid RegionalUserId { get; } = Guid.Parse("a3000000-0000-0000-0000-000000000054");
    public Guid SoleOrgId { get; } = Guid.Parse("a3000000-0000-0000-0000-000000000201");
    public Guid SoleBranchId { get; } = Guid.Parse("a3000000-0000-0000-0000-000000000211");
    public Guid SoleEnterpriseUserId { get; } = Guid.Parse("a3000000-0000-0000-0000-000000000251");

    private readonly string _dbName = "WgCompanyUsersOrg-" + Guid.NewGuid();
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
            new Company
            {
                Id = BranchId,
                Name = "Branch",
                KvkNumber = "2",
                Address = "b",
                ParentCompanyId = OrgId,
                Location = new GeoPoint(52.01, 4.21)
            },
            new Company { Id = SoleOrgId, Name = "SoleOrg", KvkNumber = "9", Address = "s", Location = new GeoPoint(52.1, 4.3) },
            new Company
            {
                Id = SoleBranchId,
                Name = "SoleBranch",
                KvkNumber = "8",
                Address = "sb",
                ParentCompanyId = SoleOrgId,
                Location = new GeoPoint(52.11, 4.31)
            });

        db.Users.AddRange(
            new User
            {
                Id = EnterpriseUserId,
                Email = "em@wgcu.local",
                FullName = "EM",
                Role = UserRole.EnterpriseManager,
                IsActive = true,
                CompanyId = OrgId
            },
            new User
            {
                Id = BranchUserId,
                Email = "vm@wgcu.local",
                FullName = "VM",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = BranchId
            },
            new User
            {
                Id = RegionalUserId,
                Email = "rm@wgcu.local",
                FullName = "RM",
                Role = UserRole.RegionalManager,
                IsActive = true,
                CompanyId = BranchId
            },
            new User
            {
                Id = SoleEnterpriseUserId,
                Email = "sole@wgcu.local",
                FullName = "Sole",
                Role = UserRole.EnterpriseManager,
                IsActive = true,
                CompanyId = SoleOrgId
            });

        db.UserCompanies.AddRange(
            new UserCompany { UserId = EnterpriseUserId, CompanyId = OrgId },
            new UserCompany { UserId = EnterpriseUserId, CompanyId = BranchId },
            new UserCompany { UserId = BranchUserId, CompanyId = BranchId },
            new UserCompany { UserId = RegionalUserId, CompanyId = BranchId },
            new UserCompany { UserId = SoleEnterpriseUserId, CompanyId = SoleOrgId },
            new UserCompany { UserId = SoleEnterpriseUserId, CompanyId = SoleBranchId });

        db.Regions.Add(new Region
        {
            Id = Guid.Parse("a3000000-0000-0000-0000-000000000331"),
            Name = "Regio",
            OrganizationCompanyId = OrgId
        });
        db.RegionCompanies.Add(new RegionCompany
        {
            RegionId = Guid.Parse("a3000000-0000-0000-0000-000000000331"),
            CompanyId = BranchId
        });

        db.SaveChanges();
        _seeded = true;
    }
}
