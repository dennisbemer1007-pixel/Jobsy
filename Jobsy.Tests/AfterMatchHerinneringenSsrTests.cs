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
        Assert.Contains("ndm-mail-chrome.js", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_renders_reminder_mockup_blocks()
    {
        var culture = Culture;
        var demo = AfterMatchHerinneringenSsrData.Demo(culture);
        var html = AfterMatchHerinneringenSsrShells.BuildBody(
            culture,
            demo,
            AfterMatchDemoIds.ScheduledApp,
            includeBackLink: false,
            editableReminders: false);

        Assert.Contains("Morgen je gesprek bij Bakkerij De Korenaar", html, StringComparison.Ordinal);
        Assert.Contains("Linda kijkt ernaar uit", html, StringComparison.Ordinal);
        Assert.Contains("ingang naast de winkel", html, StringComparison.Ordinal);
        Assert.Contains("Droog, 13 graden", html, StringComparison.Ordinal);
        Assert.Contains("Route bekijken", html, StringComparison.Ordinal);
        Assert.Contains("Oefen met Lobsy", html, StringComparison.Ordinal);
        Assert.Contains("donderdag 22 oktober", html, StringComparison.Ordinal);
        Assert.Contains("Met de fiets ben je er in 9 minuten", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-ask-lobsy", html, StringComparison.Ordinal);
        Assert.Contains("ndm-mockup-footer", html, StringComparison.Ordinal);
        Assert.Contains(culture["Footer.Privacy"], html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-ndm-reminder-save", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Live_shell_includes_editable_save()
    {
        var demo = AfterMatchHerinneringenSsrData.Demo(Culture);
        var html = AfterMatchHerinneringenSsrShells.BuildBody(
            Culture,
            demo,
            AfterMatchDemoIds.ScheduledApp,
            includeBackLink: false,
            editableReminders: true);

        Assert.Contains("data-ndm-reminder-save", html, StringComparison.Ordinal);
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
