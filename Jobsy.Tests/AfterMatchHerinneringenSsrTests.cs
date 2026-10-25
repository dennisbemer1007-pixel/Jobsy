using Bunit;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchHerinneringenSsrTests : BunitContext
{
    public AfterMatchHerinneringenSsrTests()
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
    public void Herinneringen_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchHerinneringenSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("herinneringen-ssr", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-herinneringen.js", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_renders_reminder_mockup_blocks()
    {
        var culture = Culture;
        var demo = AfterMatchHerinneringenSsrData.Demo();
        var html = AfterMatchHerinneringenSsrShells.BuildBody(
            culture,
            demo,
            AfterMatchDemoIds.ScheduledApp,
            includeBackLink: false,
            editableReminders: true);

        Assert.Contains("data-ndm-herinneringen-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("De dag ervoor en 2 uur van tevoren", html, StringComparison.Ordinal);
        Assert.Contains("Morgen is je gesprek", html, StringComparison.Ordinal);
        Assert.Contains("Over 2 uur je gesprek", html, StringComparison.Ordinal);
        Assert.Contains("class=\"phone\"", html, StringComparison.Ordinal);
        Assert.Contains("Zelf instellen", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-reminder-save", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-reminder=\"dayBefore18\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_shell_omits_editable_save_on_demo()
    {
        var demo = AfterMatchHerinneringenSsrData.Demo();
        var html = AfterMatchHerinneringenSsrShells.BuildBody(
            Culture,
            demo,
            AfterMatchDemoIds.ScheduledApp,
            includeBackLink: false,
            editableReminders: false);

        Assert.DoesNotContain("data-ndm-reminder-save", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-ndm-reminder-editable", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_seo_catalog_uses_herinneringen_title()
    {
        var appId = AfterMatchDemoIds.ScheduledApp;
        var path = $"/candidate/applications/{appId:D}/after-match/herinneringen-ssr";
        var entry = Jobsy.Web.Seo.PageSeoCatalog.Resolve(path);
        Assert.Equal("AfterMatch.Screen07.PageTitle", entry.TitleKey);
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
