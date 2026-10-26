using System.Net;
using Jobsy.Core.Enums;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Tests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.TestAccounts;

[Collection("SequentialApi")]
public sealed class AcceptatieSeedResetExternalPurgeTests : IClassFixture<AcceptatieSeedResetExternalPurgeApiFactory>
{
    private readonly AcceptatieSeedResetExternalPurgeApiFactory _factory;

    public AcceptatieSeedResetExternalPurgeTests(AcceptatieSeedResetExternalPurgeApiFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Seed_reset_purge_removes_user_added_external_and_detail_is_not_found()
    {
        _ = _factory.CreateClient();
        var staleId = Guid.Parse("ab07d31f-0000-4000-8000-000000000099");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.CandidateExternalVacancies.Add(new Jobsy.Core.Entities.CandidateExternalVacancy
            {
                Id = staleId,
                CandidateUserId = _factory.KandidaatJobsyLocalId,
                SourceUrl = "https://example.com/uat-testvacature",
                SourceHost = "example.com",
                Title = "UAT testvacature",
                CompanyName = "BV",
                Status = CandidateExternalVacancyStatus.Applied,
                SavedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var seeder = new TestAccountsSeedService(db, config);
            Assert.True(await seeder.RepairCompleteCandidateAssessmentsAsync());
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            Assert.False(await db.CandidateExternalVacancies.AnyAsync(v => v.Id == staleId));
        }

        using var client = JobsyTestAuth.CreateAuthenticatedClient(
            _factory,
            _factory.KandidaatJobsyLocalId,
            _factory.KandidaatJobsyLocalSessionVersion);
        var response = await client.GetAsync($"api/candidate/external-vacancies/{staleId:D}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class AcceptatieSeedResetExternalPurgeApiFactory : ApiWebApplicationFactory
{
    public Guid KandidaatJobsyLocalId { get; private set; }
    public int KandidaatJobsyLocalSessionVersion { get; private set; }

    private readonly string _dbName = "SeedResetExtPurge-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        JobsyTestAuth.ApplyProductionJwtSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("VerificationCodes:Pepper", "test-verification-pepper-at-least-32-chars-long");
        builder.UseSetting("JobsyAuth:AllowEphemeralDataProtection", "true");
        builder.UseSetting("Lobsy:DeploymentEnvironment", "Acceptatie");
        builder.UseSetting("TestAccounts:Enabled", "true");
        builder.UseSetting("RENDER_SERVICE_NAME", "lobsy-acc-api");
        builder.UseSetting("PublicWebBaseUrl", "https://acceptatie.lobsy.nl");
        builder.UseSetting("JobsyAuth:AllowStubPayments", "true");
        builder.UseSetting("TestAccounts:EmailDomain", "lobsy.nl");
        builder.UseSetting("TestAccounts:Password:Candidate", "fake-Password-For-Tests-1");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Database=lobsy;Username=u;Password=p");

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
                         && d.ServiceType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<>)
                         && d.ServiceType.GenericTypeArguments[0] == typeof(JobsyDbContext)).ToList())
            {
                services.Remove(d);
            }

            services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(_dbName));
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
        var kandidaatLocal = new Jobsy.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "kandidaat@jobsy.local",
            FullName = "Sanne de Vries",
            Role = UserRole.Candidate,
            IsActive = true,
            IsTestAccount = true
        };
        db.Users.Add(kandidaatLocal);
        KandidaatJobsyLocalId = kandidaatLocal.Id;
        KandidaatJobsyLocalSessionVersion = kandidaatLocal.SessionVersion;
        db.SaveChanges();
        _seeded = true;
    }
}
