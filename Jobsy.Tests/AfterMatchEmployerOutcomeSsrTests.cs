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

public sealed class AfterMatchEmployerOutcomeSsrTests : BunitContext
{
    public AfterMatchEmployerOutcomeSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Employer_outcome_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchOutcomeSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("?panel=outcome", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Employer_panel_routing_outcome_goes_to_panel_ssr()
    {
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("outcome"));
    }

    [Fact]
    public void Shell_renders_screen12_mockup_blocks()
    {
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
            null,
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
            "sanne.demo@jobsy.local",
            new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
            null);

        var data = AfterMatchEmployerOutcomeSsrData.FromThread(Culture, thread, appId);
        var html = AfterMatchEmployerOutcomeSsrShells.BuildBody(Culture, data, appId);

        Assert.Contains("data-ndm-employer-outcome-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains(Culture["AfterMatch.Screen12.Question"], html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-outcome-submit", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Ssr_routing_uses_scheduled_app_demo()
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

        Assert.True(AfterMatchEmployerSsrRouting.ShouldUseOutcomeScreen(scheduled));
    }

    [Fact]
    public void Outcome_blocked_before_interview_start()
    {
        var futureStart = DateTime.UtcNow.AddDays(2);
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
            futureStart,
            futureStart.AddMinutes(30),
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne",
            null,
            null,
            null);

        Assert.False(AfterMatchEmployerSsrRouting.CanRecordOutcomeNow(scheduled));

        var culture = Culture;
        var data = AfterMatchEmployerOutcomeSsrData.FromThread(culture, scheduled, AfterMatchDemoIds.ScheduledApp);
        var html = AfterMatchEmployerOutcomeSsrShells.BuildBody(culture, data, AfterMatchDemoIds.ScheduledApp);
        Assert.Contains("na afloop van het gesprek", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data-ndm-outcome-submit", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Seo_catalog_maps_werkgever_outcome_ssr_to_screen12()
    {
        var entry = Jobsy.Web.Seo.PageSeoCatalog.Resolve(
            $"/werkgever/sollicitaties/{AfterMatchDemoIds.ScheduledApp:D}/after-match/outcome-ssr");
        Assert.Equal("AfterMatch.Screen12.Hero.Title", entry.TitleKey);
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
