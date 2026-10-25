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

public sealed class AfterMatchEmployerProposeSsrTests : BunitContext
{
    public AfterMatchEmployerProposeSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Employer_propose_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchProposeSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("after-match?panel=propose", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Employer_panel_routing_propose_goes_to_panel_ssr()
    {
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("propose"));
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("slots"));
    }

    [Fact]
    public void Shell_renders_propose_panel_blocks()
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
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-2),
            null);

        var data = new AfterMatchEmployerProposeSsrShells.PageData(
            thread,
            $"/werkgever/sollicitaties/{appId:D}/after-match",
            $"/werkgever/sollicitaties/{appId:D}",
            AfterMatchEmployerDemoSlots.InitialUtc,
            InterviewFormat.OnSite,
            "Loc",
            "Linda",
            "Note");

        var html = AfterMatchEmployerProposeSsrShells.BuildBody(culture, data, appId);
        Assert.Contains("data-ndm-panel=\"propose\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-propose-panel", html, StringComparison.Ordinal);
        Assert.Contains("Donderdag 22 oktober", html, StringComparison.Ordinal);
        Assert.Contains("Vrijdag 23 oktober", html, StringComparison.Ordinal);
        Assert.Contains("Maandag 26 oktober", html, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Screen03.Submit"], html, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Employer.Propose.AddSlot"], html, StringComparison.Ordinal);
        Assert.Contains("Telefonisch", html, StringComparison.Ordinal);
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
