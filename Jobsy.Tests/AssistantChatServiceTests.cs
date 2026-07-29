using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
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
        var sut = new AssistantChatService(
            db,
            new StubHttpClientFactory(),
            new StubIntegrationCredentials(),
            new StubMetrics(),
            dash,
            Options.Create(new OpenAiOptions()),
            NullLogger<AssistantChatService>.Instance);

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

    private static AssistantChatService CreateSut(JobsyDbContext db)
        => new(
            db,
            new StubHttpClientFactory(),
            new StubIntegrationCredentials(),
            new StubMetrics(),
            new StubSalesDashboard(),
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
        public Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
            bool includePlatformOnly,
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MetricCountDto>>([]);

        public Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
            string key,
            bool includePlatformOnly,
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MetricDrilldownItemDto>>([]);
    }

    private sealed class StubSalesDashboard : ISalesManagerDashboardService
    {
        public SalesManagerDashboardDto? Dashboard { get; init; }

        public Task<SalesManagerDashboardDto?> GetDashboardAsync(
            Guid salesManagerUserId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Dashboard);

        public Task<IReadOnlyList<SalesManagerListItemDto>> ListSalesManagersAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SalesManagerListItemDto>>([]);
    }
}
