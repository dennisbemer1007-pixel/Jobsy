using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchEmployerWatNuSsrTests : BunitContext
{
    public AfterMatchEmployerWatNuSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Employer_wat_nu_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchPanelSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-employer.js?v=20261025-ndm-wg-interact", razor, StringComparison.Ordinal);
        Assert.Contains("na-de-match.css?v=20261025-ndm-wg-interact", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Middleware_routes_werkgever_default_panel_to_panel_ssr()
    {
        var src = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Middleware/AfterMatchPickSlotRouteRewriteMiddleware.cs"));
        Assert.Contains("AfterMatchEmployerPanelRouting", src, StringComparison.Ordinal);
        Assert.Contains("werkgever/sollicitaties", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Employer_panel_routing_propose_goes_to_panel_ssr()
    {
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("propose"));
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel(""));
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("messages"));
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("outcome"));
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("cancel"));
    }

    [Fact]
    public void Shell_renders_wat_nu_mockup_blocks()
    {
        var culture = Culture;
        var appId = AfterMatchDemoIds.WaitApp;
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Proposing,
            AfterMatchContactShareState.From(InterviewStatus.Proposing, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            null,
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            null,
            null,
            null,
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            "sanne.demo@jobsy.local",
            DateTime.UtcNow.AddDays(-2),
            null);

        var baseHref = $"/werkgever/sollicitaties/{appId:D}/after-match";
        var data = new AfterMatchEmployerWatNuSsrShells.PageData(
            thread,
            $"{baseHref}?panel=propose",
            $"{baseHref}?panel=messages",
            $"/werkgever/sollicitaties/{appId:D}",
            null);

        var html = AfterMatchEmployerWatNuSsrShells.BuildBody(culture, data, appId);
        Assert.Contains("data-testid=\"ndm-werkgever-live\"", html, StringComparison.Ordinal);
        Assert.Contains("Sanne de Vries", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-open-panel=\"propose\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-open-panel=\"messages\"", html, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Employer.Share.Title"], html, StringComparison.Ordinal);
    }

    [Fact]
    public void Ssr_routing_only_proposing()
    {
        var proposing = new AfterMatchThreadDto(
            AfterMatchDemoIds.WaitApp,
            Guid.NewGuid(),
            InterviewStatus.Proposing,
            AfterMatchContactShareState.From(InterviewStatus.Proposing, null),
            null,
            "T",
            "C",
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
            "Sanne",
            null,
            null,
            null);

        Assert.True(AfterMatchEmployerSsrRouting.ShouldUseWatNuScreen(proposing));
        Assert.False(AfterMatchEmployerSsrRouting.ShouldUseWatNuScreen(
            proposing with { InterviewStatus = InterviewStatus.Scheduled }));
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
