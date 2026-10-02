using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
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

public class VacanciesManageApiTests : IClassFixture<VacanciesManageApiFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly VacanciesManageApiFactory _factory;

    public VacanciesManageApiTests(VacanciesManageApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Manage_scope_bm_sees_all_org_branches_rm_region_vm_own()
    {
        using var bm = Authed(_factory.EnterpriseAId);
        var bmList = await bm.GetFromJsonAsync<List<JsonElement>>("api/vacancies/manage", JsonOpts);
        Assert.NotNull(bmList);
        Assert.Contains(bmList, v => v.GetProperty("id").GetGuid() == _factory.VacancyA1Id);
        Assert.Contains(bmList, v => v.GetProperty("id").GetGuid() == _factory.VacancyA2Id);
        Assert.DoesNotContain(bmList, v => v.GetProperty("id").GetGuid() == _factory.VacancyBId);

        using var rm = Authed(_factory.RegionalA1UserId);
        var rmList = await rm.GetFromJsonAsync<List<JsonElement>>("api/vacancies/manage", JsonOpts);
        Assert.NotNull(rmList);
        Assert.Contains(rmList, v => v.GetProperty("id").GetGuid() == _factory.VacancyA1Id);
        Assert.DoesNotContain(rmList, v => v.GetProperty("id").GetGuid() == _factory.VacancyA2Id);

        using var vm = Authed(_factory.BranchA1UserId);
        var vmList = await vm.GetFromJsonAsync<List<JsonElement>>("api/vacancies/manage", JsonOpts);
        Assert.NotNull(vmList);
        Assert.Contains(vmList, v => v.GetProperty("id").GetGuid() == _factory.VacancyA1Id);
        Assert.DoesNotContain(vmList, v => v.GetProperty("id").GetGuid() == _factory.VacancyA2Id);
    }

    [Fact]
    public async Task Manage_companyIds_filter_intersects_server_side()
    {
        using var bm = Authed(_factory.EnterpriseAId);
        var filtered = await bm.GetFromJsonAsync<List<JsonElement>>(
            $"api/vacancies/manage?companyIds={_factory.BranchA1Id}", JsonOpts);
        Assert.NotNull(filtered);
        Assert.All(filtered, v => Assert.Equal(_factory.BranchA1Id, v.GetProperty("companyId").GetGuid()));
        Assert.DoesNotContain(filtered, v => v.GetProperty("id").GetGuid() == _factory.VacancyA2Id);

        var empty = await bm.GetFromJsonAsync<List<JsonElement>>(
            $"api/vacancies/manage?companyIds={_factory.BranchBId}", JsonOpts);
        Assert.NotNull(empty);
        Assert.Empty(empty);
    }

    [Theory]
    [InlineData("POST", "api/vacancies")]
    [InlineData("PUT", "api/vacancies/{id}")]
    [InlineData("POST", "api/vacancies/publish")]
    [InlineData("POST", "api/vacancies/{id}/approve-publish")]
    [InlineData("POST", "api/vacancies/{id}/highlight")]
    [InlineData("POST", "api/vacancies/{id}/pushbom")]
    [InlineData("POST", "api/vacancies/{id}/extend")]
    [InlineData("POST", "api/vacancies/{id}/inactive")]
    public async Task Rm_gets_403_on_mutating_vacancy_endpoints(string method, string template)
    {
        using var client = Authed(_factory.RegionalA1UserId);
        using var response = await SendAsync(client, method, Expand(template));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Vm_gets_403_on_approve_publish_and_sibling_vacancy()
    {
        using var vm = Authed(_factory.BranchA1UserId);
        using var approve = await vm.PostAsync(
            $"api/vacancies/{_factory.VacancyA1Id}/approve-publish", null);
        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);

        using var sibling = await vm.PostAsync(
            $"api/vacancies/{_factory.VacancyA2Id}/highlight", null);
        Assert.True(
            sibling.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"Expected 403/404 for sibling, got {sibling.StatusCode}");
    }

    [Fact]
    public async Task Bm_gets_non_403_on_lifecycle_endpoints()
    {
        using var bm = Authed(_factory.EnterpriseAId);
        using var highlight = await bm.PostAsync(
            $"api/vacancies/{_factory.VacancyA1Id}/highlight", null);
        Assert.NotEqual(HttpStatusCode.Forbidden, highlight.StatusCode);
        Assert.True((int)highlight.StatusCode is >= 200 and < 500);

        using var approve = await bm.PostAsync(
            $"api/vacancies/{_factory.PendingVacancyId}/approve-publish", null);
        Assert.NotEqual(HttpStatusCode.Forbidden, approve.StatusCode);
        Assert.True((int)approve.StatusCode is >= 200 and < 500);
    }

    private string Expand(string template)
        => template
            .Replace("{id}", _factory.VacancyA1Id.ToString("D"), StringComparison.Ordinal);

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url)
    {
        if (url.Contains("/publish", StringComparison.Ordinal)
            && !url.Contains("approve-publish", StringComparison.Ordinal)
            && method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("/vacancies/", StringComparison.Ordinal))
        {
            return await client.PostAsJsonAsync(url, new
            {
                vacancyId = Guid.Parse("f2000000-0000-0000-0000-000000000101"),
                highlight = false,
                pushBom = false,
                extend = false
            });
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
        {
            return await client.PutAsJsonAsync(url, new
            {
                title = "x",
                description = "y",
                companyId = Guid.Parse("f2000000-0000-0000-0000-000000000011"),
                startDate = DateOnly.FromDateTime(DateTime.UtcNow),
                endDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
                hourlyWage = 14m
            });
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && url == "api/vacancies")
        {
            return await client.PostAsJsonAsync(url, new
            {
                title = "x",
                description = "y",
                companyId = Guid.Parse("f2000000-0000-0000-0000-000000000011"),
                startDate = DateOnly.FromDateTime(DateTime.UtcNow),
                endDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
                hourlyWage = 14m
            });
        }

        return method.ToUpperInvariant() switch
        {
            "POST" => await client.PostAsync(url, null),
            "PUT" => await client.PutAsync(url, null),
            _ => throw new InvalidOperationException(method)
        };
    }
}

public sealed class VacanciesManageApiFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    public Guid OrgAId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000001");
    public Guid BranchA1Id { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000011");
    public Guid BranchA2Id { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000012");
    public Guid BranchBId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000021");
    public Guid EnterpriseAId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000051");
    public Guid BranchA1UserId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000052");
    public Guid RegionalA1UserId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000054");
    public Guid VacancyA1Id { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000101");
    public Guid VacancyA2Id { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000102");
    public Guid VacancyBId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000103");
    public Guid PendingVacancyId { get; } = Guid.Parse("f2000000-0000-0000-0000-000000000104");

    private readonly string _dbName = "WgVacManageApi-" + Guid.NewGuid();
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Companies.AddRange(
            new Company
            {
                Id = OrgAId,
                VerificationStatus = CompanyVerificationStatus.Verified,
                Name = "OrgA", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4.2) },
            new Company
            {
                
                VerificationStatus = CompanyVerificationStatus.Verified,
                Id = BranchA1Id,
                Name = "A1",
                KvkNumber = "2",
                Address = "a1",
                ParentCompanyId = OrgAId,
                Location = new GeoPoint(52.01, 4.21),
                TokensManagedByEnterprise = true
            },
            new Company
            {
                
                VerificationStatus = CompanyVerificationStatus.Verified,
                Id = BranchA2Id,
                Name = "A2",
                KvkNumber = "3",
                Address = "a2",
                ParentCompanyId = OrgAId,
                Location = new GeoPoint(52.02, 4.22),
                TokensManagedByEnterprise = true
            },
            new Company
            {
                Id = BranchBId,
                VerificationStatus = CompanyVerificationStatus.Verified,
                Name = "B", KvkNumber = "4", Address = "b", Location = new GeoPoint(51.9, 4.3) });

        db.Users.AddRange(
            new User
            {
                Id = EnterpriseAId,
                Email = "em-a@wgvac.local",
                FullName = "EM",
                Role = UserRole.EnterpriseManager,
                IsActive = true,
                CompanyId = OrgAId
            },
            new User
            {
                Id = BranchA1UserId,
                Email = "bm-a1@wgvac.local",
                FullName = "Sanne Bakker",
                FirstName = "Sanne",
                LastName = "Bakker",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = BranchA1Id
            },
            new User
            {
                Id = RegionalA1UserId,
                Email = "rm-a1@wgvac.local",
                FullName = "RM",
                Role = UserRole.RegionalManager,
                IsActive = true,
                CompanyId = BranchA1Id
            });

        db.UserCompanies.AddRange(
            new UserCompany { UserId = EnterpriseAId, CompanyId = OrgAId },
            new UserCompany { UserId = EnterpriseAId, CompanyId = BranchA1Id },
            new UserCompany { UserId = EnterpriseAId, CompanyId = BranchA2Id },
            new UserCompany { UserId = BranchA1UserId, CompanyId = BranchA1Id },
            new UserCompany { UserId = RegionalA1UserId, CompanyId = BranchA1Id });

        Vacancy Make(Guid id, Guid companyId, string title, VacancyStatus status) => new()
        {
            Id = id,
            Title = title,
            Description = "Omschrijving lang genoeg.",
            HourlyWage = 14,
            StartDate = today,
            EndDate = today.AddDays(30),
            Status = status,
            CompanyId = companyId,
            Location = new GeoPoint(52.01, 4.21),
            ContentModerationPassed = true,
            WorkTypeLabels = "Tuinbouw"
        };

        db.Vacancies.AddRange(
            Make(VacancyA1Id, BranchA1Id, "Kas A1", VacancyStatus.Active),
            Make(VacancyA2Id, BranchA2Id, "Kas A2", VacancyStatus.Active),
            Make(VacancyBId, BranchBId, "Kas B", VacancyStatus.Active),
            Make(PendingVacancyId, BranchA1Id, "Pending A1", VacancyStatus.PendingApproval));

        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = 2m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m, IsActive = true });

        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = OrgAId,
            Amount = 100m,
            Kind = TokenTransactionKind.Grant,
            Reason = TokenSpendReason.None,
            OldBalance = 0,
            NewBalance = 100m,
            CreatedAt = DateTime.UtcNow
        });

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
