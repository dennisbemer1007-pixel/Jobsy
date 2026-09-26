using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
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

[Collection("SequentialApi")]
public class CandidateInsightsOnWriteApiTests : IClassFixture<CandidateInsightsOnWriteFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly CandidateInsightsOnWriteFactory _factory;

    public CandidateInsightsOnWriteApiTests(CandidateInsightsOnWriteFactory factory) => _factory = factory;

    [Fact]
    public async Task Kompas_returns_profile_tests_matches_and_insights_status_in_one_response()
    {
        var client = Authed();
        var response = await client.GetAsync("api/me/kompas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("profile", out var profile));
        Assert.Equal(_factory.CandidateEmail, profile.GetProperty("email").GetString());
        Assert.True(body.TryGetProperty("competencies", out _));
        Assert.True(body.TryGetProperty("careerInterests", out _));
        Assert.True(body.TryGetProperty("culture", out _));
        Assert.True(body.TryGetProperty("values", out _));
        Assert.True(body.TryGetProperty("topMatches", out _));
        Assert.True(body.TryGetProperty("insightsStatus", out var status));
        Assert.Contains(status.GetString(), ["Ready", "Updating"], StringComparer.OrdinalIgnoreCase);
        Assert.True(body.TryGetProperty("whoAmI", out var whoAmI));
        Assert.Equal(JsonValueKind.Object, whoAmI.ValueKind);
        Assert.True(whoAmI.TryGetProperty("status", out var whoStatus));
        Assert.Contains(whoStatus.GetString(), ["Empty", "Ready", "Updating"], StringComparer.OrdinalIgnoreCase);
        Assert.True(whoAmI.TryGetProperty("keywords", out var keywords));
        Assert.Equal(JsonValueKind.Array, keywords.ValueKind);
        // story may be omitted when null (WhenWritingNull)
        if (whoAmI.TryGetProperty("story", out var story))
        {
            Assert.True(story.ValueKind is JsonValueKind.String or JsonValueKind.Null);
        }
        Assert.True(body.TryGetProperty("profileCompletenessPercent", out var completeness));
        Assert.InRange(completeness.GetInt32(), 0, 100);
    }

    [Fact]
    public async Task WhoAmI_career_matches_and_role_fit_gets_never_call_ai()
    {
        _factory.Ai.Reset();
        var client = Authed();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/me/who-am-i")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/me/career-interests")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/me/matched-vacancies")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/me/role-fit")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/me/kompas")).StatusCode);

        Assert.Equal(0, _factory.Ai.WhoAmICalls);
    }

    [Fact]
    public async Task Completing_competency_test_enqueues_and_computer_writes_match_snapshot()
    {
        var client = Authed();
        var answers = Enumerable.Range(1, 25).ToDictionary(i => i.ToString(), _ => 4);
        var put = await client.PutAsJsonAsync("api/me/competencies", new { answers, complete = true });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var computer = scope.ServiceProvider.GetRequiredService<ICandidateInsightsComputer>();
        await computer.RecomputeAsync(_factory.CandidateId);

        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var snapshot = await db.CandidateMatchSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == _factory.CandidateId);
        Assert.NotNull(snapshot);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.InputFingerprint));
        Assert.Equal(InsightsStatuses.Ready, snapshot.Status);

        var matches = await client.GetAsync("api/me/matched-vacancies");
        Assert.Equal(HttpStatusCode.OK, matches.StatusCode);
    }

    [Fact]
    public async Task Role_fit_get_returns_stored_snapshot_without_ai()
    {
        _factory.Ai.Reset();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var existing = await db.CandidateRoleFitChecks
                .FirstOrDefaultAsync(r => r.UserId == _factory.CandidateId);
            if (existing is not null)
            {
                db.CandidateRoleFitChecks.Remove(existing);
            }

            db.CandidateRoleFitChecks.Add(new CandidateRoleFitCheck
            {
                Id = Guid.NewGuid(),
                UserId = _factory.CandidateId,
                JobTitle = "Vulploegmedewerker",
                MatchPercent = 82,
                ResultJson = RoleFitCheckJson.Serialize(RoleFitCheckBuilder.Build(
                    "Vulploegmedewerker",
                    new CompetencyScores(80, 70, 75, 40, 55),
                    new RiasecScores(40, 30, 20, 50, 35, 60),
                    fromDeepAnalysis: false)),
                InputFingerprint = "seed",
                FromDeepAnalysis = false,
                FromOpenAi = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = Authed();
        var state = await client.GetFromJsonAsync<JsonElement>("api/me/role-fit", JsonOpts);
        Assert.Equal("Vulploegmedewerker", state.GetProperty("lastResult").GetProperty("jobTitle").GetString());
        Assert.Equal(0, _factory.Ai.WhoAmICalls);
    }

    private HttpClient Authed()
    {
        return JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);
    }
}

public sealed class CandidateInsightsOnWriteFactory : WebApplicationFactory<Program>
{
    public Guid CandidateId { get; } = Guid.Parse("d1000000-0000-0000-0000-000000000020");
    public string CandidateEmail => "insights-kandidaat@jobsy.local";
    public SpyWhoAmIAi Ai { get; } = new();

    private readonly string _dbName = "InsightsOnWrite-" + Guid.NewGuid();
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

            services.RemoveAll<IWhoAmIGenerationService>();
            services.AddSingleton<IWhoAmIGenerationService>(Ai);
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

        db.Users.Add(new User
        {
            Id = CandidateId,
            Email = CandidateEmail,
            FullName = "Insights Kandidaat",
            FirstName = "Insights",
            LastName = "Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = new DateOnly(1995, 4, 12),
            OpenForWork = true,
            TestAiConsentAt = DateTime.UtcNow,
            TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion,
            HomeLocation = new GeoPoint(52.09, 4.31),
            PreferencesJson = JsonSerializer.Serialize(new
            {
                roles = new[] { "Winkel" },
                maxTravelMinutes = 30,
                preferredTransport = "Fiets",
                aboutMe = "Ik werk graag in de winkel.",
                minHoursPerWeek = 12,
                maxHoursPerWeek = 24
            })
        });

        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = CandidateId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}",
            SamenwerkenPercent = 80,
            ResultaatgerichtheidPercent = 70,
            StressbestendigheidPercent = 75,
            InnovatiePercent = 40,
            ExtraversiePercent = 55,
            CompletedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.CandidateCareerInterests.Add(new CandidateCareerInterest
        {
            Id = Guid.NewGuid(),
            UserId = CandidateId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}",
            RealisticPercent = 40,
            InvestigativePercent = 30,
            ArtisticPercent = 20,
            SocialPercent = 50,
            EnterprisingPercent = 35,
            ConventionalPercent = 60,
            HollandCode = "CSR",
            RiasecTagsJson = "[]",
            MatchTagsJson = "[]",
            CompassJson = "",
            CompletedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
        {
            Id = Guid.NewGuid(),
            UserId = CandidateId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}",
            AutonomyPercent = 70,
            InformalPercent = 60,
            CollaborationPercent = 80,
            FlexibilityPercent = 55,
            InnovationPercent = 50,
            PeopleFirstPercent = 65,
            OpennessPercent = 55,
            ConscientiousnessPercent = 70,
            ExtraversionPercent = 60,
            AgreeablenessPercent = 75,
            EmotionalStabilityPercent = 70,
            CompletedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
        _seeded = true;
    }

    public sealed class SpyWhoAmIAi : IWhoAmIGenerationService
    {
        public int WhoAmICalls;

        public void Reset() => WhoAmICalls = 0;

        public Task<WhoAmIGeneratedStory> GenerateAsync(
            CompetencyScores competency,
            RiasecScores career,
            CulturePersonalityScores culture,
            WhoAmIProfileHighlights? profile = null,
            SchwartzValuesScores? values = null,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref WhoAmICalls);
            // Worker/write path may call this; GETs must keep WhoAmICalls at 0.
            return Task.FromResult(new WhoAmIGeneratedStory(
                "Ik werk graag samen en vind duidelijkheid belangrijk.",
                ["samenwerken", "duidelijkheid"],
                FromOpenAi: false));
        }
    }
}
