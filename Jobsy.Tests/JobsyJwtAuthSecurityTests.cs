using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jobsy.Api;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class JobsyJwtAuthSecurityTests : IClassFixture<JobsyJwtAuthSecurityFactory>
{
    private readonly JobsyJwtAuthSecurityFactory _factory;

    public JobsyJwtAuthSecurityTests(JobsyJwtAuthSecurityFactory factory) => _factory = factory;

    [Fact]
    public async Task Forged_X_Jobsy_Email_header_is_ignored_and_rejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", "admin@jobsy.local");
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", "anything");
        client.DefaultRequestHeaders.Add("X-Jobsy-Name", "Hacker");
        client.DefaultRequestHeaders.Add(
            CloudflareOriginMiddleware.HeaderName,
            JobsyTestAuth.OriginSecret);

        var response = await client.GetAsync("api/me/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tampered_or_expired_token_is_rejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            CloudflareOriginMiddleware.HeaderName,
            JobsyTestAuth.OriginSecret);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JobsyTestAuth.Mint(_factory.UserId) + "x");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/me/profile")).StatusCode);

        var expired = JobsyAccessToken.Create(
            _factory.UserId,
            0,
            JobsyAccessToken.DevelopmentPrivateKeyPem,
            lifetime: TimeSpan.FromMinutes(-30));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/me/profile")).StatusCode);
    }

    [Fact]
    public async Task Revoked_SessionVersion_is_rejected()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == _factory.UserId);
            user.SessionVersion = 5;
            await db.SaveChangesAsync();
        }

        // Clear session version cache by waiting is not needed — new factory scope uses cache keyed per process.
        // Mint with old version 0.
        var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.UserId, sessionVersion: 0, includeOriginHeader: true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/me/profile")).StatusCode);

        var ok = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.UserId, sessionVersion: 5, includeOriginHeader: true);
        Assert.Equal(HttpStatusCode.OK, (await ok.GetAsync("api/me/profile")).StatusCode);
    }

    [Fact]
    public async Task For_login_with_only_old_provision_secret_returns_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            CloudflareOriginMiddleware.HeaderName,
            JobsyTestAuth.OriginSecret);
        client.DefaultRequestHeaders.Add("X-Jobsy-Provision-Secret", "old-shared-secret");

        var response = await client.PostAsJsonAsync(
            "api/auth/device-sessions/for-login",
            new { email = "admin@jobsy.local", rememberDevice = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_without_origin_header_returns_403_in_Production()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.UserId, sessionVersion: 5);
        var response = await client.GetAsync("api/me/profile");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_is_reachable_without_origin_header()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class JobsyJwtAuthSecurityFactory : WebApplicationFactory<Program>
{
    public Guid UserId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000099");

    private readonly string _dbName = "JwtAuthSec-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        JobsyTestAuth.ApplyProductionJwtSettings(builder);
        JobsyTestAuth.ApplyProductionOriginSettings(builder);
        builder.UseSetting("JobsyAuth:AllowEphemeralDataProtection", "true");
        builder.UseSetting("VerificationCodes:Pepper", "test-pepper-jwt-auth-security-32chars!!");
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("JobsyAuth:ExternalProvisionSecret", "provision-for-tests-only");
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

            services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.Database.EnsureCreated();
        if (!db.Users.Any(u => u.Id == UserId))
        {
            db.Users.Add(new User
            {
                Id = UserId,
                Email = "jwt.user@example.com",
                FullName = "JWT User",
                Role = UserRole.Candidate,
                IsActive = true,
                SessionVersion = 5,
                HomeLocation = new GeoPoint(52, 4)
            });
            db.SaveChanges();
        }

        return host;
    }
}
