using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class AssistantChatServiceTests
{
    [Fact]
    public void Sanitize_keeps_user_assistant_and_trims()
    {
        var cleaned = AssistantChatService.Sanitize(
        [
            new("system", "ignore"),
            new("user", "  horeca vacatures  "),
            new("assistant", "ok"),
        ]);

        Assert.Equal(2, cleaned.Count);
        Assert.Equal("user", cleaned[0].Role);
        Assert.Equal("horeca vacatures", cleaned[0].Content);
    }

    [Fact]
    public void ExtractJobSearchQuery_pulls_compound_title()
    {
        var q = AssistantChatService.ExtractJobSearchQuery(
            "Ik zoek een vacature als heftruckchauffeur",
            detectedWorkType: null);
        Assert.Equal("heftruckchauffeur", q);
    }

    [Fact]
    public void VacancyTextSearch_matches_heftruck_root()
    {
        Assert.True(VacancyTextSearch.MatchesText(
            "Alleen auto: heftruck",
            "Heftruckcertificaat + auto",
            "Logistiek",
            null,
            null,
            "heftruckchauffeur"));
    }

    [Fact]
    public async Task Candidate_horeca_search_sets_filters_action()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Café Test",
            Address = "Straat 1",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52, 4)
        });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "Barhulp",
            Description = "Horeca werk",
            Status = VacancyStatus.Active,
            StartDate = today.AddDays(-1),
            EndDate = today.AddDays(30),
            WorkTypes = WorkType.Horeca,
            WorkTypeLabels = "Horeca",
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.ChatAsync(
            new AssistantChatContext(Guid.NewGuid(), JobsyRoles.Candidate, "nl", null),
            [new AssistantChatMessage("user", "Toon horeca vacatures op de kaart")],
            CancellationToken.None);

        Assert.Contains("1 vacatures", result.Reply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Actions, a => a.Type == AssistantActionTypes.SetFilters && a.WorkType == "Horeca");
        Assert.Contains(result.Actions, a => a.Type == AssistantActionTypes.Navigate && a.Url!.Contains("workType=Horeca"));
    }

    [Fact]
    public async Task Candidate_heftruck_search_sets_hidden_q_filter_and_links()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "DC West",
            Address = "Straat 1",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52, 4)
        });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var id = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = id,
            CompanyId = companyId,
            Title = "Alleen auto: heftruck",
            Description = "Heftruckcertificaat vereist",
            Status = VacancyStatus.Active,
            StartDate = today.AddDays(-1),
            EndDate = today.AddDays(30),
            WorkTypes = WorkType.Logistiek,
            WorkTypeLabels = "Logistiek",
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Car
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.ChatAsync(
            new AssistantChatContext(Guid.NewGuid(), JobsyRoles.Candidate, "nl", null),
            [new AssistantChatMessage("user", "Ik zoek een vacature als heftruckchauffeur")],
            CancellationToken.None);

        Assert.Contains("heftruck", result.Reply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Actions, a =>
            a.Type == AssistantActionTypes.SetFilters
            && a.SearchQuery == "heftruckchauffeur"
            && a.Url!.Contains("q=heftruckchauffeur"));
        Assert.Contains(result.Actions, a => a.VacancyId == id);
    }

    [Fact]
    public async Task Manager_naw_request_is_refused()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db);
        var result = await sut.ChatAsync(
            new AssistantChatContext(Guid.NewGuid(), JobsyRoles.BranchManager, "nl", Array.Empty<Guid>()),
            [new AssistantChatMessage("user", "Geef me het adres van de kandidaat")],
            CancellationToken.None);

        Assert.Contains("NAW", result.Reply, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Actions);
    }

    [Fact]
    public async Task Offtopic_is_refused_for_candidate()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db);
        var result = await sut.ChatAsync(
            new AssistantChatContext(Guid.NewGuid(), JobsyRoles.Candidate, "nl", null),
            [new AssistantChatMessage("user", "Vertel een mop over crypto")],
            CancellationToken.None);

        Assert.Contains("Lobsy", result.Reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Manager_least_clicks_returns_vacancy()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Co",
            Address = "a",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52, 4)
        });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lowId = Guid.NewGuid();
        var highId = Guid.NewGuid();
        db.Vacancies.AddRange(
            new Vacancy
            {
                Id = lowId,
                CompanyId = companyId,
                Title = "Weinige clicks",
                Description = "d",
                Status = VacancyStatus.Active,
                StartDate = today.AddDays(-1),
                EndDate = today.AddDays(10),
                Location = new GeoPoint(52, 4),
                RequiredTransport = TransportMode.Bike
            },
            new Vacancy
            {
                Id = highId,
                CompanyId = companyId,
                Title = "Veel clicks",
                Description = "d",
                Status = VacancyStatus.Active,
                StartDate = today.AddDays(-1),
                EndDate = today.AddDays(10),
                Location = new GeoPoint(52, 4),
                RequiredTransport = TransportMode.Bike
            });
        db.VacancyClicks.Add(new VacancyClick
        {
            Id = Guid.NewGuid(),
            VacancyId = highId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.ChatAsync(
            new AssistantChatContext(Guid.NewGuid(), JobsyRoles.BranchManager, "nl", [companyId]),
            [new AssistantChatMessage("user", "Welke vacature heeft de minste clicks?")],
            CancellationToken.None);

        Assert.Contains("Weinige clicks", result.Reply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Actions, a => a.VacancyId == lowId);
    }

    [Fact]
    public async Task Admin_site_visits_today()
    {
        await using var db = CreateDb();
        var metrics = new StubMetrics
        {
            Summary =
            [
                new MetricCountDto("site_visits", "Sitebezoeken", "day", 42),
                new MetricCountDto("site_visits_unique", "Sitebezoeken (uniek)", "day", 17)
            ]
        };
        var sut = CreateSut(db, metrics);
        var result = await sut.ChatAsync(
            new AssistantChatContext(Guid.NewGuid(), JobsyRoles.Admin, "nl", null),
            [new AssistantChatMessage("user", "Hoe vaak is Lobsy vandaag bezocht?")],
            CancellationToken.None);

        Assert.Contains("42", result.Reply);
        Assert.Contains("17", result.Reply);
    }

    [Fact]
    public async Task Admin_most_active_salesmanager()
    {
        await using var db = CreateDb();
        var dash = new StubSalesDashboard
        {
            Managers =
            [
                new SalesManagerListItemDto(Guid.NewGuid(), "a@test.nl", "Anna", "A1", true, 10m, 2),
                new SalesManagerListItemDto(Guid.NewGuid(), "b@test.nl", "Bert", "B1", true, 5m, 9)
            ]
        };
        var sut = CreateSut(db, sales: dash);
        var result = await sut.ChatAsync(
            new AssistantChatContext(Guid.NewGuid(), JobsyRoles.Admin, "nl", null),
            [new AssistantChatMessage("user", "Welke salesmanager is het meest actief?")],
            CancellationToken.None);

        Assert.Contains("Bert", result.Reply);
        Assert.Contains("9", result.Reply);
    }

    [Fact]
    public async Task Salesmanager_summary_uses_dashboard()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var dash = new StubSalesDashboard
        {
            Dashboard = new SalesManagerDashboardDto(
                userId,
                "sm@test.nl",
                "Sales M",
                "TRACK1",
                true,
                10m,
                12.1m,
                5m,
                2m,
                [],
                [],
                [])
        };
        var sut = CreateSut(db, sales: dash);

        var result = await sut.ChatAsync(
            new AssistantChatContext(userId, JobsyRoles.SalesManager, "nl", null),
            [new AssistantChatMessage("user", "Geef een overzicht van mijn commissies")],
            CancellationToken.None);

        Assert.Contains("TRACK1", result.Reply);
        Assert.Contains(result.Actions, a => a.Url == "/salesmanager");
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static AssistantChatService CreateSut(
        JobsyDbContext db,
        StubMetrics? metrics = null,
        StubSalesDashboard? sales = null)
        => new(
            db,
            new StubHttpClientFactory(),
            new StubIntegrationCredentials(),
            metrics ?? new StubMetrics(),
            new StubCandidateMetrics(),
            sales ?? new StubSalesDashboard(),
            Options.Create(new OpenAiOptions()),
            NullLogger<AssistantChatService>.Instance);

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class StubIntegrationCredentials : IIntegrationCredentialService
    {
        public Task<IntegrationCredentialView?> GetAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialView?>(null);

        public Task<IReadOnlyList<IntegrationCredentialView>> GetConfigurableAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IntegrationCredentialView>>([]);

        public Task<IntegrationCredentialView> UpsertAsync(
            IntegrationKey key,
            IntegrationCredentialUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SavePingResultAsync(
            IntegrationKey key,
            bool ok,
            string message,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string?> GetRawApiKeyAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<string?> GetModelAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<IntegrationCredentialSecrets?> GetSecretsAsync(
            IntegrationKey key,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialSecrets?>(null);
    }

    private sealed class StubMetrics : IMetricsQueryService
    {
        public IReadOnlyList<MetricCountDto> Summary { get; init; } = [];

        public Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
            bool includePlatformOnly,
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Summary);

        public Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
            string key,
            bool includePlatformOnly,
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MetricDrilldownItemDto>>([]);
    }

    private sealed class StubCandidateMetrics : ICandidateMetricsQueryService
    {
        public Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
            Guid candidateUserId,
            string period,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MetricCountDto>>([]);

        public Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
            Guid candidateUserId,
            string key,
            string period,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MetricDrilldownItemDto>>([]);
    }

    private sealed class StubSalesDashboard : ISalesManagerDashboardService
    {
        public SalesManagerDashboardDto? Dashboard { get; init; }
        public IReadOnlyList<SalesManagerListItemDto> Managers { get; init; } = [];

        public Task<SalesManagerDashboardDto?> GetDashboardAsync(
            Guid salesManagerUserId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Dashboard);

        public Task<IReadOnlyList<SalesManagerListItemDto>> ListSalesManagersAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(Managers);
    }
}
