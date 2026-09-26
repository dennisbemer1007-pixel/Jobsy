using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class RateLimitPartitioningUnitTests
{
    private const string Secret = "unit-test-internal-client-ip-secret";

    [Fact]
    public void Prefers_user_id_over_client_ip_claim()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString("D")),
            new Claim(JobsyAccessToken.ClientIpClaim, "203.0.113.10")
        ], "JobsyJwt"));
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        var key = RateLimitPartitioning.ResolvePartitionKey(http, Secret);
        Assert.Equal("uid:" + userId.ToString("D"), key);
    }

    [Fact]
    public void Falls_back_to_user_id_when_trusted_ip_missing()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", userId.ToString("D"))],
            "JobsyJwt"));
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        var key = RateLimitPartitioning.ResolvePartitionKey(http, Secret);
        Assert.Equal("uid:" + userId.ToString("D"), key);
    }

    [Fact]
    public void Authenticated_user_ignores_forged_visitor_ip_header()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", userId.ToString("D"))],
            "JobsyJwt"));
        http.Request.Headers[RateLimitPartitioning.ClientIpHeader] = "198.51.100.7";
        http.Request.Headers[RateLimitPartitioning.InternalSecretHeader] = Secret;
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        var key = RateLimitPartitioning.ResolvePartitionKey(http, Secret);
        Assert.Equal("uid:" + userId.ToString("D"), key);
    }

    [Fact]
    public void Anonymous_uses_trusted_client_ip_header_with_secret()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers[RateLimitPartitioning.ClientIpHeader] = "198.51.100.7";
        http.Request.Headers[RateLimitPartitioning.InternalSecretHeader] = Secret;
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        var key = RateLimitPartitioning.ResolvePartitionKey(http, Secret);
        Assert.Equal("cip:198.51.100.7", key);
    }

    [Fact]
    public void Rejects_forged_client_ip_without_matching_secret()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers[RateLimitPartitioning.ClientIpHeader] = "198.51.100.7";
        http.Request.Headers[RateLimitPartitioning.InternalSecretHeader] = "wrong-secret";
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        var key = RateLimitPartitioning.ResolvePartitionKey(http, Secret);
        Assert.Equal("ip:10.0.0.1", key);
    }
}

[Collection("SequentialApi")]
public class RateLimitUserIsolationTests : IClassFixture<RateLimitIsolationFactory>
{
    private readonly RateLimitIsolationFactory _factory;

    public RateLimitUserIsolationTests(RateLimitIsolationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Two_users_each_making_100_requests_per_minute_do_not_block_each_other()
    {
        using var userA = CreateClient(_factory.UserAId);
        using var userB = CreateClient(_factory.UserBId);

        // If partitions shared the Web→API hop IP, user B would be rejected
        // immediately after user A exhausted the 100-permit window.
        for (var i = 0; i < 100; i++)
        {
            using var a = await userA.GetAsync("/api/vacancies");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, a.StatusCode);
        }

        for (var i = 0; i < 100; i++)
        {
            using var b = await userB.GetAsync("/api/vacancies");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, b.StatusCode);
        }

        using var over = await userA.GetAsync("/api/vacancies");
        Assert.Equal(HttpStatusCode.TooManyRequests, over.StatusCode);
        Assert.True(over.Headers.TryGetValues("Retry-After", out var values));
        Assert.NotEmpty(values);
    }

    [Fact]
    public async Task Twenty_map_visitors_from_different_ips_do_not_get_429()
    {
        const string secret = "test-internal-client-ip-secret";
        // Each anonymous visitor loads pins + a few cards (map session).
        // Shared hop-IP bucket would exhaust after 100 total; per-visitor
        // partitions keep every visitor under the 100/min public-read limit.
        for (var visitor = 0; visitor < 20; visitor++)
        {
            using var client = _factory.CreateClient();
            var visitorIp = $"198.51.100.{visitor + 1}";
            client.DefaultRequestHeaders.Remove(RateLimitPartitioning.ClientIpHeader);
            client.DefaultRequestHeaders.Remove(RateLimitPartitioning.InternalSecretHeader);
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                RateLimitPartitioning.ClientIpHeader,
                visitorIp);
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                RateLimitPartitioning.InternalSecretHeader,
                secret);

            for (var i = 0; i < 8; i++)
            {
                using var response = await client.GetAsync("/api/vacancies/pins");
                Assert.True(
                    response.StatusCode != HttpStatusCode.TooManyRequests,
                    $"Visitor {visitorIp} request {i + 1} got 429");
            }
        }
    }

    private HttpClient CreateClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JobsyTestAuth.Mint(userId));
        return client;
    }
}

public sealed class RateLimitIsolationFactory : WebApplicationFactory<Program>
{
    public Guid UserAId { get; } = Guid.Parse("f1000000-0000-0000-0000-0000000000a1");
    public Guid UserBId { get; } = Guid.Parse("f1000000-0000-0000-0000-0000000000b2");

    private readonly string _dbName = "RateLimitIsolation-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("RateLimiting:PublicReadPermitLimit", "100");
        builder.UseSetting(
            RateLimitPartitioning.ConfigKey,
            "test-internal-client-ip-secret");
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
        if (!db.Users.Any(u => u.Id == UserAId))
        {
            db.Users.Add(new User
            {
                Id = UserAId,
                Email = "rate-a@jobsy.local",
                FullName = "Rate A",
                Role = UserRole.Candidate,
                IsActive = true
            });
            db.Users.Add(new User
            {
                Id = UserBId,
                Email = "rate-b@jobsy.local",
                FullName = "Rate B",
                Role = UserRole.Candidate,
                IsActive = true
            });
            db.SaveChanges();
        }

        _seeded = true;
    }
}
