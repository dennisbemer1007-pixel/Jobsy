using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public sealed class HybridDashboardCacheTests
{
    [Fact]
    public void Memory_cache_expires_after_ttl()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var cache = new DashboardMemoryCache(clock);
        var descriptor = new DashboardCacheDescriptor(
            DashboardCacheKind.MetricsSummary,
            "all",
            "week",
            IncludePlatformOnly: false,
            CompanyIds: null,
            UserId: null,
            Take: 0);

        cache.Set("metrics:all:0:week", new[] { new MetricCountDto("applications", "Sollicitaties", "week", 4) }, descriptor);

        Assert.True(cache.TryGet<IReadOnlyList<MetricCountDto>>("metrics:all:0:week", out var hit));
        Assert.Equal(4, hit![0].Value);

        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.False(cache.TryGet<IReadOnlyList<MetricCountDto>>("metrics:all:0:week", out _));
    }

    [Fact]
    public async Task Caching_metrics_service_hits_inner_once_and_overlays_pending_live()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Bakkerij",
            KvkNumber = "11112222",
            Address = "A",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "Bakker",
            Description = "x",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(now.Date),
            EndDate = DateOnly.FromDateTime(now.Date.AddDays(20)),
            Status = VacancyStatus.Active,
            CompanyId = companyId,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateName = "Live",
            CandidateEmail = "live@example.com",
            CandidateCity = "Delft",
            PreferredTransport = "Fiets",
            EstimatedTravelMinutes = 8,
            Status = ApplicationStatus.Pending,
            CreatedAt = now,
            EmailVerifiedAt = now
        });
        await db.SaveChangesAsync();

        var inner = new CountingMetricsQueryService();
        var cache = new DashboardMemoryCache();
        var live = new DashboardLiveOverlay(db, new MissingLedger(), new MissingAmbassadeurSettings());
        var sut = new CachingMetricsQueryService(inner, cache, live);

        var first = await sut.GetSummaryAsync(false, [companyId], "week");
        var second = await sut.GetSummaryAsync(false, [companyId], "week");

        Assert.Equal(1, inner.SummaryCalls);
        Assert.Equal(1, first.Single(m => m.Key == "applications_pending").Value);
        Assert.Equal(1, second.Single(m => m.Key == "applications_pending").Value);
        Assert.Equal(9, first.Single(m => m.Key == "applications").Value);
        Assert.Equal(9, second.Single(m => m.Key == "applications").Value);
    }

    [Fact]
    public async Task Drilldown_is_never_cached()
    {
        var inner = new CountingMetricsQueryService();
        var sut = new CachingMetricsQueryService(inner, new DashboardMemoryCache(), new PassthroughLiveOverlay());

        await sut.GetDrilldownAsync("applications", false, null, "week");
        await sut.GetDrilldownAsync("applications", false, null, "week");

        Assert.Equal(2, inner.DrilldownCalls);
        Assert.Equal(0, inner.SummaryCalls);
    }

    [Fact]
    public void Role_dashboards_expose_ververs_control()
    {
        var root = FindRepoRoot();
        var button = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Shared", "DashboardRefreshButton.razor"));
        Assert.Contains("Ververs", button, StringComparison.Ordinal);
        Assert.Contains("api/dashboard/refresh", File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Services", "JobsyApiClient.cs")));

        string[] files =
        [
            "Components/Pages/Admin/AdminHomePanel.razor",
            "Components/Pages/EmployerHomePanel.razor",
            "Components/Pages/Intermediary/IntermediaryDashboard.razor",
            "Components/Pages/SalesManagerHomePanel.razor",
            "Components/Pages/AmbassadeurHomePanel.razor"
        ];

        foreach (var relative in files)
        {
            var text = File.ReadAllText(Path.Combine(root, "Jobsy.Web", relative));
            Assert.Contains("DashboardRefreshButton", text, StringComparison.Ordinal);
        }
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private sealed class CountingMetricsQueryService : IMetricsQueryService
    {
        public int SummaryCalls { get; private set; }
        public int DrilldownCalls { get; private set; }

        public Task<IReadOnlyList<MetricCountDto>> GetSummaryAsync(
            bool includePlatformOnly,
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            CancellationToken cancellationToken = default)
        {
            SummaryCalls++;
            IReadOnlyList<MetricCountDto> metrics =
            [
                new("applications_pending", "Openstaande sollicitaties", period, 99),
                new("applications", "Sollicitaties", period, 9)
            ];
            return Task.FromResult(metrics);
        }

        public Task<IReadOnlyList<MetricDrilldownItemDto>> GetDrilldownAsync(
            string key,
            bool includePlatformOnly,
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            CancellationToken cancellationToken = default)
        {
            DrilldownCalls++;
            return Task.FromResult<IReadOnlyList<MetricDrilldownItemDto>>([]);
        }

        public Task<VacancyPerformanceBoardDto> GetVacancyPerformanceAsync(
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            int take = 3,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyPerformanceBoardDto(period, [], []));

        public Task<ClientPerformanceBoardDto> GetClientPerformanceAsync(
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ClientPerformanceBoardDto(period, []));
    }

    private sealed class PassthroughLiveOverlay : IDashboardLiveOverlay
    {
        public Task<IReadOnlyList<MetricCountDto>> OverlayMetricsAsync(
            IReadOnlyList<MetricCountDto> cached,
            bool includePlatformOnly,
            IReadOnlyCollection<Guid>? companyIds,
            string period,
            CancellationToken cancellationToken = default)
            => Task.FromResult(cached);

        public Task<ClientPerformanceBoardDto> OverlayClientsAsync(
            ClientPerformanceBoardDto cached,
            IReadOnlyCollection<Guid>? companyIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult(cached);

        public Task<SalesManagerDashboardDto> OverlaySalesAsync(
            SalesManagerDashboardDto cached,
            CancellationToken cancellationToken = default)
            => Task.FromResult(cached);

        public Task<AmbassadeurDashboardDto> OverlayAmbassadeurAsync(
            AmbassadeurDashboardDto cached,
            CancellationToken cancellationToken = default)
            => Task.FromResult(cached);
    }

    private sealed class MissingLedger : ICommissionLedgerService
    {
        public Task<decimal> GetBalanceExVatAsync(Guid salesManagerUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(0m);

        public Task<decimal> GetUninvoicedBalanceExVatAsync(Guid salesManagerUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(0m);

        public Task<IReadOnlyList<CommissionLedgerEntry>> ListEntriesAsync(
            Guid salesManagerUserId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CommissionLedgerEntry>>([]);

        public Task<CommissionLedgerEntry?> TryCreditFounderBonusAsync(
            Guid salesManagerUserId, Guid companyId, string paymentId, int? firstYearSlot, CancellationToken cancellationToken = default)
            => Task.FromResult<CommissionLedgerEntry?>(null);

        public Task<CommissionLedgerEntry?> TryCreditTokenCommissionAsync(
            Guid salesManagerUserId, Guid companyId, Guid tokenCheckoutId, decimal purchaseAmountEuro,
            DateTime? firstYearStartedAt, decimal? directRate = null, int? durationDays = null,
            decimal? year2Rate = null, decimal? year3Rate = null, CancellationToken cancellationToken = default)
            => Task.FromResult<CommissionLedgerEntry?>(null);

        public Task<CommissionLedgerEntry?> TryCreditIndirectTokenCommissionAsync(
            Guid referringSalesManagerUserId, Guid companyId, Guid tokenCheckoutId, decimal purchaseAmountEuro,
            DateTime? firstYearStartedAt, decimal? indirectRate = null, int? durationDays = null, CancellationToken cancellationToken = default)
            => Task.FromResult<CommissionLedgerEntry?>(null);

        public Task<CommissionLedgerEntry?> TryCreditAmbassadeurTokenCommissionAsync(
            Guid ambassadeurUserId, Guid companyId, Guid tokenCheckoutId, decimal purchaseAmountEuro,
            DateTime? firstYearStartedAt, decimal rate, int? durationDays = null, CancellationToken cancellationToken = default)
            => Task.FromResult<CommissionLedgerEntry?>(null);

        public Task AttachEntriesToInvoiceAsync(
            Guid invoiceId, IReadOnlyList<Guid> entryIds, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<CommissionLedgerEntry> RecordPayoutAsync(
            Guid salesManagerUserId, Guid invoiceId, decimal amountExVat, decimal vatAmount, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class MissingAmbassadeurSettings : IAmbassadeurSettingsService
    {
        public Task<AmbassadeurSettingsDto> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AmbassadeurSettingsDto(50, 1m, 15m, DateTime.UtcNow));

        public Task<AmbassadeurSettingsDto> UpdateAsync(
            AmbassadeurSettingsUpdateRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AmbassadeurSettingsDto(request.CandidateThreshold, request.PercentPerThreshold, request.MaxCommissionPercentage, DateTime.UtcNow));

        public Task SetCommissionOverrideAsync(
            Guid ambassadeurUserId,
            decimal? percentageOverride,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
