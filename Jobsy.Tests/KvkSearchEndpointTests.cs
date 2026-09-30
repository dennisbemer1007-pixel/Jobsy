using System.Net;
using System.Text.Json;
using Jobsy.Api;
using Jobsy.Core.Authorization;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class KvkSearchEndpointTests : IClassFixture<KvkSearchEndpointFactory>
{
    private readonly KvkSearchEndpointFactory _factory;

    public KvkSearchEndpointTests(KvkSearchEndpointFactory factory) => _factory = factory;

    [Fact]
    public async Task Search_short_query_returns_400()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/kvk/search?q=gr");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_stub_returns_grouped_hits_without_owner_pii()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/kvk/search?q=groen%20en%20zorg&plaats=Utrecht");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Ok", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("hits").GetArrayLength() >= 1);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ContactName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", json);

        var first = root.GetProperty("hits").EnumerateArray().First();
        Assert.Equal("90123456", first.GetProperty("kvkNumber").GetString());
        Assert.Contains("Groen", first.GetProperty("name").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Profile_returns_website_and_postal_address()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/registration/kvk/90123456/profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Ok", root.GetProperty("status").GetString());
        Assert.Contains(
            root.GetProperty("websites").EnumerateArray().Select(e => e.GetString()),
            w => w == "groenenzorg.nl");
        var hq = root.GetProperty("establishments").EnumerateArray()
            .First(e => e.GetProperty("establishmentNumber").GetString() == "000045678901");
        Assert.NotEqual(JsonValueKind.Null, hq.GetProperty("postalAddress").ValueKind);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_rate_limit_returns_429_on_31st_call()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Remove(RateLimitPartitioning.ClientIpHeader);
        client.DefaultRequestHeaders.Remove(RateLimitPartitioning.InternalSecretHeader);
        client.DefaultRequestHeaders.Add(RateLimitPartitioning.ClientIpHeader, "203.0.113.44");
        client.DefaultRequestHeaders.Add(
            RateLimitPartitioning.InternalSecretHeader,
            "test-internal-client-ip-secret");

        HttpResponseMessage? last = null;
        for (var i = 0; i < 31; i++)
        {
            last = await client.GetAsync("api/kvk/search?q=groen");
            if (i < 30)
            {
                Assert.Equal(HttpStatusCode.OK, last.StatusCode);
            }
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }
}

public sealed class KvkSearchEndpointFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "KvkSearchEndpoint-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("RateLimiting:KvkSearchPermitLimit", "30");
        builder.UseSetting(RateLimitPartitioning.ConfigKey, "test-internal-client-ip-secret");
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
}
