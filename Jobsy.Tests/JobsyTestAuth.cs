using System.Net.Http.Headers;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>Mints JobsyJwt bearer tokens for API integration tests.</summary>
public static class JobsyTestAuth
{
    public const string OriginSecret = "test-cloudflare-origin-secret";

    private static readonly Lazy<(string PrivatePem, string PublicPem)> DedicatedProductionKeys =
        new(() => JobsyAccessToken.GenerateDevelopmentKeyPair());

    /// <summary>Unique ES256 pair for Production-environment integration tests (not the repo dev PEM).</summary>
    public static (string PrivatePem, string PublicPem) ProductionTestKeyPair => DedicatedProductionKeys.Value;

    public static void ApplyStandardAuthSettings(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Prefer Development fallback keys (UseSetting mangles multiline PEM).
        // Explicit issuer/audience keep mint + validate aligned.
        builder.UseSetting("JobsyAuth:Jwt:Issuer", JobsyAccessToken.DefaultIssuer);
        builder.UseSetting("JobsyAuth:Jwt:Audience", JobsyAccessToken.DefaultAudience);
        builder.UseSetting("Training:TrackingSecret", "test-training-tracking-secret");
        // Suites that assumed "no settings row ⇒ employers ON" keep that behaviour.
        // Production and acceptatie do not set this key. An explicit DB false still wins.
        builder.UseSetting("Jobsy:TestEmployersEnabled", "true");
    }

    public static void ApplyProductionJwtSettings(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        var (privatePem, publicPem) = DedicatedProductionKeys.Value;
        // Single-line PEM with escaped newlines survives IConfiguration UseSetting.
        builder.UseSetting(
            "JobsyAuth:Jwt:PublicKeyPem",
            publicPem.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal));
        builder.UseSetting(
            "JobsyAuth:Jwt:PrivateKeyPem",
            privatePem.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal));
        builder.UseSetting("JobsyAuth:Jwt:Issuer", JobsyAccessToken.DefaultIssuer);
        builder.UseSetting("JobsyAuth:Jwt:Audience", JobsyAccessToken.DefaultAudience);
        builder.UseSetting("Training:TrackingSecret", "test-training-tracking-secret");
        builder.UseSetting("Jobsy:TestEmployersEnabled", "true");
    }

    public static void ApplyProductionOriginSettings(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("CLOUDFLARE_ORIGIN_SECRET", OriginSecret);
        builder.UseSetting("Cloudflare:OriginSecret", OriginSecret);
    }

    public static string Mint(
        Guid userId,
        int sessionVersion = 0,
        string? clientIp = null,
        string? privatePem = null)
        => JobsyAccessToken.Create(
            userId,
            sessionVersion,
            privatePem ?? JobsyAccessToken.DevelopmentPrivateKeyPem,
            clientIp: clientIp);

    public static string MintForFactory<TEntry>(
        WebApplicationFactory<TEntry> factory,
        Guid userId,
        int sessionVersion = 0,
        string? clientIp = null)
        where TEntry : class
        => JobsyAccessToken.Create(
            userId,
            sessionVersion,
            ResolveMintPrivatePem(factory.Services),
            clientIp: clientIp);

    public static string ResolveMintPrivatePem(IServiceProvider services)
        => JobsyJwtPemBootstrap.ResolvePrivatePem(
            services.GetRequiredService<IConfiguration>(),
            services.GetRequiredService<IHostEnvironment>());

    public static void Authorize(
        HttpClient client,
        Guid userId,
        int sessionVersion = 0,
        string? privatePem = null)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Mint(userId, sessionVersion, privatePem: privatePem));
    }

    public static HttpClient CreateAuthenticatedClient<TEntry>(
        WebApplicationFactory<TEntry> factory,
        Guid userId,
        int? sessionVersion = null,
        bool includeOriginHeader = false)
        where TEntry : class
    {
        sessionVersion ??= ResolveSessionVersion(factory.Services, userId);
        var client = factory.CreateClient();
        var pem = ResolveMintPrivatePem(factory.Services);
        Authorize(client, userId, sessionVersion.Value, pem);
        if (includeOriginHeader)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Jobsy-Origin-Secret",
                OriginSecret);
        }

        return client;
    }

    private static int ResolveSessionVersion(IServiceProvider services, Guid userId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        return db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.SessionVersion)
            .FirstOrDefault();
    }

    public static async Task<HttpClient> CreateAuthenticatedClientByEmailAsync<TEntry>(
        WebApplicationFactory<TEntry> factory,
        string email,
        bool includeOriginHeader = false)
        where TEntry : class
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var user = await db.Users.AsNoTracking()
            .FirstAsync(u => u.Email.ToLower() == email.ToLower());
        return CreateAuthenticatedClient(factory, user.Id, user.SessionVersion, includeOriginHeader);
    }
}
