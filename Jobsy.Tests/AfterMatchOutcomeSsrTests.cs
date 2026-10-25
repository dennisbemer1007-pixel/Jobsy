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
    public void Outcome_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchOutcomeSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("AfterMatch.Screen13.PageTitle", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-outcome.js", razor, StringComparison.Ordinal);
        Assert.Contains("na-de-match.css?v=20261027-ndm-outcome1", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_outcome_routes_to_outcome_ssr()
    {
        Assert.Equal("/outcome-ssr", AfterMatchPanelRouting.PathSuffixForPanel("outcome"));
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
            "/candidate/hoe-werkt-lobsy",
            "/candidate/hoe-werkt-lobsy",
            reason,
            "13 okt",
            "15 okt",
            "22 okt",
            "26 okt",
            []);

        var html = AfterMatchOutcomeSsrShells.BuildBody(culture, data, appId);

        Assert.Contains("data-ndm-outcome-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("ndm-hero--gesprek", html, StringComparison.Ordinal);
        Assert.Contains("Dank je wel voor het gesprek", html, StringComparison.Ordinal);
        Assert.Contains(WebUtility.HtmlEncode(reason), html, StringComparison.Ordinal);
        Assert.Contains("Wat nu?", html, StringComparison.Ordinal);
        Assert.Contains("Even iets kwijt?", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-outcome-chip", html, StringComparison.Ordinal);
        Assert.Contains("tl ndm-outcome-tl", html, StringComparison.Ordinal);
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
