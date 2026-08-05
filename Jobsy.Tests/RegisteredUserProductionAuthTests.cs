using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
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
/// Ensures Production DevelopmentAuth accepts real registrant emails when a local
/// session token from local-login is present (regression for post-registration 401s).
/// </summary>
public class RegisteredUserProductionAuthTests : IClassFixture<RegisteredUserProductionAuthFactory>
{
    private readonly RegisteredUserProductionAuthFactory _factory;

    public RegisteredUserProductionAuthTests(RegisteredUserProductionAuthFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Local_login_session_token_authorizes_non_demo_email_in_Production()
    {
        var email = "nieuwe.baas@example.com";
        var password = "TestPass1!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var companyId = Guid.Parse("e1000000-0000-0000-0000-000000000001");
            var userId = Guid.Parse("e1000000-0000-0000-0000-000000000002");
            if (!await db.Companies.AnyAsync(c => c.Id == companyId))
            {
                db.Companies.Add(new Company
                {
                    Id = companyId,
                    Name = "Nieuwe Baas BV",
                    KvkNumber = "88881111",
                    KvkEstablishmentId = "88881111_0001",
                    Address = "Teststraat 1",
                    Location = new GeoPoint(52.1, 5.1),
                    Type = CompanyType.Employer
                });
            }

            if (!await db.Users.AnyAsync(u => u.Id == userId))
            {
                db.Users.Add(new User
                {
                    Id = userId,
                    Email = email,
                    FullName = "Nieuwe Baas",
                    Role = UserRole.EnterpriseManager,
                    CompanyId = companyId,
                    IsActive = true
                });
                db.UserCompanies.Add(new UserCompany { UserId = userId, CompanyId = companyId });
                db.LocalAuthCredentials.Add(new LocalAuthCredential
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Email = email,
                    PasswordHash = JobsyPasswordHasher.Hash(password)
                });
                await db.SaveChangesAsync();
            }
        }

        var anon = _factory.CreateClient();
        using var loginResponse = await anon.PostAsJsonAsync(
            "api/auth/local-login",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginDoc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var sessionToken = loginDoc.RootElement.GetProperty("sessionToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(sessionToken));

        // Without session token → rejected outside Development for non-demo emails.
        var blocked = _factory.CreateClient();
        blocked.DefaultRequestHeaders.Add("X-Jobsy-Email", email);
        blocked.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", RegisteredUserProductionAuthFactory.DevSecret);
        Assert.Equal(HttpStatusCode.Unauthorized, (await blocked.GetAsync("api/me/profile")).StatusCode);

        // With session token → authorized.
        var ok = _factory.CreateClient();
        ok.DefaultRequestHeaders.Add("X-Jobsy-Email", email);
        ok.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", RegisteredUserProductionAuthFactory.DevSecret);
        ok.DefaultRequestHeaders.Add("X-Jobsy-Local-Session", sessionToken!);
        var profile = await ok.GetAsync("api/me/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }
}

public sealed class RegisteredUserProductionAuthFactory : WebApplicationFactory<Program>
{
    public const string DevSecret = "registered-user-prod-auth-secret";

    private readonly string _dbName = "RegisteredUserProdAuth-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production: @jobsy.local gate + local session requirement kick in.
        builder.UseEnvironment("Production");
        builder.UseSetting("JobsyAuth:AllowDevelopmentAuth", "true");
        builder.UseSetting("JobsyAuth:DevelopmentAuthSecret", DevSecret);
        builder.UseSetting("VerificationCodes:Pepper", "test-pepper-registered-user-prod-auth-32chars");
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

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyContentModerationResult(true, null));
    }
}
