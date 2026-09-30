using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Authorization;
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

public class TokenRequestsApiTests : IClassFixture<TokenRequestsApiFactory>
{
    private readonly TokenRequestsApiFactory _factory;
    public TokenRequestsApiTests(TokenRequestsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Checkout_vm_and_rm_forbidden_bm_not_forbidden()
    {
        using var vm = Authed(_factory.VmId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await vm.PostAsJsonAsync("api/tokens/checkout", new { companyId = _factory.OrgId, packSize = 5, paymentMethod = "ideal" })).StatusCode);

        using var rm = Authed(_factory.RmId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await rm.PostAsJsonAsync("api/tokens/checkout", new { companyId = _factory.OrgId, packSize = 5, paymentMethod = "ideal" })).StatusCode);

        using var bm = Authed(_factory.BmId);
        var res = await bm.PostAsJsonAsync("api/tokens/checkout", new { companyId = _factory.OrgId, packSize = 5, paymentMethod = "ideal" });
        Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Allocate_vm_rm_forbidden()
    {
        using var vm = Authed(_factory.VmId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await vm.PostAsJsonAsync("api/tokens/allocate",
                new { fromCompanyId = _factory.OrgId, toCompanyId = _factory.BranchId, amount = 1 })).StatusCode);

        using var rm = Authed(_factory.RmId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await rm.PostAsJsonAsync("api/tokens/allocate",
                new { fromCompanyId = _factory.OrgId, toCompanyId = _factory.BranchId, amount = 1 })).StatusCode);
    }

    [Fact]
    public async Task Billing_history_vm_rm_forbidden()
    {
        using var vm = Authed(_factory.VmId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await vm.GetAsync($"api/companies/{_factory.OrgId}/billing-history")).StatusCode);

        using var rm = Authed(_factory.RmId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await rm.GetAsync($"api/companies/{_factory.OrgId}/billing-history")).StatusCode);
    }

    [Fact]
    public async Task Token_requests_role_matrix()
    {
        using var vm = Authed(_factory.VmId);
        var createOwn = await vm.PostAsJsonAsync("api/werkgever/token-requests",
            new { branchCompanyId = _factory.BranchId, amount = 5, reason = "Publiceren", note = (string?)null });
        Assert.Equal(HttpStatusCode.OK, createOwn.StatusCode);

        var createSibling = await vm.PostAsJsonAsync("api/werkgever/token-requests",
            new { branchCompanyId = _factory.SiblingId, amount = 5, reason = "Publiceren", note = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, createSibling.StatusCode);

        using var rm = Authed(_factory.RmId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await rm.PostAsJsonAsync("api/werkgever/token-requests",
                new { branchCompanyId = _factory.BranchId, amount = 5, reason = "Publiceren" })).StatusCode);

        var created = await createOwn.Content.ReadFromJsonAsync<TokenRequestDto>();
        Assert.NotNull(created);

        using var vmApprove = Authed(_factory.VmId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await vmApprove.PostAsync($"api/werkgever/token-requests/{created!.Id}/approve", null)).StatusCode);

        using var bm = Authed(_factory.BmId);
        Assert.Equal(HttpStatusCode.OK,
            (await bm.PostAsync($"api/werkgever/token-requests/{created.Id}/approve", null)).StatusCode);
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}

public sealed class TokenRequestsApiFactory : WebApplicationFactory<Program>
{
    public Guid OrgId { get; } = Guid.Parse("a6000000-0000-0000-0000-000000000001");
    public Guid BranchId { get; } = Guid.Parse("a6000000-0000-0000-0000-000000000011");
    public Guid SiblingId { get; } = Guid.Parse("a6000000-0000-0000-0000-000000000012");
    public Guid BmId { get; } = Guid.Parse("a6000000-0000-0000-0000-000000000051");
    public Guid VmId { get; } = Guid.Parse("a6000000-0000-0000-0000-000000000052");
    public Guid RmId { get; } = Guid.Parse("a6000000-0000-0000-0000-000000000053");

    private readonly string _dbName = "TokReqApi-" + Guid.NewGuid();
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
            foreach (var d in efDescriptors) services.Remove(d);
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
        if (_seeded) return;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (db.Users.Any()) { _seeded = true; return; }

        db.Companies.AddRange(
            new Company { Id = OrgId, Name = "Org", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4) },
            new Company
            {
                Id = BranchId,
                Name = "Branch",
                KvkNumber = "1",
                Address = "b",
                ParentCompanyId = OrgId,
                TokensManagedByEnterprise = true,
                Location = new GeoPoint(52, 4)
            },
            new Company
            {
                Id = SiblingId,
                Name = "Sibling",
                KvkNumber = "1",
                Address = "c",
                ParentCompanyId = OrgId,
                TokensManagedByEnterprise = true,
                Location = new GeoPoint(52, 4)
            });
        db.Users.AddRange(
            new User { Id = BmId, Email = "bm@tok.local", FullName = "BM", Role = UserRole.EnterpriseManager, IsActive = true, CompanyId = OrgId },
            new User { Id = VmId, Email = "vm@tok.local", FullName = "VM", Role = UserRole.BranchManager, IsActive = true, CompanyId = BranchId },
            new User { Id = RmId, Email = "rm@tok.local", FullName = "RM", Role = UserRole.RegionalManager, IsActive = true, CompanyId = OrgId });
        db.UserCompanies.Add(new UserCompany { UserId = VmId, CompanyId = BranchId });
        db.TokenPricings.Add(new TokenPricing { Id = Guid.NewGuid(), PackSize = 5, PriceEuro = 50m, IsActive = true });
        db.SaveChanges();
        new TokenLedgerService(db).GrantAsync(OrgId, 100).GetAwaiter().GetResult();
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
