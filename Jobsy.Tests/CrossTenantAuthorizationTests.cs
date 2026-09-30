using System.Net;
using System.Net.Http.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
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

/// <summary>
/// Cross-tenant / cross-branch authorization checks.
/// <para>
/// Covered (representative): vacancies PUT, applications list filtering, salary-tables GET,
/// company culture GET, talent unlock mutate, sales/ambassadeur dashboard by userId,
/// candidate CV download isolation.
/// </para>
/// <para>
/// Not covered here (rely on controller-level roles + prompt 03/matrix tests): admin ATS,
/// CSV import, exclusivity settings, region hosts, masterdata, external API-key clients,
/// intermediary multi-client board, pushbom previews, full applications react matrix.
/// </para>
/// </summary>
public class CrossTenantAuthorizationTests : IClassFixture<CrossTenantWebAppFactory>
{
    private readonly CrossTenantWebAppFactory _factory;

    public CrossTenantAuthorizationTests(CrossTenantWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task BranchManager_A1_cannot_mutate_or_manage_A2_or_B()
    {
        using var client = Authed(_factory.BranchA1UserId);

        // PUT vacancy of A2 / B → 404 (not in managed set) or 403
        AssertForbiddenOrNotFound(await client.PutAsJsonAsync(
            $"api/vacancies/{_factory.VacancyA2Id}",
            MinimalVacancyBody(_factory.BranchA2Id)));
        AssertForbiddenOrNotFound(await client.PutAsJsonAsync(
            $"api/vacancies/{_factory.VacancyBId}",
            MinimalVacancyBody(_factory.BranchBId)));

        // Culture of other companies
        AssertForbiddenOrNotFound(await client.GetAsync($"api/company/culture?companyId={_factory.BranchA2Id}"));
        AssertForbiddenOrNotFound(await client.GetAsync($"api/company/culture?companyId={_factory.BranchBId}"));

        // Salary table of B
        AssertForbiddenOrNotFound(await client.GetAsync($"api/salary-tables/{_factory.SalaryTableBId}"));

        // Applications list must not include A2/B vacancy applications
        using var apps = await client.GetAsync("api/applications");
        Assert.Equal(HttpStatusCode.OK, apps.StatusCode);
        var body = await apps.Content.ReadAsStringAsync();
        Assert.DoesNotContain(_factory.VacancyA2Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(_factory.VacancyBId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnterpriseManager_A_can_access_A1_and_A2_but_not_B()
    {
        using var client = Authed(_factory.EnterpriseAUserId);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/company/culture?companyId={_factory.BranchA1Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/company/culture?companyId={_factory.BranchA2Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/salary-tables/{_factory.SalaryTableAId}")).StatusCode);

        AssertForbiddenOrNotFound(await client.GetAsync($"api/company/culture?companyId={_factory.BranchBId}"));
        AssertForbiddenOrNotFound(await client.GetAsync($"api/salary-tables/{_factory.SalaryTableBId}"));
    }

    [Fact]
    public async Task RegionalManager_A1_reads_A1_not_A2_or_B_and_cannot_mutate()
    {
        using var client = Authed(_factory.RegionalA1UserId);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/company/culture?companyId={_factory.BranchA1Id}")).StatusCode);
        AssertForbiddenOrNotFound(await client.GetAsync($"api/company/culture?companyId={_factory.BranchA2Id}"));
        AssertForbiddenOrNotFound(await client.GetAsync($"api/company/culture?companyId={_factory.BranchBId}"));

        using var unlock = await client.PostAsJsonAsync(
            "api/employer/talent/unlock",
            new { candidateUserId = _factory.CandidateXId, message = "nope" });
        Assert.Equal(HttpStatusCode.Forbidden, unlock.StatusCode);

        using var culturePut = await client.PutAsJsonAsync(
            "api/company/culture",
            new { answers = new Dictionary<string, int>(), complete = false });
        Assert.Equal(HttpStatusCode.Forbidden, culturePut.StatusCode);
    }

    [Fact]
    public async Task Candidate_X_cannot_download_candidate_Y_cv()
    {
        using var client = Authed(_factory.CandidateXId);
        using var response = await client.GetAsync($"api/applications/{_factory.ApplicationYId}/lobsy-cv.pdf");
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.BadRequest,
            $"Expected forbid/notfound/badrequest, got {response.StatusCode}");
    }

    [Fact]
    public async Task SalesManager_S1_cannot_read_S2_dashboard()
    {
        using var client = Authed(_factory.SalesS1UserId);
        using var response = await client.GetAsync($"api/sales-managers/{_factory.SalesS2UserId}/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ambassadeur_A1_cannot_read_A2_dashboard()
    {
        using var client = Authed(_factory.AmbassadeurA1UserId);
        using var response = await client.GetAsync($"api/ambassadeurs/{_factory.AmbassadeurA2UserId}/dashboard");
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"Expected 403/404, got {response.StatusCode}");
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }

    private static void AssertForbiddenOrNotFound(HttpResponseMessage response)
    {
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"Expected 403/404, got {response.StatusCode}");
    }

    private static CreateVacancyRequest MinimalVacancyBody(Guid companyId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new CreateVacancyRequest(
            CompanyId: companyId,
            Title: "x",
            Description: "y",
            HourlyWage: 14.5m,
            StartDate: today,
            EndDate: today.AddMonths(1),
            RequiredTransport: TransportMode.Bike,
            WorkTypes: ["Winkel"],
            MinHoursPerWeek: 8,
            MaxHoursPerWeek: 24);
    }
}

public sealed class CrossTenantWebAppFactory : WebApplicationFactory<Program>
{
    public Guid OrgAId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000001");
    public Guid BranchA1Id { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000011");
    public Guid BranchA2Id { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000012");
    public Guid BranchBId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000021");

    public Guid VacancyA1Id { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000031");
    public Guid VacancyA2Id { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000032");
    public Guid VacancyBId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000033");

    public Guid SalaryTableAId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000041");
    public Guid SalaryTableBId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000042");

    public Guid BranchA1UserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000051");
    public Guid EnterpriseAUserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000052");
    public Guid RegionalA1UserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000053");
    public Guid CandidateXId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000061");
    public Guid CandidateYId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000062");
    public Guid ApplicationYId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000071");
    public Guid SalesS1UserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000081");
    public Guid SalesS2UserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000082");
    public Guid AmbassadeurA1UserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000091");
    public Guid AmbassadeurA2UserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000092");

    private readonly string _dbName = "CrossTenant-" + Guid.NewGuid();
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

            services.AddDbContext<JobsyDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

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

        var categories = new VacancyCategoryService(db);
        categories.EnsureDefaultsAsync().GetAwaiter().GetResult();

        db.Companies.AddRange(
            new Company
            {
                Id = OrgAId,
                Name = "Org A",
                KvkNumber = "11111111",
                Address = "A",
                Location = new GeoPoint(52.0, 4.2),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        },
            new Company
            {
                Id = BranchA1Id,
                Name = "Branch A1",
                KvkNumber = "11111112",
                Address = "A1",
                ParentCompanyId = OrgAId,
                Location = new GeoPoint(52.01, 4.21),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        },
            new Company
            {
                Id = BranchA2Id,
                Name = "Branch A2",
                KvkNumber = "11111113",
                Address = "A2",
                ParentCompanyId = OrgAId,
                Location = new GeoPoint(52.02, 4.22),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        },
            new Company
            {
                Id = BranchBId,
                Name = "Branch B",
                KvkNumber = "22222222",
                Address = "B",
                Location = new GeoPoint(51.9, 4.3),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });

        db.CompanySalaryTables.AddRange(
            new CompanySalaryTable
            {
                Id = SalaryTableAId,
                CompanyId = OrgAId,
                Name = "WML A",
                IsActive = true,
                IsSystemWml = true
            },
            new CompanySalaryTable
            {
                Id = SalaryTableBId,
                CompanyId = BranchBId,
                Name = "WML B",
                IsActive = true,
                IsSystemWml = true
            });

        db.Users.AddRange(
            new User
            {
                Id = BranchA1UserId,
                Email = "branch-a1@jobsy.local",
                FullName = "Branch A1",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = BranchA1Id
            },
            new User
            {
                Id = EnterpriseAUserId,
                Email = "enterprise-a@jobsy.local",
                FullName = "Enterprise A",
                Role = UserRole.EnterpriseManager,
                IsActive = true,
                CompanyId = OrgAId
            },
            new User
            {
                Id = RegionalA1UserId,
                Email = "regio-a1@jobsy.local",
                FullName = "Regional A1",
                Role = UserRole.RegionalManager,
                IsActive = true,
                CompanyId = BranchA1Id
            },
            new User
            {
                Id = CandidateXId,
                Email = "cand-x@jobsy.local",
                FullName = "Candidate X",
                Role = UserRole.Candidate,
                IsActive = true,
                DateOfBirth = new DateOnly(1995, 1, 1),
                OpenForWork = true
            },
            new User
            {
                Id = CandidateYId,
                Email = "cand-y@jobsy.local",
                FullName = "Candidate Y",
                Role = UserRole.Candidate,
                IsActive = true,
                DateOfBirth = new DateOnly(1996, 1, 1),
                OpenForWork = true
            },
            new User
            {
                Id = SalesS1UserId,
                Email = "sales-s1@jobsy.local",
                FullName = "Sales S1",
                Role = UserRole.SalesManager,
                IsActive = true
            },
            new User
            {
                Id = SalesS2UserId,
                Email = "sales-s2@jobsy.local",
                FullName = "Sales S2",
                Role = UserRole.SalesManager,
                IsActive = true
            },
            new User
            {
                Id = AmbassadeurA1UserId,
                Email = "amb-a1@jobsy.local",
                FullName = "Amb A1",
                Role = UserRole.Ambassadeur,
                IsActive = true
            },
            new User
            {
                Id = AmbassadeurA2UserId,
                Email = "amb-a2@jobsy.local",
                FullName = "Amb A2",
                Role = UserRole.Ambassadeur,
                IsActive = true
            });

        db.UserCompanies.AddRange(
            new UserCompany { UserId = BranchA1UserId, CompanyId = BranchA1Id },
            new UserCompany { UserId = EnterpriseAUserId, CompanyId = OrgAId },
            new UserCompany { UserId = RegionalA1UserId, CompanyId = BranchA1Id });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Vacancies.AddRange(
            Vac(VacancyA1Id, BranchA1Id, SalaryTableAId, "Vac A1", today),
            Vac(VacancyA2Id, BranchA2Id, SalaryTableAId, "Vac A2", today),
            Vac(VacancyBId, BranchBId, SalaryTableBId, "Vac B", today));

        db.Applications.Add(new Application
        {
            Id = ApplicationYId,
            VacancyId = VacancyA1Id,
            CandidateUserId = CandidateYId,
            CandidateName = "Candidate Y",
            CandidateEmail = "cand-y@jobsy.local",
            Status = ApplicationStatus.Pending,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            PreferredTransport = "Fiets"
        });

        db.SalesManagerProfiles.AddRange(
            new SalesManagerProfile
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-0000000000a1"),
                UserId = SalesS1UserId,
                CompanyName = "S1",
                TrackingCode = "SM-CTS01",
                AgreementSignedAt = DateTime.UtcNow.AddDays(-1),
                AgreementVersion = "v1",
                OnboardingCompletedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SalesManagerProfile
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-0000000000a2"),
                UserId = SalesS2UserId,
                CompanyName = "S2",
                TrackingCode = "SM-CTS02",
                AgreementSignedAt = DateTime.UtcNow.AddDays(-1),
                AgreementVersion = "v1",
                OnboardingCompletedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        db.AmbassadeurProfiles.AddRange(
            new AmbassadeurProfile
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-0000000000b1"),
                UserId = AmbassadeurA1UserId,
                CompanyName = "Amb1",
                TrackingCode = "AM-CTS01",
                AgreementSignedAt = DateTime.UtcNow.AddDays(-1),
                AgreementVersion = "v1",
                OnboardingCompletedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AmbassadeurProfile
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-0000000000b2"),
                UserId = AmbassadeurA2UserId,
                CompanyName = "Amb2",
                TrackingCode = "AM-CTS02",
                AgreementSignedAt = DateTime.UtcNow.AddDays(-1),
                AgreementVersion = "v1",
                OnboardingCompletedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        db.SaveChanges();
        _seeded = true;
    }

    private static Vacancy Vac(Guid id, Guid companyId, Guid salaryTableId, string title, DateOnly today)
        => new()
        {
            Id = id,
            Title = title,
            Description = "cross-tenant",
            HourlyWage = 14.5m,
            StartDate = today.AddDays(-1),
            EndDate = today.AddMonths(1),
            Status = VacancyStatus.Active,
            CompanyId = companyId,
            Location = new GeoPoint(52.0, 4.2),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            SalaryTableId = salaryTableId,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
            MinHoursPerWeek = 8,
            MaxHoursPerWeek = 24,
            CategoryId = VacancyCategoryDefaults.RegulierId,
            RequireEmailVerification = true
        };

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyContentModerationResult(true, null));
    }
}
