using System.Security.Claims;
using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Ops;
using Jobsy.Core.Scholen;
using Jobsy.Web.Auth;
using Jobsy.Web.Components.Pages.Leraar;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AcceptatieLayoutSweepTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private static string Read(params string[] parts)
        => File.ReadAllText(Path.Combine([RepoRoot(), .. parts]));

    [Fact]
    public void Dutch_culture_literals_in_razor_resolve()
    {
        var root = Path.Combine(RepoRoot(), "Jobsy.Web", "Components");
        var rx = new Regex("""Culture\[\s*"([A-Za-z0-9_.]+)"\s*\]""", RegexOptions.Compiled);
        var missing = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in rx.Matches(text))
            {
                var key = match.Groups[1].Value;
                if (string.Equals(UiStrings.Get(key, "nl"), key, StringComparison.Ordinal))
                {
                    missing.Add($"{key} ({Path.GetFileName(file)})");
                }
            }
        }

        Assert.Empty(missing);
        Assert.NotEqual("AdminUsers.Role.SchoolAdmin", UiStrings.Get("AdminUsers.Role.SchoolAdmin", "nl"));
        Assert.NotEqual("AdminUsers.Role.Teacher", UiStrings.Get("AdminUsers.Role.Teacher", "nl"));
        Assert.NotEqual("AdminUsers.RoleDesc.SchoolAdmin", UiStrings.Get("AdminUsers.RoleDesc.SchoolAdmin", "nl"));
        Assert.NotEqual("AdminUsers.RoleDesc.Teacher", UiStrings.Get("AdminUsers.RoleDesc.Teacher", "nl"));
        Assert.NotEqual("Discovery.Test.ValuesScan.Title", UiStrings.Get("Discovery.Test.ValuesScan.Title", "nl"));
        Assert.NotEqual("Discovery.Test.CultureScan.Title", UiStrings.Get("Discovery.Test.CultureScan.Title", "nl"));
        Assert.NotEqual("AdminScholen.Report.LoadFailed", UiStrings.Get("AdminScholen.Report.LoadFailed", "nl"));
    }

    [Fact]
    public void Public_stats_skip_test_accounts_and_admins()
    {
        Assert.False(PublicStatsExclusion.ShouldSkip(null, new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.False(PublicStatsExclusion.ShouldSkip(
            new User { Role = UserRole.Candidate },
            new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.True(PublicStatsExclusion.ShouldSkip(new User { IsTestAccount = true, Role = UserRole.Candidate }, null));
        Assert.True(PublicStatsExclusion.ShouldSkip(new User { Role = UserRole.Admin }, null));
        var admin = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, nameof(UserRole.Admin))], "test"));
        Assert.True(PublicStatsExclusion.ShouldSkip(null, admin));
    }

    [Fact]
    public void Ambassadors_paused_login_maps_to_the_parked_message()
    {
        Assert.Equal(LoginState.AmbassadorsPaused, LoginStateMapping.FromQuery("ambassadors-paused", false));
        Assert.Equal(LoginState.Invalid, LoginStateMapping.FromQuery("invalid", false));
        var block = Read("Jobsy.Web", "Components", "Auth", "LoginStatusBlock.razor");
        Assert.Contains("Sales.Ambassadors.ParkedLogin", block, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_classes_use_the_current_school_year_and_valid_levels()
    {
        Assert.Equal(2026, SchoolYear.Current(new DateOnly(2026, 10, 3)));
        Assert.Equal(2025, SchoolYear.Current(new DateOnly(2026, 1, 15)));
        Assert.Null(SchoolLevelRules.ValidateYear(SchoolLevel.VmboGt, 1));
        Assert.Null(SchoolLevelRules.ValidateYear(SchoolLevel.Groep78, 7));
        var seed = Read("Jobsy.Infrastructure", "Ops", "TestAccountsSeedService.cs");
        Assert.Contains("SchoolYear.Current(", seed, StringComparison.Ordinal);
        Assert.DoesNotContain("SchoolYearStart = DateTime.UtcNow.Year", seed, StringComparison.Ordinal);
    }

    [Fact]
    public void Journey_mobile_grid_override_follows_the_base_rule()
    {
        var css = Read("Jobsy.Web", "wwwroot", "css", "features", "ontdekkingsreis.css");
        var baseIdx = css.IndexOf("grid-template-columns: 300px minmax(0, 640px) 1fr", StringComparison.Ordinal);
        Assert.True(baseIdx > 0);
        var after = css[(baseIdx + 1)..];
        Assert.Contains("grid-template-columns: minmax(0, 1fr)", after, StringComparison.Ordinal);
        Assert.Contains("width: 100%", after, StringComparison.Ordinal);
    }

    [Fact]
    public void Service_worker_precache_matches_app_razor()
    {
        var app = Read("Jobsy.Web", "Components", "App.razor");
        foreach (var name in new[] { "service-worker.js", "service-worker.published.js" })
        {
            var text = Read("Jobsy.Web", "wwwroot", name);
            foreach (Match match in Regex.Matches(text, """/(?:css|js)/[^"' ]+\?v=[^"' ]+"""))
            {
                var url = match.Value.TrimStart('/');
                Assert.Contains(url, app, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Gratis_dna_preview_has_no_inline_script_and_can_start()
    {
        var razor = Read("Jobsy.Web", "Components", "Pages", "Public", "GratisDna.razor");
        Assert.DoesNotContain("<script", razor, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CanStart => _age16Plus == true", razor, StringComparison.Ordinal);
        var header = Read("Jobsy.Web", "Components", "Layout", "Public", "PublicHeader.razor");
        Assert.Contains("PublicHeaderAccount", header, StringComparison.Ordinal);
    }

    [Fact]
    public void Header_menus_use_one_attribute_and_keep_triggers_above_the_backdrop()
    {
        var css = Read("Jobsy.Web", "wwwroot", "css", "app.css");
        Assert.Contains("html[data-header-menu", css, StringComparison.Ordinal);
        Assert.DoesNotContain(":focus-within:not(.is-closed)", css, StringComparison.Ordinal);
        Assert.Contains(".header-dropdown-backdrop", css, StringComparison.Ordinal);
        Assert.Contains("z-index: 55", css, StringComparison.Ordinal);
        Assert.Contains("z-index: 70", css, StringComparison.Ordinal);
        var js = Read("Jobsy.Web", "wwwroot", "js", "app-core.js");
        Assert.Contains("data-header-menu", js, StringComparison.Ordinal);
        Assert.Contains("if (!next) return;", js, StringComparison.Ordinal);
        var min = Read("Jobsy.Web", "wwwroot", "css", "app.min.css");
        Assert.DoesNotContain(":focus-within:not(.is-closed)", min, StringComparison.Ordinal);
    }

    [Fact]
    public void Schools_public_page_replaces_the_dead_link()
    {
        var page = Read("Jobsy.Web", "Components", "Pages", "ScholenPublic.razor");
        Assert.Contains("@page \"/scholen\"", page, StringComparison.Ordinal);
        Assert.Contains("Schools.Public.LoginCta", page, StringComparison.Ordinal);
        Assert.Contains("HowLobsy.Schools.Lead", page, StringComparison.Ordinal);
    }
}

public class LeraarDashboardBunitTests : BunitContext
{
    public LeraarDashboardBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IFeatureFlags>(new FixedFeatureFlags(employersEnabled: false, schoolsEnabled: true));
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuth()));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddAuthorizationCore();
        this.AddAuthorization().SetAuthorized("leraar").SetRoles("Teacher");
    }

    [Fact]
    public void Schools_on_renders_the_empty_class_state()
    {
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new EmptyClassesHandler())
        {
            BaseAddress = new Uri("http://jobsy.test/")
        }));
        var cut = Render<LeraarDashboard>();
        cut.WaitForAssertion(() =>
            Assert.Contains("Je hebt nog geen klas", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Schools_on_shows_a_load_error_when_the_class_list_fails()
    {
        Services.AddSingleton(new JobsyApiClient(new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:9"),
            Timeout = TimeSpan.FromSeconds(2)
        }));
        var cut = Render<LeraarDashboard>();
        cut.WaitForAssertion(() =>
            Assert.Contains("De klasgegevens konden niet geladen worden", cut.Markup, StringComparison.Ordinal));
    }

    private sealed class EmptyClassesHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            });
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                new Claim(ClaimTypes.Role, "Teacher"),
                new Claim(ClaimTypes.Name, "Leraar")
            ], "test"))));
    }
}
