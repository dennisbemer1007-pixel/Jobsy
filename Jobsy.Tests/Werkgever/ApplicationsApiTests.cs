using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

namespace Jobsy.Tests.Werkgever;

public class ApplicationsApiTests : IClassFixture<ApplicationsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly ApplicationsApiFactory _factory;

    public ApplicationsApiTests(ApplicationsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Privacy_dto_matches_LobsyCvAccessRules_for_bm_rm_vm()
    {
        foreach (var userId in new[] { _factory.EnterpriseAId, _factory.RegionalA1UserId, _factory.BranchA1UserId })
        {
            using var client = Authed(userId);
            var list = await client.GetFromJsonAsync<List<JsonElement>>("api/applications", JsonOpts);
            Assert.NotNull(list);

            var pending = list.Single(a => a.GetProperty("id").GetGuid() == _factory.PendingAppId);
            Assert.True(
                !pending.TryGetProperty("candidateName", out var pendingName)
                || pendingName.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);
            Assert.True(
                !pending.TryGetProperty("candidateEmail", out var pendingEmail)
                || pendingEmail.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);
            Assert.False(pending.GetProperty("piiRevealed").GetBoolean());

            var accepted = list.Single(a => a.GetProperty("id").GetGuid() == _factory.AcceptedAppId);
            Assert.Equal("Priya Sanders", accepted.GetProperty("candidateName").GetString());
            Assert.True(
                !accepted.TryGetProperty("candidateEmail", out var acceptedEmail)
                || acceptedEmail.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);
            Assert.True(accepted.GetProperty("piiRevealed").GetBoolean());
            Assert.True(LobsyCvAccessRules.IsPiiRevealed(ApplicationStatus.Accepted));
            Assert.False(LobsyCvAccessRules.IsDirectContactRevealed(ApplicationStatus.Accepted));

            var hired = list.Single(a => a.GetProperty("id").GetGuid() == _factory.HiredAppId);
            Assert.Equal("hired@wgapp.local", hired.GetProperty("candidateEmail").GetString());
            Assert.True(LobsyCvAccessRules.IsDirectContactRevealed(ApplicationStatus.Hired));
        }
    }

    [Fact]
    public async Task Filters_status_overdue_and_branchIds()
    {
        using var bm = Authed(_factory.EnterpriseAId);

        var pendingOnly = await bm.GetFromJsonAsync<List<JsonElement>>(
            "api/applications?status=Pending", JsonOpts);
        Assert.NotNull(pendingOnly);
        Assert.All(pendingOnly, a => Assert.Equal("Pending", a.GetProperty("status").GetString()));

        var overdue = await bm.GetFromJsonAsync<List<JsonElement>>(
            "api/applications?overdueHours=48", JsonOpts);
        Assert.NotNull(overdue);
        Assert.Contains(overdue, a => a.GetProperty("id").GetGuid() == _factory.OverdueAppId);
        Assert.DoesNotContain(overdue, a => a.GetProperty("id").GetGuid() == _factory.PendingAppId);

        var branch = await bm.GetFromJsonAsync<List<JsonElement>>(
            $"api/applications?branchIds={_factory.BranchA1Id}", JsonOpts);
        Assert.NotNull(branch);
        Assert.All(branch, a => Assert.Equal("A1", a.GetProperty("companyName").GetString()));

        var foreign = await bm.GetFromJsonAsync<List<JsonElement>>(
            $"api/applications?branchIds={_factory.BranchBId}", JsonOpts);
        Assert.NotNull(foreign);
        Assert.Empty(foreign);
    }

    [Theory]
    [InlineData("react")]
    [InlineData("contact")]
    [InlineData("fulfill")]
    public async Task Rm_gets_403_on_mutating_application_endpoints(string kind)
    {
        using var rm = Authed(_factory.RegionalA1UserId);
        using var response = kind switch
        {
            "react" => await rm.PostAsJsonAsync(
                $"api/applications/{_factory.PendingAppId}/react",
                new { status = "Accepted" }),
            "contact" => await rm.PostAsync(
                $"api/applications/{_factory.AcceptedAppId}/contact", null),
            _ => await rm.PostAsJsonAsync(
                $"api/applications/vacancies/{_factory.VacancyA1Id}/fulfill/{_factory.AcceptedAppId}",
                new { rejectOtherApplications = false, closeVacancy = false })
        };
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Foreign_vm_gets_403_on_react_contact_fulfill()
    {
        using var vm = Authed(_factory.BranchA2UserId);
        using var react = await vm.PostAsJsonAsync(
            $"api/applications/{_factory.PendingAppId}/react",
            new { status = "Accepted" });
        Assert.True(react.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);

        using var contact = await vm.PostAsync(
            $"api/applications/{_factory.AcceptedAppId}/contact", null);
        Assert.True(contact.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);

        using var fulfill = await vm.PostAsJsonAsync(
            $"api/applications/vacancies/{_factory.VacancyA1Id}/fulfill/{_factory.AcceptedAppId}",
            new { rejectOtherApplications = false, closeVacancy = false });
        Assert.True(fulfill.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Bm_and_own_vm_can_react()
    {
        using var bm = Authed(_factory.EnterpriseAId);
        using var reactBm = await bm.PostAsJsonAsync(
            $"api/applications/{_factory.PendingReactBmId}/react",
            new { status = "Rejected" });
        Assert.True(reactBm.IsSuccessStatusCode, reactBm.StatusCode.ToString());

        using var vm = Authed(_factory.BranchA1UserId);
        using var reactVm = await vm.PostAsJsonAsync(
            $"api/applications/{_factory.PendingReactVmId}/react",
            new { status = "Rejected" });
        Assert.True(reactVm.IsSuccessStatusCode, reactVm.StatusCode.ToString());
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}

public sealed class ApplicationsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "wg-apps-" + Guid.NewGuid().ToString("N");
    private bool _seeded;

    public Guid OrgAId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    public Guid BranchA1Id { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000011");
    public Guid BranchA2Id { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000012");
    public Guid BranchBId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000099");
    public Guid EnterpriseAId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000101");
    public Guid BranchA1UserId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000111");
    public Guid BranchA2UserId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000112");
    public Guid RegionalA1UserId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000121");
    public Guid VacancyA1Id { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000201");
    public Guid VacancyA2Id { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000202");
    public Guid PendingAppId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000301");
    public Guid OverdueAppId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000302");
    public Guid AcceptedAppId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000303");
    public Guid HiredAppId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000304");
    public Guid PendingReactBmId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000305");
    public Guid PendingReactVmId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000306");

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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Companies.AddRange(
            new Company { Id = OrgAId, Name = "OrgA", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4.2) },
            new Company
            {
                Id = BranchA1Id,
                Name = "A1",
                KvkNumber = "2",
                Address = "a1",
                ParentCompanyId = OrgAId,
                Location = new GeoPoint(52.01, 4.21)
            },
            new Company
            {
                Id = BranchA2Id,
                Name = "A2",
                KvkNumber = "3",
                Address = "a2",
                ParentCompanyId = OrgAId,
                Location = new GeoPoint(52.02, 4.22)
            },
            new Company { Id = BranchBId, Name = "B", KvkNumber = "4", Address = "b", Location = new GeoPoint(51.9, 4.3) });

        db.Users.AddRange(
            new User
            {
                Id = EnterpriseAId,
                Email = "em@wgapp.local",
                FullName = "EM",
                Role = UserRole.EnterpriseManager,
                IsActive = true,
                CompanyId = OrgAId
            },
            new User
            {
                Id = BranchA1UserId,
                Email = "bm1@wgapp.local",
                FullName = "VM1",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = BranchA1Id
            },
            new User
            {
                Id = BranchA2UserId,
                Email = "bm2@wgapp.local",
                FullName = "VM2",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = BranchA2Id
            },
            new User
            {
                Id = RegionalA1UserId,
                Email = "rm@wgapp.local",
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
            new UserCompany { UserId = BranchA2UserId, CompanyId = BranchA2Id },
            new UserCompany { UserId = RegionalA1UserId, CompanyId = BranchA1Id });

        Vacancy MakeVac(Guid id, Guid companyId, string title) => new()
        {
            Id = id,
            Title = title,
            Description = "Omschrijving lang genoeg.",
            HourlyWage = 14,
            StartDate = today,
            EndDate = today.AddDays(30),
            Status = VacancyStatus.Active,
            CompanyId = companyId,
            Location = new GeoPoint(52.01, 4.21),
            ContentModerationPassed = true,
            WorkTypeLabels = "Tuinbouw"
        };

        db.Vacancies.AddRange(
            MakeVac(VacancyA1Id, BranchA1Id, "Kas A1"),
            MakeVac(VacancyA2Id, BranchA2Id, "Kas A2"));

        Application MakeApp(
            Guid id,
            Guid vacancyId,
            ApplicationStatus status,
            DateTime created,
            string name,
            string email) => new()
            {
                Id = id,
                VacancyId = vacancyId,
                CandidateName = name,
                CandidateEmail = email,
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 12,
                Status = status,
                EmailVerifiedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = created,
                RespondedAt = status == ApplicationStatus.Pending ? null : DateTime.UtcNow.AddDays(-1),
                SnapshotPhoneNumber = "0611111111",
                MatchPercent = 90
            };

        var now = DateTime.UtcNow;
        db.Applications.AddRange(
            MakeApp(PendingAppId, VacancyA1Id, ApplicationStatus.Pending, now.AddHours(-10), "Hidden", "pending@wgapp.local"),
            MakeApp(OverdueAppId, VacancyA1Id, ApplicationStatus.Pending, now.AddHours(-60), "Late", "late@wgapp.local"),
            MakeApp(AcceptedAppId, VacancyA1Id, ApplicationStatus.Accepted, now.AddDays(-3), "Priya Sanders", "priya@wgapp.local"),
            MakeApp(HiredAppId, VacancyA1Id, ApplicationStatus.Hired, now.AddDays(-5), "Hired Person", "hired@wgapp.local"),
            MakeApp(PendingReactBmId, VacancyA1Id, ApplicationStatus.Pending, now.AddHours(-5), "BmReact", "bmreact@wgapp.local"),
            MakeApp(PendingReactVmId, VacancyA1Id, ApplicationStatus.Pending, now.AddHours(-5), "VmReact", "vmreact@wgapp.local"));

        db.SaveChanges();
        _seeded = true;
    }
}
