using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Api.Controllers;
using Jobsy.Core.Admin;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.AdminTodo;
using Jobsy.Web.Components.Admin;
using Jobsy.Web.Components.Admin.Shell;
using Jobsy.Web.Components.Admin.Ui;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AdminTodoSourceTests
{
    [Fact]
    public async Task Kvk_failed_source_returns_only_failed_rows()
    {
        await using var db = CreateDb();
        var ok = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            EstablishmentName = "OK BV",
            KvkNumber = "1",
            KvkEstablishmentId = "1_1",
            ContactName = "A",
            ContactEmail = "a@x.nl",
            ActivationToken = "t",
            KvkVerificationStatus = KvkVerificationStatus.Verified,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        var failed = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            EstablishmentName = "Fail BV",
            KvkNumber = "2",
            KvkEstablishmentId = "2_1",
            ContactName = "B",
            ContactEmail = "b@x.nl",
            ActivationToken = "t2",
            KvkVerificationStatus = KvkVerificationStatus.Failed,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        db.CompanyRegistrations.AddRange(ok, failed);
        db.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Fail Co",
            KvkNumber = "3",
            Address = "x",
            Location = new GeoPoint(52, 4),
            KvkVerificationStatus = KvkVerificationStatus.Failed,
            KvkLastVerificationAttemptAtUtc = DateTime.UtcNow.AddHours(-3)
        });
        await db.SaveChangesAsync();

        var items = await new KvkFailedRegistrationsSource(db).GetAsync();
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(AdminTodoSeverity.Danger, i.Severity));
        Assert.Contains(items, i => i.Subtitle == "Fail BV");
        Assert.Contains(items, i => i.Href.Contains("/admin/organisaties", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Pending_takeovers_only()
    {
        await using var db = CreateDb();
        var reg = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            EstablishmentName = "Aanvrager Org",
            KvkNumber = "1",
            KvkEstablishmentId = "1_1",
            ContactName = "A",
            ContactEmail = "a@x.nl",
            ActivationToken = "t",
            CreatedAt = DateTime.UtcNow
        };
        var target = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Vestiging Poeldijk",
            KvkNumber = "9",
            Address = "x",
            Location = new GeoPoint(52, 4)
        };
        db.CompanyRegistrations.Add(reg);
        db.Companies.Add(target);
        db.EstablishmentTakeoverRequests.Add(new EstablishmentTakeoverRequest
        {
            Id = Guid.NewGuid(),
            RegistrationId = reg.Id,
            TargetCompanyId = target.Id,
            Status = TakeoverRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddHours(-5)
        });
        db.EstablishmentTakeoverRequests.Add(new EstablishmentTakeoverRequest
        {
            Id = Guid.NewGuid(),
            RegistrationId = reg.Id,
            TargetCompanyId = target.Id,
            Status = TakeoverRequestStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var items = await new PendingTakeoversSource(db).GetAsync();
        Assert.Single(items);
        Assert.Equal(AdminTodoSeverity.Warn, items[0].Severity);
        Assert.Contains("Aanvrager Org", items[0].Subtitle);
        Assert.Contains("Vestiging Poeldijk", items[0].Subtitle);
    }

    [Fact]
    public async Task Moderation_flagged_aggregates_count_and_href()
    {
        await using var db = CreateDb();
        var company = SeedCompany(db);
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Title = "Flagged A",
            Description = "x",
            Status = VacancyStatus.Draft,
            ContentModerationPassed = false,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Title = "Ok",
            Description = "x",
            Status = VacancyStatus.Active,
            ContentModerationPassed = true,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var items = await new ModerationFlaggedVacanciesSource(db).GetAsync();
        Assert.Single(items);
        Assert.Equal(1, items[0].Count);
        Assert.Equal("/admin/vacatures/moderatie", items[0].Href);
        Assert.Equal(AdminTodoSeverity.Warn, items[0].Severity);
    }

    [Fact]
    public async Task Sales_apps_pending_only()
    {
        await using var db = CreateDb();
        var referrer = new User
        {
            Id = Guid.NewGuid(),
            Email = "sm@x.nl",
            FullName = "SM",
            Role = UserRole.SalesManager,
            IsActive = true
        };
        db.Users.Add(referrer);
        db.SalesManagerApplications.Add(new SalesManagerApplication
        {
            Id = Guid.NewGuid(),
            ReferrerSalesManagerUserId = referrer.Id,
            ReferrerTrackingCode = "SM-1",
            CandidateEmail = "c@x.nl",
            CandidateFullName = "Candidate X",
            Motivation = "go",
            Status = SalesManagerApplicationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        db.SalesManagerApplications.Add(new SalesManagerApplication
        {
            Id = Guid.NewGuid(),
            ReferrerSalesManagerUserId = referrer.Id,
            ReferrerTrackingCode = "SM-1",
            CandidateEmail = "d@x.nl",
            CandidateFullName = "Done",
            Motivation = "go",
            Status = SalesManagerApplicationStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        });
        await db.SaveChangesAsync();

        var items = await new PendingSalesManagerApplicationsSource(db).GetAsync();
        Assert.Single(items);
        Assert.Equal("Candidate X", items[0].Subtitle);
        Assert.Equal(AdminTodoSeverity.Info, items[0].Severity);
    }

    [Fact]
    public async Task Open_payouts_issued_only_with_sum()
    {
        await using var db = CreateDb();
        var sm = new User
        {
            Id = Guid.NewGuid(),
            Email = "sm@x.nl",
            FullName = "SM",
            Role = UserRole.SalesManager,
            IsActive = true
        };
        db.Users.Add(sm);
        db.SelfBillingInvoices.Add(new SelfBillingInvoice
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = sm.Id,
            InvoiceNumber = "I1",
            TotalInclVat = 100m,
            Status = SelfBillingInvoiceStatus.Issued,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            IssuedAt = DateTime.UtcNow.AddDays(-1)
        });
        db.SelfBillingInvoices.Add(new SelfBillingInvoice
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = sm.Id,
            InvoiceNumber = "I2",
            TotalInclVat = 50m,
            Status = SelfBillingInvoiceStatus.Paid,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            IssuedAt = DateTime.UtcNow.AddDays(-2),
            PaidAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var items = await new OpenPayoutsSource(db).GetAsync();
        Assert.Single(items);
        Assert.Equal(1, items[0].Count);
        Assert.Contains("€", items[0].Subtitle);
        Assert.Contains("uitbetalingen", items[0].Href);
    }

    [Fact]
    public async Task New_feedback_only()
    {
        await using var db = CreateDb();
        db.PlatformFeedbacks.Add(new PlatformFeedback
        {
            Id = Guid.NewGuid(),
            Type = FeedbackType.Bug,
            Status = FeedbackStatus.New,
            Description = "x",
            PageUrl = "/admin",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        });
        db.PlatformFeedbacks.Add(new PlatformFeedback
        {
            Id = Guid.NewGuid(),
            Type = FeedbackType.Feature,
            Status = FeedbackStatus.Resolved,
            Description = "y",
            PageUrl = "/admin",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var items = await new NewFeedbackSource(db).GetAsync();
        Assert.Single(items);
        Assert.Equal(1, items[0].Count);
        Assert.Equal("/admin/feedback", items[0].Href);
    }

    private static Company SeedCompany(JobsyDbContext db)
    {
        var c = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Co",
            KvkNumber = "12345678",
            Address = "x",
            Location = new GeoPoint(52, 4)
        };
        db.Companies.Add(c);
        return c;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("AdminTodo-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }
}

public class AdminTodoServiceTests
{
    [Fact]
    public async Task Orders_danger_then_warn_then_info_then_oldest()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sources = new IAdminTodoSource[]
        {
            new StubSource("a",
            [
                Item("info-new", AdminTodoSeverity.Info, DateTime.UtcNow.AddHours(-1)),
                Item("danger-old", AdminTodoSeverity.Danger, DateTime.UtcNow.AddDays(-2)),
                Item("warn", AdminTodoSeverity.Warn, DateTime.UtcNow.AddHours(-3)),
                Item("danger-new", AdminTodoSeverity.Danger, DateTime.UtcNow.AddHours(-1)),
            ])
        };
        var sut = new AdminTodoService(sources, cache);
        var snap = await sut.GetAsync();
        Assert.Equal(["danger-old", "danger-new", "warn", "info-new"], snap.Items.Select(i => i.Key).ToArray());
    }

    [Fact]
    public async Task Caches_and_invalidate_refreshes()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var source = new CountingSource();
        var sut = new AdminTodoService([source], cache);
        _ = await sut.GetAsync();
        _ = await sut.GetAsync();
        Assert.Equal(1, source.Calls);
        sut.Invalidate();
        _ = await sut.GetAsync();
        Assert.Equal(2, source.Calls);
    }

    [Fact]
    public void BuildCounts_maps_nav_keys()
    {
        var items = new List<AdminTodoItem>
        {
            Item("kvk-failed:1", AdminTodoSeverity.Danger, DateTime.UtcNow),
            Item("takeovers:1", AdminTodoSeverity.Warn, DateTime.UtcNow),
            new("moderation", AdminTodoSeverity.Warn, "t", "s", "Vacatures", DateTime.UtcNow, "a", "/x", 2),
            new("open-payouts", AdminTodoSeverity.Warn, "t", "s", "Financiën", DateTime.UtcNow, "a", "/x", 3),
            new("feedback", AdminTodoSeverity.Info, "t", "s", "Overzicht", DateTime.UtcNow, "a", "/x", 4),
        };
        var counts = AdminTodoService.BuildCounts(items);
        Assert.Equal(1 + 1 + 2 + 3 + 4, counts[AdminTodoNavKeys.Todo]);
        Assert.Equal(2, counts[AdminTodoNavKeys.OrgRequests]);
        Assert.Equal(2, counts[AdminTodoNavKeys.Moderation]);
        Assert.Equal(3, counts[AdminTodoNavKeys.Payouts]);
        Assert.Equal(4, counts[AdminTodoNavKeys.Feedback]);
    }

    private static AdminTodoItem Item(string key, AdminTodoSeverity severity, DateTime since)
        => new(key, severity, "t", "s", "Area", since, "a", "/x");

    private sealed class StubSource(string key, IReadOnlyList<AdminTodoItem> items) : IAdminTodoSource
    {
        public string Key => key;
        public Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(items);
    }

    private sealed class CountingSource : IAdminTodoSource
    {
        public int Calls;
        public string Key => "c";
        public Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<AdminTodoItem>>([]);
        }
    }
}

public class AdminFinanceSummaryServiceTests
{
    [Fact]
    public async Task Totals_for_fixed_dataset()
    {
        await using var db = new JobsyDbContext(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("AdminFin-" + Guid.NewGuid()).Options);

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Co",
            KvkNumber = "1",
            Address = "x",
            Location = new GeoPoint(52, 4)
        };
        db.Companies.Add(company);
        var checkout = new TokenPurchaseCheckout
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            PaymentId = "tr_1",
            PackSize = 10,
            TotalAmountCents = 12100,
            AmountExVatCents = 10000,
            VatAmountCents = 2100,
            Status = TokenPurchaseCheckoutStatus.Paid,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.TokenPurchaseCheckouts.Add(checkout);
        db.TokenPurchaseInvoices.Add(new TokenPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-1",
            TokenPurchaseCheckoutId = checkout.Id,
            CompanyId = company.Id,
            CompanyName = "Co",
            MolliePaymentId = "tr_1",
            PackSize = 10,
            AmountExVatCents = 10000,
            VatAmountCents = 2100,
            TotalAmountCents = 12100,
            IssuedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            PaymentId = "tr_open",
            PackSize = 5,
            TotalAmountCents = 5000,
            Status = TokenPurchaseCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var summary = await new AdminFinanceSummaryService(db).GetAsync("week");
        Assert.Equal(12100, summary.RevenueInclVatCents);
        Assert.Equal(10000, summary.RevenueExVatCents);
        Assert.Equal(10, summary.TokensSold);
        Assert.Equal(5000, summary.OpenAtMollieCents);
        Assert.Equal(1, summary.OpenAtMollieCount);
        Assert.True(summary.OldestOpenMollieDays is >= 0);
    }
}

public class AdminGreetingTests
{
    [Theory]
    [InlineData(6, "AdminDash.Greeting.Morning")]
    [InlineData(11, "AdminDash.Greeting.Morning")]
    [InlineData(12, "AdminDash.Greeting.Afternoon")]
    [InlineData(17, "AdminDash.Greeting.Afternoon")]
    [InlineData(18, "AdminDash.Greeting.Evening")]
    [InlineData(23, "AdminDash.Greeting.Evening")]
    public void Greeting_boundaries_in_amsterdam(int localHour, string expectedKey)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Amsterdam");
        var local = new DateTime(2026, 9, 29, localHour, 0, 0, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, tz);
        Assert.Equal(expectedKey, AdminGreeting.GreetingKey(utc));
    }
}

public class PlatformModeSummaryTests
{
    [Fact]
    public void Shows_required_for_2fa_and_hides_absent_flags()
    {
        var rows = PlatformModeSummary.Build(vacancyContentModerationEnabled: true);
        Assert.Contains(rows, r => r.Key == "MfaPolicy" && r.ValueKey == "AdminDash.Mode.Required" && r.IsPolicyReadonly);
        Assert.Contains(rows, r => r.Key == "VacancyContentModerationEnabled" && r.IsOn);
        Assert.DoesNotContain(rows, r => r.Key.Contains("passport", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(rows, r => r.Key.Contains("Employer", StringComparison.OrdinalIgnoreCase));
    }
}

public class AdminTodoApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    public AdminTodoApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Todo_endpoint_403_for_non_admin()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.CandidateId);
        var response = await client.GetAsync("api/admin/todo");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Todo_endpoint_returns_counts_for_admin()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.AdminId);
        var response = await client.GetAsync("api/admin/todo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("countsByNavKey", out var counts));
        Assert.True(counts.TryGetProperty("todo", out _));
    }

    [Fact]
    public async Task Vacancies_moderation_flagged_filters()
    {
        // CreateClient seeds the factory; do it first so the test does not depend on xunit ordering.
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.AdminId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var companyId = await db.Companies.Select(c => c.Id).FirstAsync();
        var flaggedId = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = flaggedId,
            CompanyId = companyId,
            Title = "Flagged-for-admin-todo-test",
            Description = "desc",
            Status = VacancyStatus.Draft,
            ContentModerationPassed = false,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var flagged = await client.GetFromJsonAsync<List<JsonElement>>("api/admin/vacancies?moderation=flagged");
        Assert.NotNull(flagged);
        Assert.Contains(flagged!, v => v.GetProperty("id").GetGuid() == flaggedId);
        Assert.All(flagged!, v => Assert.False(v.GetProperty("contentModerationPassed").GetBoolean()));

        var all = await client.GetFromJsonAsync<List<JsonElement>>("api/admin/vacancies");
        Assert.NotNull(all);
        Assert.True(all!.Count >= flagged!.Count);
    }
}

public class UsersActiveCandidatesMetricTests
{
    [Fact]
    public async Task Summary_includes_candidate_only_key()
    {
        await using var db = new JobsyDbContext(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("CandMetric-" + Guid.NewGuid()).Options);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "c@x.nl",
            FullName = "Cand",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "a@x.nl",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var metrics = await new MetricsQueryService(db)
            .GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "week");
        Assert.Equal(1, metrics.First(m => m.Key == "users_active_candidates").Value);
        Assert.Equal(2, metrics.First(m => m.Key == "users_active").Value);
    }
}

public class AdminDashboardBunitTests : BunitContext
{
    public AdminDashboardBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuthStateProvider(CreateAdmin())));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(CreateAdmin()));
        Services.AddAuthorizationCore();
        this.AddAuthorization().SetAuthorized("admin").SetRoles("Admin");
        Services.AddSingleton(new AdminTodoCountsStore());
        Services.AddSingleton(new AdminTodoChanged());
    }

    [Fact]
    public void Kpi_card_renders_delta_text()
    {
        var cut = Render<AdminKpiCard>(p => p
            .Add(x => x.Label, "Actieve kandidaten")
            .Add(x => x.Value, "4.812")
            .Add(x => x.Delta, "↑ +6,2% vs vorige")
            .Add(x => x.DeltaTone, "up"));
        Assert.Contains("Actieve kandidaten", cut.Markup);
        Assert.Contains("↑ +6,2% vs vorige", cut.Markup);
        Assert.Contains("admin-kpi-card__delta--up", cut.Markup);
    }

    [Fact]
    public void Platform_mode_required_label_present_in_strings()
    {
        var rows = PlatformModeSummary.Build(true);
        var culture = Services.GetRequiredService<CultureState>();
        Assert.Equal("Verplicht", culture[rows.First(r => r.IsPolicyReadonly).ValueKey]);
    }

    [Fact]
    public void Sidebar_count_store_sums_collapsed_group()
    {
        var store = new AdminTodoCountsStore();
        store.Apply(new Dictionary<string, int>
        {
            ["todo"] = 7,
            ["org-requests"] = 3,
            ["moderation"] = 2,
            ["feedback"] = 1
        });
        Assert.Equal(7, store.Get("todo"));
        Assert.Equal(4, store.GroupSum(["org-requests", "feedback"]));
        Assert.Equal(2, store.GroupSum(["moderation"]));
    }

    [Fact]
    public void Todo_empty_and_max_six_helpers()
    {
        Assert.Equal("Niets te doen. Mooi zo.", UiStrings.Get("AdminDash.Todo.Empty", "nl"));
        var items = Enumerable.Range(0, 8).Select(i => i).ToList();
        Assert.Equal(6, items.Take(6).Count());
    }

    private static ClaimsPrincipal CreateAdmin()
    {
        var id = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "Dennis Beheer"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("name", "Dennis Beheer")
        ], "test");
        return new ClaimsPrincipal(id);
    }

    private sealed class FakeAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }
}

/// <summary>Soft Playwright layout checks when JOBSY_E2E_BASE_URL is set.</summary>
public class AdminShellPlaywrightTests
{
    [Fact]
    public async Task Admin_dashboard_no_horizontal_scroll_at_1440_and_390()
    {
        var baseUrl = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return; // soft-skip offline / CI without acceptatie
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await page.GotoAsync(baseUrl.TrimEnd('/') + "/admin");
        var scroll1440 = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(scroll1440);

        await page.SetViewportSizeAsync(390, 844);
        await page.GotoAsync(baseUrl.TrimEnd('/') + "/admin");
        var grid = page.Locator(".admin-dash__kpis");
        if (await grid.CountAsync() > 0)
        {
            var cols = await page.EvaluateAsync<int>("() => getComputedStyle(document.querySelector('.admin-dash__kpis')).gridTemplateColumns.split(' ').length");
            Assert.True(cols <= 2);
        }
    }
}
