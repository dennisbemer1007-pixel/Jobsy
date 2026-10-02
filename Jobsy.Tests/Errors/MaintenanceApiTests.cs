using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Admin;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.Errors;

/// <summary>
/// errors 05 §05.2 and §05.9. While maintenance is on the API answers non-admin calls with 503
/// ProblemDetails, but health, the public status endpoint, the auth endpoints and admins pass.
/// </summary>
public class MaintenanceApiTests
{
    [Fact]
    public async Task Status_endpoint_reports_the_switch_without_the_internal_note()
    {
        await using var factory = new MaintenanceApiFactory();
        factory.SetMaintenance(true, DateTime.UtcNow.AddMinutes(20), "migratie 42");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("api/site/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("maintenance").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, doc.RootElement.GetProperty("expectedEndUtc").ValueKind);
        Assert.DoesNotContain("migratie 42", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Non_admin_api_call_gets_problem_details_with_retry_after()
    {
        await using var factory = new MaintenanceApiFactory();
        factory.SetMaintenance(true, null, null);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("api/vacancies");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(
            MaintenanceRules.DefaultRetryAfterSeconds,
            (int)response.Headers.RetryAfter!.Delta!.Value.TotalSeconds);

        using var doc = JsonDocument.Parse(body);
        Assert.Equal(MaintenanceRules.Code, doc.RootElement.GetProperty("code").GetString());
        Assert.Equal(503, doc.RootElement.GetProperty("status").GetInt32());
        Assert.True(Jobsy.Web.Diagnostics.SupportCode.IsValid(
            doc.RootElement.GetProperty("supportCode").GetString()));
    }

    [Theory]
    [InlineData("health")]
    [InlineData("api/site/status")]
    public async Task Always_allowed_paths_stay_open(string path)
    {
        await using var factory = new MaintenanceApiFactory();
        factory.SetMaintenance(true, null, null);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Auth_login_stays_open_so_an_admin_can_sign_in()
    {
        await using var factory = new MaintenanceApiFactory();
        factory.SetMaintenance(true, null, null);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "api/auth/login",
            new { email = "beheer@onderhoud.jobsy.local", password = "wrong-on-purpose" });

        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_passes_through()
    {
        await using var factory = new MaintenanceApiFactory();
        factory.SetMaintenance(true, null, null);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(factory, factory.AdminId);

        var response = await client.GetAsync("api/admin/settings/maintenance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Put_requires_admin()
    {
        await using var factory = new MaintenanceApiFactory();
        using var anonymous = factory.CreateClient();

        var anonymousResponse = await anonymous.PutAsJsonAsync(
            "api/admin/settings/maintenance",
            new { enabled = true });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var candidate = JobsyTestAuth.CreateAuthenticatedClient(factory, factory.CandidateId);
        var candidateResponse = await candidate.PutAsJsonAsync(
            "api/admin/settings/maintenance",
            new { enabled = true });
        Assert.Equal(HttpStatusCode.Forbidden, candidateResponse.StatusCode);
    }

    [Fact]
    public async Task Put_flips_the_switch_and_writes_an_audit_entry()
    {
        await using var factory = new MaintenanceApiFactory();
        using var client = JobsyTestAuth.CreateAuthenticatedClient(factory, factory.AdminId);
        var expectedEnd = new DateTime(2026, 10, 1, 14, 30, 0, DateTimeKind.Utc);

        var on = await client.PutAsJsonAsync(
            "api/admin/settings/maintenance",
            new { enabled = true, expectedEndUtc = expectedEnd, note = "migratie 42" });
        Assert.Equal(HttpStatusCode.OK, on.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var row = await db.PlatformFeatureSettings.AsNoTracking().FirstAsync();
            Assert.True(row.MaintenanceEnabled);
            Assert.Equal(expectedEnd, row.MaintenanceExpectedEndUtc);
            Assert.Equal("migratie 42", row.MaintenanceNote);

            var audit = await db.AdminAuditEvents.AsNoTracking()
                .Where(e => e.Action == AdminAuditKeys.MaintenanceOn)
                .ToListAsync();
            Assert.Single(audit);
        }

        var off = await client.PutAsJsonAsync(
            "api/admin/settings/maintenance",
            new { enabled = false });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var row = await db.PlatformFeatureSettings.AsNoTracking().FirstAsync();
            Assert.False(row.MaintenanceEnabled);
            Assert.Null(row.MaintenanceExpectedEndUtc);

            Assert.Single(await db.AdminAuditEvents.AsNoTracking()
                .Where(e => e.Action == AdminAuditKeys.MaintenanceOff)
                .ToListAsync());
        }
    }

    [Fact]
    public void The_allow_list_is_prefix_exact()
    {
        Assert.True(Jobsy.Api.Security.MaintenanceApiMiddleware.IsAlwaysAllowed("/health"));
        Assert.True(Jobsy.Api.Security.MaintenanceApiMiddleware.IsAlwaysAllowed("/api/auth/login"));
        Assert.True(Jobsy.Api.Security.MaintenanceApiMiddleware.IsAlwaysAllowed("/api/site/status"));
        Assert.False(Jobsy.Api.Security.MaintenanceApiMiddleware.IsAlwaysAllowed("/api/site/about"));
        Assert.False(Jobsy.Api.Security.MaintenanceApiMiddleware.IsAlwaysAllowed("/healthzzz"));
        Assert.False(Jobsy.Api.Security.MaintenanceApiMiddleware.IsAlwaysAllowed("/api/vacancies"));
    }
}

/// <summary>In-memory API host with one admin and one candidate, for the maintenance tests.</summary>
public sealed class MaintenanceApiFactory : WebApplicationFactory<Program>
{
    public Guid AdminId { get; } = Guid.Parse("c5000000-0000-0000-0000-000000000001");

    public Guid CandidateId { get; } = Guid.Parse("c5000000-0000-0000-0000-000000000002");

    private readonly string _dbName = "Maintenance-" + Guid.NewGuid();
    private bool _seeded;

    /// <summary>Writes the singleton row directly, bypassing the admin endpoint.</summary>
    public void SetMaintenance(bool enabled, DateTime? expectedEndUtc, string? note)
    {
        EnsureSeeded();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = db.PlatformFeatureSettings.FirstOrDefault();
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = Guid.NewGuid() };
            db.PlatformFeatureSettings.Add(row);
        }

        row.MaintenanceEnabled = enabled;
        row.MaintenanceExpectedEndUtc = expectedEndUtc;
        row.MaintenanceNote = note;
        db.SaveChanges();
    }

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

    public void EnsureSeeded()
    {
        if (_seeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User
                {
                    Id = AdminId,
                    Email = "beheer@onderhoud.jobsy.local",
                    FullName = "Ada Admin",
                    Role = UserRole.Admin,
                    IsActive = true
                },
                new User
                {
                    Id = CandidateId,
                    Email = "kim@onderhoud.jobsy.local",
                    FullName = "Kim Kandidaat",
                    Role = UserRole.Candidate,
                    IsActive = true
                });
            db.SaveChanges();
        }

        _seeded = true;
    }
}
