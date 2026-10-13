using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class PassportPartnersApiTests
{
    [Fact]
    public async Task Public_code_resolve_is_404_when_the_flag_is_off_and_rate_limited()
    {
        await using var factory = new PassportPartnersApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(RateLimitPartitioning.ClientIpHeader, "203.0.113.55");
        client.DefaultRequestHeaders.Add(RateLimitPartitioning.InternalSecretHeader, "test-internal-client-ip-secret");

        var first = await client.GetAsync("api/passport-partners/codes/K7QM2P");
        var second = await client.GetAsync("api/passport-partners/codes/K7QM2P");
        var third = await client.GetAsync("api/passport-partners/codes/K7QM2P");

        Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
        Assert.Contains("feature_disabled", await first.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Candidate_cannot_consent_on_someone_elses_link()
    {
        await using var factory = new PassportPartnersApiFactory();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.NewGuid(),
                PassportPartnersEnabled = true,
                PublicWebBaseUrl = "https://lobsy.test"
            });
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Bureau",
                KvkNumber = "12345678",
                Address = "Straat 1",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Intermediary
            };
            db.Companies.Add(company);
            var partner = new PassportPartner
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                IsActive = true,
                DisplayName = "Bureau",
                MaxBranches = 1,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.PassportPartners.Add(partner);
            db.Users.Add(User(ownerId, "owner@example.com"));
            db.Users.Add(User(otherId, "other@example.com"));
            db.PassportPartnerCandidateLinks.Add(new PassportPartnerCandidateLink
            {
                Id = linkId,
                CandidateUserId = ownerId,
                PassportPartnerId = partner.Id,
                Source = PassportPartnerLinkSource.Code,
                StartedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        (factory.Services.GetRequiredService<Jobsy.Core.Features.IFeatureFlags>() as FeatureFlags)?.Invalidate();
        var client = factory.CreateClient();
        JobsyTestAuth.Authorize(client, otherId);
        var response = await client.PostAsJsonAsync(
            $"api/me/passport-partners/{linkId}/consent",
            new { consentVersion = PrivacyConstants.PartnerShareConsentVersion, contactConsent = false, confirmAdult = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("not_found", doc.RootElement.GetProperty("error").GetString());
        using var scopeAfter = factory.Services.CreateScope();
        var after = scopeAfter.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.Null((await after.PassportPartnerCandidateLinks.SingleAsync(l => l.Id == linkId)).ConsentGivenAtUtc);
    }

    private static User User(Guid id, string email) => new()
    {
        Id = id,
        Email = email,
        FullName = "Kandidaat",
        Role = UserRole.Candidate,
        IsActive = true,
        DateOfBirth = new DateOnly(2000, 1, 1)
    };
}

public sealed class PassportPartnersApiFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    private readonly string _dbName = "PassportPartners-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("RateLimiting:PublicReadPermitLimit", "2");
        builder.UseSetting("PassportPartners:CodeHmacKey", "test-passport-partner-hmac");
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

            services.AddDbContext<JobsyDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}
