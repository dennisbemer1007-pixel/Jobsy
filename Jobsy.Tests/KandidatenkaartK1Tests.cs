using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.ValueObjects;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public sealed class KandidatenkaartK1Tests
{
    [Fact]
    public void Program_registers_authenticated_write_rate_limit_policy()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Api", "Program.cs"));
        Assert.Contains("AddPolicy(\"authenticated-write\"", program, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiJson_deserializes_kandidatenkaart_invite_preference_as_string_enum()
    {
        const string json = """{"invitePreference":"Max","showReAvailabilityPrompt":false}""";
        var dto = JsonSerializer.Deserialize<CandidateKandidatenkaartSettingsDto>(json, JobsyApiClient.ApiJson);
        Assert.NotNull(dto);
        Assert.Equal(KandidatenkaartInvitePreferenceView.Max, dto!.InvitePreference);
    }

    [Fact]
    public void ApiJson_round_trips_kandidatenkaart_settings_update()
    {
        var update = new CandidateKandidatenkaartSettingsUpdate(KandidatenkaartInvitePreferenceView.Normal);
        var json = JsonSerializer.Serialize(update, JobsyApiClient.ApiJson);
        Assert.Contains("\"Normal\"", json, StringComparison.Ordinal);
        var back = JsonSerializer.Deserialize<CandidateKandidatenkaartSettingsUpdate>(json, JobsyApiClient.ApiJson);
        Assert.Equal(KandidatenkaartInvitePreferenceView.Normal, back!.InvitePreference);
    }

    [Fact]
    public async Task Invite_post_does_not_return_500_when_rate_limit_policy_is_registered()
    {
        await using var factory = new KandidatenkaartK1ApiFactory();
        var client = JobsyTestAuth.CreateAuthenticatedClient(factory, factory.EmployerUserId);
        using var response = await client.PostAsJsonAsync(
            "api/employer/kandidatenkaart/invite",
            new
            {
                vacancyId = factory.VacancyId,
                areaKey = "demo",
                companyId = factory.CompanyId,
                minMatchPercent = 70,
                travelKm = 25
            });

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Map_without_vacancy_returns_vacancy_required_not_server_error()
    {
        await using var factory = new KandidatenkaartK1ApiFactory();
        var client = JobsyTestAuth.CreateAuthenticatedClient(factory, factory.EmployerUserId);
        using var response = await client.GetAsync(
            $"api/employer/kandidatenkaart/map?companyId={factory.CompanyId:D}&minMatch=70&travelKm=25");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("vacancy_required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Candidate_settings_get_returns_string_invite_preference()
    {
        await using var factory = new KandidatenkaartK1ApiFactory();
        var client = JobsyTestAuth.CreateAuthenticatedClient(factory, factory.CandidateUserId);
        using var response = await client.GetAsync("api/me/kandidatenkaart/settings");
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"invitePreference\":", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"invitePreference\":0", raw, StringComparison.Ordinal);
        var dto = JsonSerializer.Deserialize<CandidateKandidatenkaartSettingsDto>(raw, JobsyApiClient.ApiJson);
        Assert.Equal(KandidatenkaartInvitePreferenceView.Normal, dto!.InvitePreference);
    }
}

file sealed class KandidatenkaartK1ApiFactory : ApiWebApplicationFactory
{
    public Guid CompanyId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000001");
    public Guid VacancyId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000010");
    public Guid EmployerUserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000021");
    public Guid CandidateUserId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000020");

    private readonly string _dbName = "KkK1-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
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

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        EnsureSeeded(host.Services);
        return host;
    }

    private void EnsureSeeded(IServiceProvider services)
    {
        if (_seeded)
        {
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (db.Users.Any(u => u.Id == EmployerUserId))
        {
            _seeded = true;
            return;
        }

        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            KandidatenkaartEnabled = true,
            EmployersEnabled = true,
            CandidatePassportEnabled = true
        });

        db.Companies.Add(new Company
        {
            Id = CompanyId,
            Name = "KK Test Co",
            KvkNumber = "12345678",
            Address = "Test 1",
            Location = new GeoPoint(52.0, 4.2),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            KandidatenkaartActiveUntilUtc = DateTime.UtcNow.AddYears(1),
            IsTestData = true
        });

        db.Users.Add(new User
        {
            Id = EmployerUserId,
            Email = "kk-employer@jobsy.local",
            FullName = "KK Employer",
            Role = UserRole.BranchManager,
            IsActive = true,
            CompanyId = CompanyId,
            TermsAcceptedAt = DateTime.UtcNow,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion
        });

        db.Users.Add(new User
        {
            Id = CandidateUserId,
            Email = "kk-candidate@jobsy.local",
            FullName = "KK Candidate",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            TermsAcceptedAt = DateTime.UtcNow,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            HomeLocation = new GeoPoint(52.09, 4.31)
        });

        db.CandidateKandidatenkaartSettings.Add(new CandidateKandidatenkaartSettings
        {
            UserId = CandidateUserId,
            InvitePreference = KandidatenkaartInvitePreference.Normal
        });

        db.SaveChanges();
        _seeded = true;
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
