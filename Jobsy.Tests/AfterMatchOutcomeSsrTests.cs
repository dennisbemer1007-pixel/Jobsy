using System.Net;
using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchOutcomeSsrTests : BunitContext
{
    public AfterMatchOutcomeSsrTests()
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
    public void Panel_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchPanelSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("AfterMatch.Screen13.PageTitle", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-outcome.js", razor, StringComparison.Ordinal);
        Assert.Contains("na-de-match.css?v=20261028-ndm-outcome6", razor, StringComparison.Ordinal);
        var ndmCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/na-de-match.css"));
        Assert.Contains(".app-shell--candidate:has(.ndm-outcome-page) .app-main", ndmCss, StringComparison.Ordinal);
        Assert.Contains("padding-bottom:calc(var(--bottom-nav-h,4.25rem)", ndmCss, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_outcome_and_default_route_to_panel_ssr()
    {
        Assert.Equal("/panel-ssr", AfterMatchPanelRouting.PathSuffixForPanel("outcome"));
        Assert.Equal("/panel-ssr", AfterMatchPanelRouting.PathSuffixForPanel(""));
    }

    [Fact]
    public void Outcome_screen_routing_accepts_done_interview_without_not_now_enum()
    {
        var thread = new AfterMatchThreadDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InterviewStatus.Done,
            AfterMatchContactShareState.From(InterviewStatus.Done, null),
            null,
            "Barista",
            "Testbedrijf",
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            null,
            null,
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "",
            null,
            null,
            null);
        Assert.True(AfterMatchSsrRouting.ShouldUseOutcomeScreen(thread));
    }

    [Fact]
    public void Shell_renders_not_now_mockup_blocks()
    {
        var culture = Culture;
        var appId = AfterMatchDemoIds.NotNowApp;
        var start = new DateTime(2026, 10, 22, 8, 30, 0, DateTimeKind.Utc);
        var reason = "Je was heel zorgvuldig en vriendelijk.";
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Done,
            AfterMatchContactShareState.From(InterviewStatus.Done, AfterMatchOutcomeKind.NotNow),
            AfterMatchOutcomeKind.NotNow,
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
            start.AddDays(-9),
            reason);

        var data = new AfterMatchOutcomeSsrData.PageData(
            thread,
            "/candidate/applications?tab=berichten",
            "/candidate/applications?tab=activiteiten",
            "/banenkaart",
            "/carriere",
            "/candidate/hoe-werkt-lobsy",
            reason,
            "13 okt",
            "15 okt",
            "22 okt",
            "26 okt",
            []);

        var html = AfterMatchOutcomeSsrShells.BuildBody(culture, data, appId);

        Assert.Contains("data-ndm-outcome-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ndm-hero--gesprek", html, StringComparison.Ordinal);
        Assert.Contains("<section class=\"intro\">", html, StringComparison.Ordinal);
        Assert.Contains("Dank je wel voor het gesprek", html, StringComparison.Ordinal);
        Assert.Contains(WebUtility.HtmlEncode(reason), html, StringComparison.Ordinal);
        Assert.Contains("Wat nu?", html, StringComparison.Ordinal);
        Assert.Contains("Even iets kwijt?", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-outcome-chip", html, StringComparison.Ordinal);
        Assert.Contains("tl ndm-outcome-tl", html, StringComparison.Ordinal);
        Assert.Contains("Leren en ontwikkelen", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/carriere\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hide-m", html, StringComparison.Ordinal);
        Assert.Contains("ndm-mockup-footer", html, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
