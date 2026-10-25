using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchBevestigingSsrTests : BunitContext
{
    public AfterMatchBevestigingSsrTests()
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
    public void Bevestiging_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchBevestigingSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("bevestiging-ssr", razor, StringComparison.Ordinal);
        Assert.Contains("na-de-match.css?v=20261029-ndm-mail06", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_route_is_anonymous_no_blazor()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/__Preview/NaDeMatchAccPreviewScreens.razor"));
        Assert.Contains("AllowAnonymous", razor, StringComparison.Ordinal);
        Assert.Contains("/preview/na-de-match/{ScreenId}", razor, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_renders_mail_confirmation_mockup_blocks()
    {
        var culture = Culture;
        var demo = AfterMatchBevestigingSsrData.Demo();
        var html = AfterMatchBevestigingSsrShells.BuildBody(
            culture,
            demo,
            AfterMatchDemoIds.ScheduledApp,
            includeBackLink: false);

        Assert.Contains("data-ndm-bevestiging-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("Bevestiging voor allebei, met agenda-item", html, StringComparison.Ordinal);
        Assert.Contains("Mail aan kandidaat", html, StringComparison.Ordinal);
        Assert.Contains("Mail aan werkgever", html, StringComparison.Ordinal);
        Assert.Contains("Je gesprek staat erin, Sanne", html, StringComparison.Ordinal);
        Assert.Contains("Sanne komt donderdag", html, StringComparison.Ordinal);
        Assert.Contains("Zo ziet het agenda-item eruit in Outlook", html, StringComparison.Ordinal);
        Assert.Contains("Wat er hierna gebeurt", html, StringComparison.Ordinal);
        Assert.Contains("class=\"olk\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_seo_catalog_uses_bevestiging_title()
    {
        var appId = AfterMatchDemoIds.ScheduledApp;
        var path = $"/candidate/applications/{appId:D}/after-match/bevestiging-ssr";
        var entry = Jobsy.Web.Seo.PageSeoCatalog.Resolve(path);
        Assert.Equal("AfterMatch.Screen06.PageTitle", entry.TitleKey);
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
