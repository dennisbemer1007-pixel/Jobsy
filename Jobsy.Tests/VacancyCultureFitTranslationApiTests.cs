using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
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
public class VacancyCultureFitTranslationApiTests : IClassFixture<VacancyCultureFitTranslationFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly VacancyCultureFitTranslationFactory _factory;

    public VacancyCultureFitTranslationApiTests(VacancyCultureFitTranslationFactory factory) => _factory = factory;

    [Fact]
    public async Task Viewing_same_vacancy_twice_calls_culture_fit_ai_at_most_once()
    {
        _factory.CultureAi.Reset();
        // Drain any prior queue work by running refine once for a known pair, then reset.
        using (var scope = _factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<ICandidateVacancyCultureFitService>();
            await svc.RefineAsync(_factory.CandidateId, _factory.VacancyId);
        }

        _factory.CultureAi.Reset();
        var client = Authed();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/vacancies/{_factory.VacancyId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/vacancies/{_factory.VacancyId}")).StatusCode);

        // GET path must never call AI synchronously.
        Assert.Equal(0, _factory.CultureAi.Calls);

        using (var scope = _factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<ICandidateVacancyCultureFitService>();
            await svc.RefineAsync(_factory.CandidateId, _factory.VacancyId);
        }

        // Second refine with unchanged fingerprint should skip AI (already FromOpenAi stored).
        var afterFirst = _factory.CultureAi.Calls;
        Assert.True(afterFirst <= 1);
        using (var scope = _factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<ICandidateVacancyCultureFitService>();
            await svc.RefineAsync(_factory.CandidateId, _factory.VacancyId);
        }

        Assert.Equal(afterFirst, _factory.CultureAi.Calls);
    }

    [Fact]
    public async Task Vacancy_edit_invalidates_culture_fit_and_translations()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.CandidateVacancyCultureFits.Add(new CandidateVacancyCultureFit
            {
                Id = Guid.NewGuid(),
                UserId = _factory.CandidateId,
                VacancyId = _factory.VacancyId,
                ResultJson = CultureFitStoredJson.Serialize(
                    new CultureFitResult(80, "high", "Cultuur Fit: Hoog", "Past goed.", true)),
                InputFingerprint = "seed",
                FromOpenAi = true,
                ComputedAtUtc = DateTime.UtcNow
            });
            db.VacancyTranslations.Add(new VacancyTranslation
            {
                Id = Guid.NewGuid(),
                VacancyId = _factory.VacancyId,
                Language = "en",
                SourceHash = VacancyTranslationHash.ForSource("Magazijnmedewerker", "Pakketten sorteren"),
                TranslatedJson = """{"title":"Warehouse worker","description":"Sort parcels"}""",
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var culture = scope.ServiceProvider.GetRequiredService<ICandidateVacancyCultureFitService>();
            var translation = scope.ServiceProvider.GetRequiredService<ITranslationService>();
            await culture.InvalidateForVacancyAsync(_factory.VacancyId);
            await translation.InvalidateVacancyAsync(_factory.VacancyId);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            Assert.False(await db.CandidateVacancyCultureFits.AnyAsync(r => r.VacancyId == _factory.VacancyId));
            Assert.False(await db.VacancyTranslations.AnyAsync(r => r.VacancyId == _factory.VacancyId));
        }
    }

    [Fact]
    public async Task Applications_list_serves_stored_translations_without_ai_for_hits()
    {
        _factory.TranslationProbe.Reset();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            for (var i = 0; i < 10; i++)
            {
                var vid = Guid.Parse($"e1000000-0000-0000-0000-0000000000{i:D2}");
                if (!await db.Vacancies.AnyAsync(v => v.Id == vid))
                {
                    db.Vacancies.Add(new Vacancy
                    {
                        Id = vid,
                        Title = $"Functie {i}",
                        Description = $"Omschrijving {i}",
                        CompanyId = _factory.CompanyId,
                        Status = VacancyStatus.Active,
                        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                        Location = new GeoPoint(52.09, 4.31),
                        CreatedVia = VacancySource.Manual,
                        CreatedAtUtc = DateTime.UtcNow,
                        CulturePillarsJson = """["informeel","samen","kalm"]"""
                    });
                }

                if (!await db.Applications.AnyAsync(a => a.CandidateUserId == _factory.CandidateId && a.VacancyId == vid))
                {
                    db.Applications.Add(new Application
                    {
                        Id = Guid.NewGuid(),
                        VacancyId = vid,
                        CandidateUserId = _factory.CandidateId,
                        CandidateName = "Cultuur Fit Kandidaat",
                        CandidateEmail = _factory.CandidateEmail,
                        PreferredTransport = "Fiets",
                        EstimatedTravelMinutes = 15,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                        EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-i),
                        Status = ApplicationStatus.Pending
                    });
                }

                if (i < 8)
                {
                    var title = $"Functie {i}";
                    var hash = VacancyTranslationHash.ForSource(title, "");
                    if (!await db.VacancyTranslations.AnyAsync(t => t.VacancyId == vid && t.Language == "en"))
                    {
                        db.VacancyTranslations.Add(new VacancyTranslation
                        {
                            Id = Guid.NewGuid(),
                            VacancyId = vid,
                            Language = "en",
                            SourceHash = hash,
                            TranslatedJson = $"{{\"title\":\"Role {i}\",\"description\":\"\"}}",
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        var client = Authed();
        client.DefaultRequestHeaders.Add("X-Jobsy-Language", "en");
        var response = await client.GetAsync("api/me/applications");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts))
            .EnumerateArray()
            .Select(e => e.GetProperty("vacancyTitle").GetString()!)
            .ToList();
        Assert.True(items.Count >= 10);
        Assert.Equal(8, items.Count(t => t.StartsWith("Role ", StringComparison.Ordinal)));
        // Stored hits must not call OpenAI; missing rows return Dutch when no API key.
        Assert.Equal(0, _factory.TranslationProbe.Calls);
    }

    [Fact]
    public async Task Vacancy_get_never_waits_on_culture_fit_ai()
    {
        _factory.CultureAi.BlockAndCount = true;
        _factory.CultureAi.Reset();
        var client = Authed();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var response = await client.GetAsync($"api/vacancies/{_factory.VacancyId}", cts.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("cultureFitStatus", out var status));
        Assert.Contains(status.GetString(), ["Ready", "Updating"], StringComparer.OrdinalIgnoreCase);
        Assert.Equal(0, _factory.CultureAi.Calls);
        _factory.CultureAi.BlockAndCount = false;
    }

    private HttpClient Authed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", _factory.CandidateEmail);
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", VacancyCultureFitTranslationFactory.DevSecret);
        return client;
    }
}

public sealed class VacancyCultureFitTranslationFactory : WebApplicationFactory<Program>
{
    public const string DevSecret = "culture-fit-tr-secret";
    public Guid CandidateId { get; } = Guid.Parse("e2000000-0000-0000-0000-000000000020");
    public Guid CompanyId { get; } = Guid.Parse("e2000000-0000-0000-0000-000000000001");
    public Guid VacancyId { get; } = Guid.Parse("e2000000-0000-0000-0000-000000000010");
    public string CandidateEmail => "culturefit-kandidaat@jobsy.local";
    public SpyCultureFitAi CultureAi { get; } = new();
    public CountingHttpFactory TranslationProbe { get; } = new();

    private readonly string _dbName = "CultureFitTr-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("JobsyAuth:AllowDevelopmentAuth", "true");
        builder.UseSetting("JobsyAuth:DevelopmentAuthSecret", DevSecret);
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

            services.RemoveAll<ICultureFitAiService>();
            services.AddSingleton<ICultureFitAiService>(CultureAi);

            // Count OpenAI HTTP traffic from translation (IntegrationProbe client).
            services.AddHttpClient("IntegrationProbe")
                .ConfigurePrimaryHttpMessageHandler(() => TranslationProbe.Handler);
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

        db.Companies.Add(new Company
        {
            Id = CompanyId,
            Name = "Cultuur Fit BV",
            KvkNumber = "11223344",
            Address = "Teststraat 1, Den Haag",
            Location = new GeoPoint(52.09, 4.31)
        });
        db.Users.Add(new User
        {
            Id = CandidateId,
            Email = CandidateEmail,
            FullName = "Cultuur Fit Kandidaat",
            FirstName = "Cultuur",
            LastName = "Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = new DateOnly(1996, 3, 1),
            OpenForWork = true,
            HomeLocation = new GeoPoint(52.09, 4.31),
            PreferencesJson = JsonSerializer.Serialize(new
            {
                roles = new[] { "Magazijn" },
                maxTravelMinutes = 40,
                preferredTransport = "Fiets",
                aboutMe = "Ik werk graag in een informeel team.",
                language = "en",
                minHoursPerWeek = 12,
                maxHoursPerWeek = 32
            })
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = VacancyId,
            Title = "Magazijnmedewerker",
            Description = "Pakketten sorteren",
            CompanyId = CompanyId,
            Status = VacancyStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40)),
            Location = new GeoPoint(52.08, 4.30),
            CreatedVia = VacancySource.Manual,
            CreatedAtUtc = DateTime.UtcNow,
            CulturePillarsJson = """["informeel","samen","kalm"]""",
            WorkTypeLabels = "Magazijn",
            MinHoursPerWeek = 16,
            MaxHoursPerWeek = 32
        });
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = CandidateId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}",
            SamenwerkenPercent = 85,
            ResultaatgerichtheidPercent = 55,
            StressbestendigheidPercent = 80,
            InnovatiePercent = 50,
            ExtraversiePercent = 88,
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
            InformalPercent = 80,
            CollaborationPercent = 85,
            FlexibilityPercent = 60,
            InnovationPercent = 50,
            PeopleFirstPercent = 70,
            OpennessPercent = 55,
            ConscientiousnessPercent = 65,
            ExtraversionPercent = 80,
            AgreeablenessPercent = 75,
            EmotionalStabilityPercent = 70,
            CompletedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
        _seeded = true;
    }

    public sealed class SpyCultureFitAi : ICultureFitAiService
    {
        public int Calls;
        public bool BlockAndCount;

        public void Reset() => Calls = 0;

        public async Task<CultureFitResult?> TryRefineAsync(
            CultureFitResult local,
            CompetencyScores scores,
            IReadOnlyList<string> pillarLabels,
            CulturePersonalityScores? culture = null,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            if (BlockAndCount)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }

            return local with { FromOpenAi = true, Why = local.Why + " (AI)" };
        }
    }

    public sealed class CountingHttpFactory
    {
        public int Calls;
        public CountingHandler Handler { get; }

        public CountingHttpFactory() => Handler = new CountingHandler(this);

        public void Reset() => Calls = 0;

        public sealed class CountingHandler : HttpMessageHandler
        {
            private readonly CountingHttpFactory _owner;

            public CountingHandler(CountingHttpFactory owner) => _owner = owner;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _owner.Calls);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"error\":\"no-key\"}")
                });
            }
        }
    }
}
