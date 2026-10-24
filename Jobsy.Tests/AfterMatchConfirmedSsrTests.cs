using System.Net;
using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchConfirmedSsrTests : BunitContext
{
    public AfterMatchConfirmedSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new()));
    }

    [Fact]
    public void Confirmed_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchConfirmedSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("AfterMatch.Screen05.PageTitle", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-confirmed.js", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_renders_scheduled_mockup_blocks()
    {
        var culture = Culture;
        var appId = AfterMatchDemoIds.ScheduledApp;
        var start = new DateTime(2026, 10, 22, 8, 30, 0, DateTimeKind.Utc);
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Scheduled,
            AfterMatchContactShareState.From(InterviewStatus.Scheduled, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            "9 min fietsen",
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            null,
            start,
            start.AddMinutes(30),
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-10));

        var data = new AfterMatchConfirmedSsrData.PageData(
            thread,
            "/candidate/applications?tab=berichten",
            "/candidate/applications/" + appId.ToString("D") + "/after-match?panel=reschedule&return=%2Fcandidate%2Fapplications%3Ftab%3Dberichten",
            "/candidate/applications/" + appId.ToString("D") + "/after-match?panel=cancel&return=%2Fcandidate%2Fapplications%3Ftab%3Dberichten",
            "https://maps.example",
            "/api/ndm-confirmed-client/calendar.ics?applicationId=" + appId.ToString("D"),
            "https://calendar.google.com/calendar/render?action=TEMPLATE",
            "https://outlook.office.com/calendar/action/compose",
            "/candidate/hoe-werkt-lobsy",
            "/candidate/hoe-werkt-lobsy",
            "Lead",
            "Donderdag 22 oktober, 10:30 tot 11:00",
            "Op locatie",
            "13 okt",
            "15 okt",
            "22 okt");

        var html = AfterMatchConfirmedSsrShells.BuildBody(culture, data, appId);

        Assert.Contains("data-ndm-confirmed-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("Je gesprek staat erin", html, StringComparison.Ordinal);
        Assert.Contains("Gesprek gepland", html, StringComparison.Ordinal);
        Assert.Contains("Zet in je agenda", html, StringComparison.Ordinal);
        Assert.Contains("Herinneringen", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-reminder=\"dayBefore18\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"s now\"", html, StringComparison.Ordinal);
        Assert.Contains("Verzetten", html, StringComparison.Ordinal);
        Assert.Contains("panel=cancel", html, StringComparison.Ordinal);
        Assert.Contains("ndm-cancel-panel", html, StringComparison.Ordinal);
        Assert.Contains("De dag ervoor, om 18:00", html, StringComparison.Ordinal);
        Assert.Contains("E-mail en melding", html, StringComparison.Ordinal);
        Assert.Contains("data-google-href=\"https://calendar.google.com/calendar/render?action=TEMPLATE\"", html, StringComparison.Ordinal);
        Assert.Contains("/candidate/hoe-werkt-lobsy", html, StringComparison.Ordinal);
        Assert.Contains("Route bekijken", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Middleware_routes_default_after_match_to_confirmed_ssr()
    {
        var root = FindRepoRoot();
        var middleware = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Middleware/AfterMatchPickSlotRouteRewriteMiddleware.cs"));
        Assert.Contains("/confirmed-ssr", middleware, StringComparison.Ordinal);
        Assert.Contains("\"messages\" or \"reschedule\" => \"/blazor\"", middleware, StringComparison.Ordinal);
        Assert.Contains("\"cancel\" => \"/confirmed-ssr\"", middleware, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_seo_catalog_uses_gesprek_gepland_for_confirmed_ssr()
    {
        var appId = AfterMatchDemoIds.ScheduledApp;
        var path = $"/candidate/applications/{appId:D}/after-match/confirmed-ssr";
        var entry = Jobsy.Web.Seo.PageSeoCatalog.Resolve(path);
        Assert.Equal("AfterMatch.Screen05.PageTitle", entry.TitleKey);
    }

    [Fact]
    public void Acc_demo_scheduled_application_id_is_documented()
    {
        Assert.Equal(Guid.Parse("77777771-0003-4000-8000-000000000003"), AfterMatchDemoIds.ScheduledApp);
        Assert.Equal(Guid.Parse("b0000001-0000-4000-8000-000000000035"), TestAccountsIds.SollicDemoAppScheduled);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Could not find repo root.");
    }
}
