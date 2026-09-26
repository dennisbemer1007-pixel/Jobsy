using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api;
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

namespace Jobsy.Tests;

/// <summary>
/// Ensures production rejects forged identity headers and accepts signed JobsyJwt tokens
/// only when the request has passed the Cloudflare origin check.
/// </summary>
public class RegisteredUserProductionAuthTests : IClassFixture<RegisteredUserProductionAuthFactory>
{
    private readonly RegisteredUserProductionAuthFactory _factory;

    public RegisteredUserProductionAuthTests(RegisteredUserProductionAuthFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Forged_identity_header_is_rejected_but_jwt_with_origin_header_authorizes_in_Production()
    {
        var email = "nieuwe.baas@example.com";
        var userId = Guid.Parse("e1000000-0000-0000-0000-000000000002");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var companyId = Guid.Parse("e1000000-0000-0000-0000-000000000001");
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
                await db.SaveChangesAsync();
            }
        }

        var forged = _factory.CreateClient();
        forged.DefaultRequestHeaders.Add("X-Jobsy-Email", email);
        forged.DefaultRequestHeaders.Add("X-Jobsy-Origin-Secret", JobsyTestAuth.OriginSecret);
        Assert.Equal(HttpStatusCode.Unauthorized, (await forged.GetAsync("api/me/profile")).StatusCode);

        var missingOrigin = JobsyTestAuth.CreateAuthenticatedClient(_factory, userId);
        Assert.Equal(HttpStatusCode.Forbidden, (await missingOrigin.GetAsync("api/me/profile")).StatusCode);

        var authorized = JobsyTestAuth.CreateAuthenticatedClient(
            _factory,
            userId,
            includeOriginHeader: true);
        Assert.Equal(HttpStatusCode.OK, (await authorized.GetAsync("api/me/profile")).StatusCode);
    }
}

public sealed class RegisteredUserProductionAuthFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RegisteredUserProdAuth-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        JobsyTestAuth.ApplyProductionJwtSettings(builder);
        JobsyTestAuth.ApplyProductionOriginSettings(builder);
        builder.UseSetting("JobsyAuth:AllowEphemeralDataProtection", "true");
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
