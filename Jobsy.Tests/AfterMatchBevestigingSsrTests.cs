using Bunit;
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
        Assert.Contains("na-de-match.css?v=20261030-ndm-scroll1", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-mail-chrome.js", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_renders_mail_confirmation_mockup_blocks()
    {
        var culture = Culture;
        var demo = AfterMatchBevestigingSsrData.Demo(culture);
        var html = AfterMatchBevestigingSsrShells.BuildBody(
            culture,
            demo,
            AfterMatchDemoIds.ScheduledApp,
            includeBackLink: false);

        Assert.Contains("data-ndm-bevestiging-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("Voorbeelden van de bevestiging", html, StringComparison.Ordinal);
        Assert.Contains(">Kandidaat<", html, StringComparison.Ordinal);
        Assert.Contains(">Werkgever<", html, StringComparison.Ordinal);
        Assert.Contains("Donderdag 22 oktober, 10:30 tot 11:00", html, StringComparison.Ordinal);
        Assert.Contains("Linda de Wit, eigenaar", html, StringComparison.Ordinal);
        Assert.Contains("9 min fietsen of 18 min met tram 2", html, StringComparison.Ordinal);
        Assert.Contains("Op locatie, Loosduinseweg 120", html, StringComparison.Ordinal);
        Assert.Contains("Zet in je agenda", html, StringComparison.Ordinal);
        Assert.Contains("class=\"mfoot\"", html, StringComparison.Ordinal);
        Assert.Contains("Tips voor je gesprek", html, StringComparison.Ordinal);
        Assert.Contains("Gesprek openen in Lobsy", html, StringComparison.Ordinal);
        Assert.Contains(">Locatie<", html, StringComparison.Ordinal);
        Assert.Contains("2571 AH Den Haag", html, StringComparison.Ordinal);
        Assert.Contains("lobsy.nl/g/7KX2", html, StringComparison.Ordinal);
        Assert.Contains("Je telefoonnummer deel je pas na het gesprek", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Zo blijft het veilig", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-ask-lobsy", html, StringComparison.Ordinal);
        Assert.Contains("Vraag het Lobsy", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Na_de_match_css_has_desktop_bottom_nav_clearance_for_all_ndm_pages()
    {
        var root = FindRepoRoot();
        var ndmCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/na-de-match.css"));
        Assert.Contains(".app-shell--candidate:has(.ndm-page) .app-main", ndmCss, StringComparison.Ordinal);
        Assert.Contains(
            "padding-bottom:calc(var(--bottom-nav-h,4.25rem) + env(safe-area-inset-bottom,0px) + 20px)!important",
            ndmCss,
            StringComparison.Ordinal);
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
