using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class EmployerRun3FollowupTests
{
    [Theory]
    [InlineData("nl", 1, "1 token")]
    [InlineData("nl", 2, "2 tokens")]
    [InlineData("en", 1, "1 token")]
    [InlineData("pl", 1, "1 token")]
    [InlineData("pl", 2, "2 tokeny")]
    [InlineData("pl", 5, "5 tokenów")]
    [InlineData("pl", 12, "12 tokenów")]
    [InlineData("ro", 1, "1 token")]
    [InlineData("ar", 0, "0 رمزاً")]
    [InlineData("ar", 1, "رمز واحد")]
    [InlineData("ar", 2, "رمزان")]
    [InlineData("ar", 4, "4 رموز")]
    [InlineData("ar", 11, "11 رمزاً")]
    public void Token_phrase_uses_the_locale_plural(string lang, int count, string expected)
    {
        var culture = new CultureState(null!, null!, null!);
        culture.InitializeFromLanguage(lang);
        Assert.Equal(expected, CountPhrase.Tokens(culture, count));
    }

    [Fact]
    public void Vacancy_and_application_phrases_are_singular_for_one()
    {
        var culture = new CultureState(null!, null!, null!);
        culture.InitializeFromLanguage("nl");
        Assert.Equal("1 vacature", CountPhrase.Vacancies(culture, 1));
        Assert.Equal("3 vacatures", CountPhrase.Vacancies(culture, 3));
        Assert.Equal("1 sollicitatie", CountPhrase.Applications(culture, 1));
        Assert.Equal("4 sollicitaties", CountPhrase.Applications(culture, 4));
        Assert.Equal(
            "Vacature en 4 sollicitaties definitief verwijderen? Dit kun je niet ongedaan maken.",
            string.Format(UiStrings.Get("AdminVacancy.Confirm.Purge", "nl"), CountPhrase.Applications(culture, 4)));
        Assert.Equal(
            "Vacature offline halen? Kandidaten zien hem dan niet meer. Weer online zetten is gratis.",
            UiStrings.Get("WgVac.Confirm.Offline", "nl"));
        foreach (var (lang, freeHint) in new[]
                 {
                     ("nl", "gratis"),
                     ("en", "free"),
                     ("pl", "bezpłatne"),
                     ("ro", "gratuit"),
                     ("ar", "مجاني"),
                 })
        {
            Assert.Contains(freeHint, UiStrings.Get("WgVac.Confirm.Offline", lang), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("competenties", UiStrings.Get("Talent.Lead", lang), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("competenc", UiStrings.Get("Talent.Lead", lang), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Match", UiStrings.Get("Insights.Premium.Check.Match", lang), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Onboarding_status_is_translatable_for_intermediary_and_not_a_500_for_sales()
    {
        await using var factory = new EmployerRun3ApiFactory();
        using var intermediary = factory.CreateClient();
        JobsyTestAuth.Authorize(intermediary, factory.IntermediaryId);
        var done = await intermediary.GetAsync("api/employer/onboarding/status");
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        using (var doc = JsonDocument.Parse(await done.Content.ReadAsStringAsync()))
        {
            var about = doc.RootElement.GetProperty("checklist").EnumerateArray()
                .Single(i => i.GetProperty("key").GetString() == "about");
            Assert.True(about.GetProperty("done").GetBoolean());
            Assert.Contains("cultuur", about.GetProperty("detail").GetString(), StringComparison.Ordinal);
        }

        using var draftClient = factory.CreateClient();
        JobsyTestAuth.Authorize(draftClient, factory.DraftIntermediaryId);
        var draft = await draftClient.GetAsync("api/employer/onboarding/status");
        Assert.Equal(HttpStatusCode.OK, draft.StatusCode);
        using (var doc = JsonDocument.Parse(await draft.Content.ReadAsStringAsync()))
        {
            var about = doc.RootElement.GetProperty("checklist").EnumerateArray()
                .Single(i => i.GetProperty("key").GetString() == "about");
            Assert.False(about.GetProperty("done").GetBoolean());
            Assert.DoesNotContain("cultuur", about.GetProperty("detail").GetString() ?? "", StringComparison.Ordinal);
        }

        using var sales = factory.CreateClient();
        JobsyTestAuth.Authorize(sales, factory.SalesId);
        var salesStatus = await sales.GetAsync("api/employer/onboarding/status");
        Assert.Equal(HttpStatusCode.Forbidden, salesStatus.StatusCode);

        var dashboard = await sales.GetAsync("api/sales/me/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.DoesNotContain(
            await db.PlatformLogs.AsNoTracking().ToListAsync(),
            l => l.Category == "Api" && l.Message.Contains("InvalidOperationException", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Intermediary_login_reads_do_not_throw()
    {
        await using var factory = new EmployerRun3ApiFactory();
        using var client = factory.CreateClient();
        JobsyTestAuth.Authorize(client, factory.IntermediaryId);

        foreach (var path in new[]
        {
            "api/companies/mine",
            "api/tokens/balance",
            "api/metrics/summary",
            "api/metrics/vacancy-performance",
            "api/metrics/client-performance",
            "api/vacancies/manage"
        })
        {
            var response = await client.GetAsync(path);
            Assert.True(
                response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden,
                $"{path} returned {(int)response.StatusCode}");
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }

    [Fact]
    public async Task Vacancy_application_count_matches_the_vacancy_pipeline()
    {
        await using var factory = new EmployerRun3ApiFactory();
        using var client = factory.CreateClient();
        JobsyTestAuth.Authorize(client, factory.EmployerId);

        var manage = await client.GetAsync("api/vacancies/manage");
        Assert.Equal(HttpStatusCode.OK, manage.StatusCode);
        using (var doc = JsonDocument.Parse(await manage.Content.ReadAsStringAsync()))
        {
            var row = doc.RootElement.EnumerateArray()
                .Single(v => v.GetProperty("id").GetGuid() == factory.VacancyAId);
            Assert.Equal(11, row.GetProperty("applicationCount").GetInt32());
        }

        var scoped = await client.GetAsync($"api/applications?vacancyId={factory.VacancyAId}");
        Assert.Equal(HttpStatusCode.OK, scoped.StatusCode);
        var scopedItems = await scoped.Content.ReadFromJsonAsync<List<AppId>>();
        Assert.NotNull(scopedItems);
        Assert.Equal(11, scopedItems.Count);
        Assert.All(scopedItems, i => Assert.Equal(factory.VacancyAId, i.VacancyId));

        var global = await client.GetAsync("api/applications");
        Assert.Equal(HttpStatusCode.OK, global.StatusCode);
        var globalItems = await global.Content.ReadFromJsonAsync<List<AppId>>();
        Assert.NotNull(globalItems);
        Assert.DoesNotContain(globalItems, i => i.VacancyId == factory.VacancyAId);
    }

    private sealed record AppId(Guid Id, Guid VacancyId);
}

public sealed class EmployerRun3ApiFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    private readonly string _dbName = "Run3-" + Guid.NewGuid().ToString("N");
    private bool _seeded;

    public Guid EmployerCompanyId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000001");
    public Guid EmployerId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000002");
    public Guid VacancyAId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000011");
    public Guid VacancyBId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000012");
    public Guid IntermediaryCompanyId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000021");
    public Guid IntermediaryId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000022");
    public Guid DraftCompanyId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000031");
    public Guid DraftIntermediaryId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000032");
    public Guid SalesId { get; } = Guid.Parse("c3000000-0000-0000-0000-000000000041");

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
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(JobsyDbContext)
                    || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                    || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                    || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                        && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                .ToList();
            foreach (var d in descriptors)
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
            services.RemoveAll<IVacancyContentModerationService>();
            services.AddSingleton<IVacancyContentModerationService>(new AllowAllModeration());
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

        _seeded = true;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.Companies.AddRange(
            Company(EmployerCompanyId, "Horeca"),
            Company(IntermediaryCompanyId, "Bureau"),
            Company(DraftCompanyId, "Bureau draft"));
        db.Users.AddRange(
            User(EmployerId, "em-run3@jobsy.local", UserRole.EnterpriseManager, EmployerCompanyId),
            User(IntermediaryId, "im-run3@jobsy.local", UserRole.Intermediary, IntermediaryCompanyId),
            User(DraftIntermediaryId, "im-draft@jobsy.local", UserRole.Intermediary, DraftCompanyId),
            User(SalesId, "sales-run3@jobsy.local", UserRole.SalesManager, null));

        db.Vacancies.AddRange(
            Vacancy(VacancyAId, EmployerCompanyId, "Horeca medewerker (test)"),
            Vacancy(VacancyBId, EmployerCompanyId, "Andere vacature"));

        var old = DateTime.UtcNow.AddDays(-20);
        for (var i = 0; i < 11; i++)
        {
            db.Applications.Add(Application(VacancyAId, $"oud-{i}@jobsy.local", old.AddMinutes(i), verified: true));
        }

        db.Applications.Add(Application(VacancyAId, "ongeverifieerd@jobsy.local", old, verified: false));

        var recent = DateTime.UtcNow;
        for (var i = 0; i < 500; i++)
        {
            db.Applications.Add(Application(VacancyBId, $"nieuw-{i}@jobsy.local", recent.AddSeconds(-i), verified: true));
        }

        db.CompanyCultureProfiles.AddRange(
            Culture(IntermediaryCompanyId, CandidateCompetencyStatuses.Completed),
            Culture(DraftCompanyId, CandidateCompetencyStatuses.Draft));
        db.CompanyValuesProfiles.AddRange(
            Values(IntermediaryCompanyId),
            Values(DraftCompanyId));
        db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
        {
            Id = Guid.Parse("c3000000-0000-0000-0000-000000000099"),
            SalesManagerUserId = SalesId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 10m,
            CompanyId = null,
            CreatedAt = DateTime.UtcNow,
            AvailableFromUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static Company Company(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        KvkNumber = id.ToString("N")[..8],
        Address = "Straat 1",
        Location = new GeoPoint(52.01, 4.21),
        Type = CompanyType.Employer,
        WorkTypeLabels = "horeca",
        VerificationStatus = CompanyVerificationStatus.Verified,
        VerificationMethod = CompanyVerificationMethod.AdminCreated,
        VerifiedAtUtc = DateTime.UtcNow.AddDays(-1)
    };

    private static User User(Guid id, string email, UserRole role, Guid? companyId) => new()
    {
        Id = id,
        Email = email,
        FullName = email,
        Role = role,
        IsActive = true,
        CompanyId = companyId
    };

    private static Vacancy Vacancy(Guid id, Guid companyId, string title) => new()
    {
        Id = id,
        Title = title,
        Description = "Je kunt direct solliciteren.",
        CompanyId = companyId,
        Status = VacancyStatus.Active,
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        Location = new GeoPoint(52.09, 4.31),
        CreatedVia = VacancySource.Manual,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static Application Application(Guid vacancyId, string email, DateTime created, bool verified) => new()
    {
        Id = Guid.NewGuid(),
        VacancyId = vacancyId,
        CandidateName = "Kandidaat",
        CandidateEmail = email,
        PreferredTransport = "Fiets",
        EstimatedTravelMinutes = 12,
        CreatedAt = created,
        EmailVerifiedAt = verified ? created : null,
        Status = ApplicationStatus.Pending
    };

    private static CompanyCultureProfile Culture(Guid companyId, string status) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Status = status,
        AnswersJson = "{}",
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
        CompletedAtUtc = status == CandidateCompetencyStatuses.Completed ? DateTime.UtcNow : null
    };

    private static CompanyValuesProfile Values(Guid companyId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        CardIdsJson = "[\"a\",\"b\",\"c\"]",
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyContentModerationResult(true, null));
    }
}
