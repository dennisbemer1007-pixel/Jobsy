using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Tests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.TestAccounts;

[Collection("SequentialApi")]
public sealed class AcceptatieSollicitatiesDemoEnsureApiTests : IClassFixture<AcceptatieSollicitatiesDemoEnsureApiFactory>
{
    private readonly AcceptatieSollicitatiesDemoEnsureApiFactory _factory;

    public AcceptatieSollicitatiesDemoEnsureApiTests(AcceptatieSollicitatiesDemoEnsureApiFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Ensure_returns_not_found_for_non_allowlisted_candidate()
    {
        _ = _factory.CreateClient();
        using var client = JobsyTestAuth.CreateAuthenticatedClient(
            _factory,
            _factory.OtherCandidateId,
            _factory.OtherCandidateSessionVersion);
        var response = await client.PostAsync("api/me/acceptatie-sollicitaties-demo-ensure", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Ensure_seeds_mockup_rows_for_kandidaat_jobsy_local()
    {
        _ = _factory.CreateClient();
        using var client = JobsyTestAuth.CreateAuthenticatedClient(
            _factory,
            _factory.KandidaatJobsyLocalId,
            _factory.KandidaatJobsyLocalSessionVersion);
        var response = await client.PostAsync("api/me/acceptatie-sollicitaties-demo-ensure", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EnsureDto>();
        Assert.NotNull(body);
        Assert.True(body.Applied);
        Assert.True(body.VerifiedApplicationCount >= 6);
    }

    [Fact]
    public async Task Ensure_seeds_mockup_rows_for_grokbot_candidate()
    {
        _ = _factory.CreateClient();
        using var client = JobsyTestAuth.CreateAuthenticatedClient(
            _factory,
            _factory.GrokbotCandidateId,
            _factory.GrokbotSessionVersion);
        var response = await client.PostAsync("api/me/acceptatie-sollicitaties-demo-ensure", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EnsureDto>();
        Assert.NotNull(body);
        Assert.True(body.Applied);
        Assert.True(body.VerifiedApplicationCount >= 6);
        Assert.True(body.MessageThreadCount >= 2, body.DemoHealthOutcome);
        Assert.Contains("threads=", body.DemoHealthOutcome, StringComparison.Ordinal);
    }

    private sealed record EnsureDto(
        bool Applied,
        int VerifiedApplicationCount,
        int MessageThreadCount,
        string? DemoHealthOutcome,
        string? SkipReason);
}

public sealed class AcceptatieSollicitatiesDemoEnsureApiFactory : ApiWebApplicationFactory
{
    public Guid GrokbotCandidateId { get; private set; }
    public int GrokbotSessionVersion { get; private set; }
    public Guid KandidaatJobsyLocalId { get; private set; }
    public int KandidaatJobsyLocalSessionVersion { get; private set; }
    public Guid OtherCandidateId { get; private set; }
    public int OtherCandidateSessionVersion { get; private set; }

    private readonly string _dbName = "SollicDemoEnsure-" + Guid.NewGuid();
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
        builder.UseSetting("TestAccounts:Password:BranchManager", "fake-Password-For-Tests-1");
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
                         && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
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
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var seeder = new TestAccountsSeedService(db, config);
        seeder.SeedAsync(dryRun: false, onlyKeys: null).GetAwaiter().GetResult();
        db.SaveChanges();

        var catalog = db.Users.Single(u => u.Email == "test-kandidaat@lobsy.nl");
        catalog.Email = TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail;
        GrokbotCandidateId = catalog.Id;
        GrokbotSessionVersion = catalog.SessionVersion;

        var kandidaatLocal = new Jobsy.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "kandidaat@jobsy.local",
            FullName = "Sanne de Vries",
            Role = Jobsy.Core.Enums.UserRole.Candidate,
            IsActive = true,
            IsTestAccount = true
        };
        db.Users.Add(kandidaatLocal);
        KandidaatJobsyLocalId = kandidaatLocal.Id;
        KandidaatJobsyLocalSessionVersion = kandidaatLocal.SessionVersion;

        var other = new Jobsy.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "other-candidate@lobsy.nl",
            FullName = "Other",
            Role = Jobsy.Core.Enums.UserRole.Candidate,
            IsActive = true,
            IsTestAccount = true
        };
        db.Users.Add(other);
        OtherCandidateId = other.Id;
        OtherCandidateSessionVersion = other.SessionVersion;
        db.SaveChanges();

        _seeded = true;
    }
}
