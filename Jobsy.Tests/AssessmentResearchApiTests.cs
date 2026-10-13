using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
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

[Collection("SequentialApi")]
public class AssessmentResearchApiTests : IClassFixture<AssessmentResearchApiFactory>
{
    private readonly AssessmentResearchApiFactory _factory;

    public AssessmentResearchApiTests(AssessmentResearchApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Consent_export_includes_item_rows_with_test_version_and_respects_revocation()
    {
        var admin = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);
        await admin.PutAsJsonAsync("api/admin/research/settings", new { researchConsentEnabled = true });

        var candidate = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);
        var accept = await candidate.PostAsJsonAsync(
            "api/me/research/consent",
            new
            {
                demographics = new
                {
                    ageBand = "25-34",
                    gender = "female",
                    educationLevel = "hbo",
                    nativeLanguage = "nl",
                    testLanguage = "nl"
                }
            });
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        var answers = Enumerable.Range(1, CompetencyTestCatalog.QuestionCount)
            .ToDictionary(i => i, _ => 3);
        var save = await candidate.PutAsJsonAsync(
            "api/me/competencies",
            new { answers, complete = true });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var exportBefore = await admin.GetAsync("api/admin/research/export.csv");
        Assert.Equal(HttpStatusCode.OK, exportBefore.StatusCode);
        var csvBefore = await exportBefore.Content.ReadAsStringAsync();
        Assert.Contains("competence_quickscan", csvBefore, StringComparison.Ordinal);
        Assert.Contains("test_version", csvBefore, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(",1,", csvBefore, StringComparison.Ordinal);

        await candidate.DeleteAsync("api/me/research/consent");
        var exportAfter = await admin.GetAsync("api/admin/research/export.csv");
        var csvAfter = await exportAfter.Content.ReadAsStringAsync();
        Assert.DoesNotContain("competence_quickscan", csvAfter, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dashboard_reports_target_n300()
    {
        var admin = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);
        await admin.PutAsJsonAsync("api/admin/research/settings", new { researchConsentEnabled = true });
        var dash = await admin.GetFromJsonAsync<DashboardDto>("api/admin/research/dashboard");
        Assert.NotNull(dash);
        Assert.Equal(PrivacyConstants.ResearchExportTargetRespondents, dash!.TargetPerCell);
    }

    private sealed class DashboardDto
    {
        public List<DashboardRowDto> Rows { get; set; } = [];
        public int TargetPerCell { get; set; }
    }

    private sealed class DashboardRowDto
    {
        public string TestKey { get; set; } = "";
        public int ConsentedCount { get; set; }
        public bool Suppressed { get; set; }
    }
}

public sealed class AssessmentResearchApiFactory : WebApplicationFactory<Program>
{
    public Guid AdminId { get; } = Guid.Parse("a1000000-0000-0000-0000-000000000099");
    public Guid CandidateId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000099");

    private readonly string _dbName = "ResearchApi-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");
        builder.UseSetting("JobsyFeatures:ResearchConsentEnabled", "true");

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
        if (db.Users.Any(u => u.Id == CandidateId))
        {
            _seeded = true;
            return;
        }

        db.Users.AddRange(
            new User
            {
                Id = AdminId,
                Email = "admin.research@jobsy.local",
                FullName = "Admin Research",
                Role = UserRole.Admin,
                IsActive = true,
                ConsentVersion = PrivacyConstants.CurrentConsentVersion
            },
            new User
            {
                Id = CandidateId,
                Email = "candidate.research@jobsy.local",
                FullName = "Candidate Research",
                Role = UserRole.Candidate,
                IsActive = true,
                DateOfBirth = new DateOnly(1995, 1, 1),
                ConsentVersion = PrivacyConstants.CurrentConsentVersion,
                TestAiConsentAt = DateTime.UtcNow,
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            });
        db.SaveChanges();
        _seeded = true;
    }
}
