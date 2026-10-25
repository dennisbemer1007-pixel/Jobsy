using Bunit;
using Jobsy.Core.Contracts;
using Microsoft.AspNetCore.Http;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchMessagesSsrTests : BunitContext
{
    public AfterMatchMessagesSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    [Fact]
    public void Messages_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchMessagesSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-messages.js?v=20261027-ndm-messages5", razor, StringComparison.Ordinal);
        Assert.Contains("na-de-match.css?v=20261027-ndm-messages5", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Middleware_routes_panel_messages_to_messages_ssr()
    {
        var src = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Middleware/AfterMatchPickSlotRouteRewriteMiddleware.cs"));
        Assert.Contains("AfterMatchPanelRouting", src, StringComparison.Ordinal);
    }

    [Fact]
    public void View_application_from_messages_done_goes_to_after_match_outcome()
    {
        var appId = Guid.Parse("b0000001-0000-4000-8000-000000000037");
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Done,
            AfterMatchContactShareState.From(InterviewStatus.Done, AfterMatchOutcomeKind.NotNow),
            AfterMatchOutcomeKind.NotNow,
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
            "Uitkomst reden");
        var href = AfterMatchSollicitatiesUrls.ViewApplicationFromMessages(appId, thread);
        Assert.Contains($"/candidate/applications/{appId:D}/after-match", href, StringComparison.Ordinal);
        Assert.Contains("return=", href, StringComparison.Ordinal);
        Assert.Contains("panel%3Dmessages", href, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void View_application_from_messages_without_slots_goes_to_sollicitaties_detail()
    {
        var appId = Guid.Parse("b0000001-0000-4000-8000-000000000036");
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Proposing,
            AfterMatchContactShareState.From(InterviewStatus.Proposing, null),
            null,
            "Medewerker",
            "Testbedrijf Grijs (test)",
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
        var href = AfterMatchSollicitatiesUrls.ViewApplicationFromMessages(appId, thread);
        Assert.Contains($"/candidate/applications?sel={appId:D}", href, StringComparison.Ordinal);
        Assert.Contains("return=", href, StringComparison.Ordinal);
        Assert.Contains("panel%3Dmessages", href, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void App_public_nav_sync_uses_prefix_match_for_applications()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/js/app-public.js"));
        Assert.Contains("navLinkMatchesPath", src, StringComparison.Ordinal);
        Assert.Contains("h + \"/\"", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Applications_nav_active_on_after_match_subpaths()
    {
        var flags = FeatureFlagSnapshot.Defaults with { EmployersEnabled = true };
        var items = RoleNavCatalog.CandidateItems(flags);
        var apps = items.First(i => i.TitleKey == "Nav.Applications");
        Assert.True(RoleNavCatalog.IsActive(
            apps,
            "candidate/applications/b0000001-0000-4000-8000-000000000035/after-match/messages-ssr",
            items));
    }

    [Fact]
    public void Messages_shell_uses_intro_not_hero_band()
    {
        var src = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Applications/AfterMatchMessagesSsrShells.cs"));
        Assert.Contains("ndm-messages-intro", src, StringComparison.Ordinal);
        Assert.DoesNotContain("ndm-hero--gesprek", src, StringComparison.Ordinal);
        Assert.Contains("PublicFooter.Accessibility", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_routing_messages_wins_over_trailing_cancel()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["panel"] = new[] { "messages", "cancel" },
        });
        Assert.Equal("messages", AfterMatchPanelRouting.ResolvePanel(query));
        Assert.Equal("/messages-ssr", AfterMatchPanelRouting.PathSuffixForPanel("messages"));
    }

    [Fact]
    public void Reschedule_footnote_is_always_rendered()
    {
        var html = BuildRescheduleShell("reschedule");
        Assert.Contains("ndm-reschedule-cancel-footnote", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ndm-reschedule-cancel-footnote\" hidden", html, StringComparison.Ordinal);
        Assert.Contains("Afzeggen telt niet tegen je", html, StringComparison.Ordinal);
    }

    private static string BuildRescheduleShell(string mode)
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
            null);

        var data = new AfterMatchRescheduleSsrData.PageData(
            thread,
            "/candidate/applications",
            $"/candidate/applications/{appId:D}/after-match",
            $"/candidate/applications/{appId:D}/after-match?panel=pick-slot",
            "Linda",
            "Lead",
            "Donderdag 22 oktober, 10:30",
            "Op locatie",
            "13 okt",
            "15 okt",
            "22 okt",
            null);

        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(
            new FakeAuth());
        ctx.Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
        var culture = ctx.Services.GetRequiredService<CultureState>();
        return AfterMatchRescheduleSsrShells.BuildBody(culture, data, appId, mode);
    }

    private sealed class FakeAuth : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
    {
        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(new()));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
