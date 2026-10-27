using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
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
public sealed class AcceptatieAfterMatchDemoEnsureExternalPurgeTests : IClassFixture<AcceptatieAfterMatchDemoEnsureApiFactory>
{
    private readonly AcceptatieAfterMatchDemoEnsureApiFactory _factory;

    public AcceptatieAfterMatchDemoEnsureExternalPurgeTests(AcceptatieAfterMatchDemoEnsureApiFactory factory)
        => _factory = factory;

    [Fact]
    public async Task After_match_demo_ensure_purges_allowlisted_external_vacancies()
    {
        _ = _factory.CreateClient();
        var staleId = Guid.Parse("73349dcd-0000-4000-8000-000000000099");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.CandidateExternalVacancies.Add(new Jobsy.Core.Entities.CandidateExternalVacancy
            {
                Id = staleId,
                CandidateUserId = _factory.KandidaatJobsyLocalId,
                SourceUrl = "https://example.com/acc-external-test",
                SourceHost = "example.com",
                Title = "Acc external test",
                CompanyName = "BV",
                Status = CandidateExternalVacancyStatus.Applied,
                SavedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        using var client = JobsyTestAuth.CreateAuthenticatedClient(
            _factory,
            _factory.OndernemerJobsyLocalId,
            _factory.OndernemerJobsyLocalSessionVersion,
            includeOriginHeader: true);
        var response = await client.PostAsync("api/me/acceptatie-after-match-demo-ensure", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AfterMatchEnsureResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Applied);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            Assert.False(await db.CandidateExternalVacancies.AnyAsync(v => v.Id == staleId));
        }
    }

    private sealed record AfterMatchEnsureResponse(bool Applied, string? SkipReason);
}

public sealed class AcceptatieAfterMatchDemoEnsureApiFactory : ApiWebApplicationFactory
{
    public Guid KandidaatJobsyLocalId { get; private set; }
    public int KandidaatJobsyLocalSessionVersion { get; private set; }
    public Guid OndernemerJobsyLocalId { get; private set; }
    public int OndernemerJobsyLocalSessionVersion { get; private set; }

    private readonly string _dbName = "AfterMatchEnsurePurge-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        JobsyTestAuth.ApplyProductionJwtSettings(builder);
        JobsyTestAuth.ApplyProductionOriginSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("VerificationCodes:Pepper", "test-verification-pepper-at-least-32-chars-long");
        builder.UseSetting("JobsyAuth:AllowEphemeralDataProtection", "true");
        builder.UseSetting("Lobsy:DeploymentEnvironment", "Acceptatie");
        builder.UseSetting("TestAccounts:Enabled", "true");
        builder.UseSetting("RENDER_SERVICE_NAME", "lobsy-acc-api");
        builder.UseSetting("PublicWebBaseUrl", "https://acceptatie.lobsy.nl");
        builder.UseSetting("JobsyAuth:AllowStubPayments", "true");
        builder.UseSetting("TestAccounts:EmailDomain", "jobsy.local");
        builder.UseSetting("ConnectionStrings:JobsyDb", "Host=127.0.0.1;Database=lobsy;Username=u;Password=p");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            ReplaceDbWithInMemory(services, _dbName);
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
        var kandidaat = new Jobsy.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "kandidaat@jobsy.local",
            FullName = "Sanne de Vries",
            Role = UserRole.Candidate,
            IsActive = true,
            IsTestAccount = true,
        };
        var ondernemer = new Jobsy.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "ondernemer@jobsy.local",
            FullName = "Ondernemer Demo",
            Role = UserRole.BranchManager,
            IsActive = true,
            IsTestAccount = true,
        };
        db.Users.AddRange(kandidaat, ondernemer);
        KandidaatJobsyLocalId = kandidaat.Id;
        KandidaatJobsyLocalSessionVersion = kandidaat.SessionVersion;
        OndernemerJobsyLocalId = ondernemer.Id;
        OndernemerJobsyLocalSessionVersion = ondernemer.SessionVersion;
        db.SaveChanges();
        _seeded = true;
    }

    private static void ReplaceDbWithInMemory(IServiceCollection services, string dbName)
    {
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

        services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(dbName));
    }
}
