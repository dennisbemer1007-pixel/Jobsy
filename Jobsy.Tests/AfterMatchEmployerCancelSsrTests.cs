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

public sealed class AfterMatchEmployerCancelSsrTests : BunitContext
{
    public AfterMatchEmployerCancelSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Employer_cancel_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchCancelSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("/after-match/cancel-ssr", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-employer-cancel.js", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_routing_cancel_goes_to_cancel_ssr()
    {
        Assert.Equal("/cancel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("cancel"));
    }

    [Fact]
    public void Shell_renders_cancel_mockup_blocks()
    {
        var culture = Culture;
        var appId = AfterMatchDemoIds.ScheduledApp;
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Scheduled,
            AfterMatchContactShareState.From(InterviewStatus.Scheduled, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            null,
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            null,
            DateTime.UtcNow.AddDays(2),
            null,
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-10),
            null);

        var baseHref = $"/werkgever/sollicitaties/{appId:D}/after-match";
        var data = new AfterMatchEmployerCancelSsrShells.PageData(
            thread,
            baseHref,
            $"/werkgever/sollicitaties/{appId:D}");

        var html = AfterMatchEmployerCancelSsrShells.BuildBody(culture, data, appId);
        Assert.Contains("data-ndm-employer-cancel-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-employer-cancel-submit", html, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Screen08.CancelConfirm"], html, StringComparison.Ordinal);
    }

    [Fact]
    public void Ssr_routing_cancel_scheduled_or_reschedule()
    {
        var scheduled = new AfterMatchThreadDto(
            AfterMatchDemoIds.ScheduledApp,
            Guid.NewGuid(),
            InterviewStatus.Scheduled,
            AfterMatchContactShareState.From(InterviewStatus.Scheduled, null),
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

        Assert.True(AfterMatchEmployerSsrRouting.ShouldUseCancelScreen(scheduled));
        Assert.False(AfterMatchEmployerSsrRouting.ShouldUseCancelScreen(
            scheduled with { InterviewStatus = InterviewStatus.Proposing }));
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
