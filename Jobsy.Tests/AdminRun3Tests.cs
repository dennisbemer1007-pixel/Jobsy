using System.Net;
using System.Text.Json;
using Bunit;
using Jobsy.Api;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Admin;
using Jobsy.Web.Components.Admin.Ui;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AdminOpenLinkTests
{
    [Fact]
    public void Same_open_url_does_not_navigate_again()
    {
        const string current = "https://acc.lobsy.nl/admin/gebruikers?open=11111111-1111-1111-1111-111111111111";
        const string target = "/admin/gebruikers?open=11111111-1111-1111-1111-111111111111";
        Assert.False(AdminOpenLink.ShouldNavigate(current, target, interactive: true));
    }

    [Fact]
    public void Different_open_url_navigates_only_when_interactive()
    {
        const string current = "https://acc.lobsy.nl/admin/gebruikers";
        const string target = "/admin/gebruikers?open=11111111-1111-1111-1111-111111111111";
        Assert.True(AdminOpenLink.ShouldNavigate(current, target, interactive: true));
        Assert.False(AdminOpenLink.ShouldNavigate(current, target, interactive: false));
    }

    [Fact]
    public void Missing_target_never_navigates()
    {
        Assert.False(AdminOpenLink.ShouldNavigate("/admin/gebruikers?open=missing", "/admin/gebruikers?open=missing", true));
        Assert.False(AdminOpenLink.ShouldNavigate("/admin/gebruikers", "", true));
    }
}

public class AdminFilterFastTypeTests : BunitContext
{
    public AdminFilterFastTypeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new AuthStub()));
        Services.AddAuthorizationCore();
    }

    [Fact]
    public async Task Fifteen_characters_at_50ms_stay_in_the_input_and_win_the_query()
    {
        var cut = Render<FastSearchHost>();
        var input = cut.Find("input[type=search]");
        await input.TriggerEventAsync("onfocus", new FocusEventArgs());

        const string typed = "kandidaatxyzabc";
        Assert.Equal(15, typed.Length);
        var accumulated = "";
        foreach (var ch in typed)
        {
            accumulated += ch;
            await input.InputAsync(accumulated);
            await Task.Delay(50);
        }

        Assert.Equal(typed, cut.Find("input[type=search]").GetAttribute("value"));
        cut.WaitForAssertion(() => Assert.Equal(typed, cut.Find("#applied").TextContent), TimeSpan.FromSeconds(2));
    }

    private sealed class AuthStub : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    private sealed class FastSearchHost : ComponentBase, IDisposable
    {
        private string _query = "";
        private string _applied = "";
        private readonly DebouncedAction _debounce = new();

        public void Dispose() => _debounce.Dispose();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<AdminFilterBar>(0);
            builder.AddComponentParameter(1, nameof(AdminFilterBar.Query), _query);
            builder.AddComponentParameter(2, nameof(AdminFilterBar.QueryChanged), EventCallback.Factory.Create<string>(this, OnQuery));
            builder.CloseComponent();
            builder.OpenElement(3, "output");
            builder.AddAttribute(4, "id", "applied");
            builder.AddContent(5, _applied);
            builder.CloseElement();
        }

        private Task OnQuery(string q)
        {
            // Same shape as the users page: return the debounce task, and publish a stale query
            // until the quiet period ends. The input must not be overwritten while focused.
            return _debounce.RunAsync(80, async ct =>
            {
                await Task.Delay(10, ct);
                _query = q;
                _applied = q;
                await InvokeAsync(StateHasChanged);
            });
        }
    }
}

public class AdminRun3LabelTests
{
    [Fact]
    public void Audit_verbs_name_the_operation()
    {
        string Text(string key) => key;
        Assert.Equal("AdminAudit.Action.CategoryCreated", AdminActionLabels.Label("vacancy-category.create", Text));
        Assert.Equal("AdminAudit.Action.CategoryDeleted", AdminActionLabels.Label("vacancy-category.delete", Text));
        Assert.Equal("AdminAudit.Action.MasterdataUpdated", AdminActionLabels.Label("masterdata.update", Text));
        Assert.Equal("AdminAudit.Action.Flyer", AdminActionLabels.Label("settings.flyer.update", Text));
        Assert.Equal("AdminAction.Resource.PlatformLogs", AdminActionLabels.Resource("platformLogs", Text));
        Assert.Equal("Overgeslagen: geen IBAN", AdminActionLabels.VatStatus("SkippedNoIban", _ => "Overgeslagen: geen IBAN"));
        Assert.Equal("vestiging+", AdminActionLabels.Scope("branch+"));
    }

    [Fact]
    public void October_export_uses_the_month_in_the_filename()
    {
        Assert.Equal("token-aankopen-2026-10.csv", TokenExportFileNames.Purchases(2026, 4, 10));
        Assert.Equal("token-aankopen-2026-Q4.csv", TokenExportFileNames.Purchases(2026, 4, null));
    }

    [Fact]
    public void Flyer_defaults_match_the_saved_service_defaults()
    {
        var form = MarketingFlyerDefaults.CreateForm();
        Assert.Equal(MarketingFlyerSettingsService.DefaultHeadline, form.Headline);
        Assert.Equal(MarketingFlyerSettingsService.DefaultSubheadline, form.Subheadline);
        Assert.Equal(MarketingFlyerSettingsService.DefaultIntro, form.Intro);
        Assert.Equal(MarketingFlyerSettingsService.DefaultBulletPoints, form.BulletPoints);
        Assert.Equal(MarketingFlyerSettingsService.DefaultQrPath, form.QrPath);
        Assert.Null(form.UpdatedAtUtc);
    }

    [Fact]
    public void Dashboard_is_not_active_on_a_child_admin_route()
    {
        var dashboard = AdminNav.Groups.SelectMany(g => g.Items).Single(i => i.Href == "/admin");
        var companies = AdminNav.Groups.SelectMany(g => g.Items).Single(i => i.Href == "/admin/organisaties");
        Assert.False(AdminNav.Matches("/admin/organisaties", dashboard));
        Assert.True(AdminNav.Matches("/admin", dashboard));
        Assert.True(AdminNav.Matches("/admin/organisaties", companies));
        var requests = AdminNav.Groups.SelectMany(g => g.Items).Single(i => i.Href == "/admin/organisaties/aanvragen");
        Assert.False(AdminNav.Matches("/admin/organisaties/aanvragen", companies));
        Assert.True(AdminNav.Matches("/admin/organisaties/aanvragen", requests));
    }

    [Fact]
    public void Midnight_utc_stays_on_the_amsterdam_calendar_day()
    {
        var utc = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Utc);
        var local = AmsterdamTime.ToLocal(utc);
        Assert.Equal(10, local.Month);
        Assert.Equal(1, local.Day);
    }

    [Fact]
    public void Support_code_is_split_out_of_the_message()
    {
        Assert.Equal("LB-7Q3K", SupportCodeDisplay.Find("LB-7Q3K InvalidOperationException"));
        Assert.Null(SupportCodeDisplay.Find("geen code"));
    }
}

public class PlatformErrorLogOn5xxTests
{
    [Fact]
    public async Task Unhandled_exception_writes_the_support_code()
    {
        var sink = new CapturingLog();
        var services = new ServiceCollection();
        services.AddSingleton<IPlatformErrorLog>(sink);
        var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/boom";

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("secret boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            new ProductionEnv());

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("Api", entry.Category);
        Assert.StartsWith("LB-", entry.SupportCode, StringComparison.Ordinal);
        Assert.Contains("secret boom", entry.Detail, StringComparison.Ordinal);
        Assert.Contains("/api/boom", entry.Detail, StringComparison.Ordinal);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(entry.SupportCode, doc.RootElement.GetProperty("supportCode").GetString());

        var circuit = File.ReadAllText(Path.Combine(FindWebRoot(), "Hosting/CircuitExceptionLogger.cs"));
        Assert.Contains("api/platform-logs/errors", circuit, StringComparison.Ordinal);
        Assert.Contains("supportCode", circuit, StringComparison.Ordinal);
    }

    private static string FindWebRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("repo"), "Jobsy.Web");
    }

    private sealed class CapturingLog : IPlatformErrorLog
    {
        public List<(string Category, string Message, string? SupportCode, string? Detail)> Entries { get; } = [];

        public Task WriteAsync(string category, string message, string? supportCode, string? detail, CancellationToken cancellationToken = default)
        {
            Entries.Add((category, message, supportCode, detail));
            return Task.CompletedTask;
        }
    }

    private sealed class ProductionEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public class AdminTableLabelSourceTests
{
    [Fact]
    public void Listed_admin_tables_label_every_cell()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            "Jobsy.Web/Components/Pages/Admin/CompaniesAdmin.razor",
            "Jobsy.Web/Components/Admin/Sections/VacanciesAdminSection.razor",
            "Jobsy.Web/Components/Pages/Admin/TodoAdmin.razor",
            "Jobsy.Web/Components/Pages/Admin/RolesAdmin.razor",
            "Jobsy.Web/Components/Admin/Sections/UsersAdminSection.razor",
            "Jobsy.Web/Components/Pages/Admin/Scholen/ScholenList.razor",
            "Jobsy.Web/Components/Pages/Admin/MfaSessionsAdmin.razor",
            "Jobsy.Web/Components/Pages/Admin/PrivacyAdmin.razor",
            "Jobsy.Web/Components/Pages/Admin/FinanceAdmin.razor",
            "Jobsy.Web/Components/Pages/Admin/TokenFinanceAdmin.razor",
            "Jobsy.Web/Components/Admin/Sections/FinancePayoutsSection.razor",
            "Jobsy.Web/Components/Admin/Sections/PricingWhatDrivesSection.razor"
        };

        var missing = new List<string>();
        foreach (var relative in files)
        {
            var lines = File.ReadAllLines(Path.Combine(root, relative));
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("<td", StringComparison.Ordinal) && !line.Contains("data-label", StringComparison.Ordinal))
                {
                    missing.Add($"{relative}:{i + 1}");
                }
            }
        }

        Assert.True(missing.Count == 0, "Cells without data-label: " + string.Join(", ", missing));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root");
    }
}
