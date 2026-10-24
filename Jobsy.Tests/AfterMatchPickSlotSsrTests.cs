using System.Net;
using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchPickSlotSsrTests : BunitContext
{
    public AfterMatchPickSlotSsrTests()
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
    public void Pick_slot_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchPickSlotSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("[ExcludeFromInteractiveRouting]", razor, StringComparison.Ordinal);
        Assert.Contains("AfterMatch.Screen04.PageTitle", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-pick-slot.js", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_orders_slots_and_preselects_first()
    {
        var culture = Culture;
        var appId = Guid.Parse("77777771-0002-4000-8000-000000000002");
        var start1 = DateTime.UtcNow.AddDays(3).Date.AddHours(14);
        var start0 = start1.AddDays(-1).AddHours(-4);
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Proposing,
            AfterMatchContactShareState.From(InterviewStatus.Proposing, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            "9 min fietsen",
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            "Kom gerust in je gewone kleren.",
            null,
            null,
            [
                new AfterMatchSlotDto(Guid.NewGuid(), start1, start1.AddMinutes(30), InterviewSlotProposer.Employer, null, false),
                new AfterMatchSlotDto(Guid.NewGuid(), start0, start0.AddMinutes(30), InterviewSlotProposer.Employer, null, false),
            ],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-10));

        var data = new AfterMatchPickSlotSsrData.PageData(
            thread,
            "/candidate/applications",
            "Terug",
            "/candidate/applications",
            thread.OpenSlots.OrderBy(s => s.StartUtc).ToList());

        var html = AfterMatchPickSlotSsrShells.BuildBody(culture, data, appId);

        Assert.Contains("data-ndm-pick-slot-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"bc\"", html, StringComparison.Ordinal);
        Assert.Contains("Goed om te weten", html, StringComparison.Ordinal);
        Assert.Contains("Een ander moment voorstellen", html, StringComparison.Ordinal);
        Assert.Contains("id=\"ndm-propose-form\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-scroll-propose", html, StringComparison.Ordinal);
        Assert.Contains("Vraag het Lobsy", html, StringComparison.Ordinal);

        var firstSlotIdx = html.IndexOf("class=\"slot on\"", StringComparison.Ordinal);
        var secondSlotIdx = html.IndexOf("class=\"slot\"", StringComparison.Ordinal);
        Assert.True(firstSlotIdx >= 0);
        Assert.True(secondSlotIdx > firstSlotIdx);
        var earlierTitle = start0.ToLocalTime().ToString("dddd d MMMM", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));
        if (char.IsLower(earlierTitle[0]))
        {
            earlierTitle = char.ToUpper(earlierTitle[0]) + earlierTitle[1..];
        }

        var laterIdx = html.IndexOf(start1.ToLocalTime().ToString("d MMMM", System.Globalization.CultureInfo.GetCultureInfo("nl-NL")), StringComparison.Ordinal);
        var earlierIdx = html.IndexOf(earlierTitle, StringComparison.Ordinal);
        Assert.True(earlierIdx >= 0 && laterIdx >= 0);
        Assert.True(earlierIdx < laterIdx, "Slots must be chronological in markup.");
    }

    [Fact]
    public void Playwright_screenshots_never_confirm_a_slot()
    {
        var root = FindRepoRoot();
        var playwright = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Tests/E2e/NaDeMatchScreenshotPlaywrightTests.cs"));
        Assert.DoesNotContain("data-ndm-confirm-slot", playwright, StringComparison.Ordinal);
        Assert.DoesNotContain("select-slot", playwright, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dit moment kiezen", playwright, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_seo_catalog_uses_gesprek_plannen_for_pick_slot_ssr()
    {
        var appId = Guid.Parse("77777771-0002-4000-8000-000000000002");
        var path = $"/candidate/applications/{appId:D}/after-match/pick-slot-ssr";
        var entry = Jobsy.Web.Seo.PageSeoCatalog.Resolve(path);
        Assert.Equal("AfterMatch.Screen04.PageTitle", entry.TitleKey);
    }

    [Fact]
    public void Mark_read_flush_script_uses_bff_only()
    {
        var root = FindRepoRoot();
        var flush = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/sollicitaties-mark-read-flush.js"));
        var berichten = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/sollicitaties-berichten.js"));
        Assert.Contains("/api/sollicitaties-client/berichten/mark-read", flush, StringComparison.Ordinal);
        Assert.DoesNotContain("after-match/messages/read", flush, StringComparison.Ordinal);
        Assert.DoesNotContain("after-match/messages/read", berichten, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_breadcrumb_uses_return_href_and_has_mockup_footer()
    {
        var culture = Culture;
        var appId = Guid.Parse("77777771-0002-4000-8000-000000000002");
        var returnHref = "/candidate/applications?tab=berichten&thread=" + appId.ToString("D");
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Proposing,
            AfterMatchContactShareState.From(InterviewStatus.Proposing, null),
            null,
            "Functie",
            "Bedrijf",
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            null,
            null,
            [new AfterMatchSlotDto(Guid.NewGuid(), DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddMinutes(30), InterviewSlotProposer.Employer, null, false)],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Test",
            null,
            DateTime.UtcNow.AddDays(-1));
        var data = new AfterMatchPickSlotSsrData.PageData(
            thread,
            returnHref,
            "Terug",
            returnHref,
            thread.OpenSlots);
        var html = AfterMatchPickSlotSsrShells.BuildBody(culture, data, appId);
        Assert.Contains(WebUtility.HtmlEncode(returnHref), html, StringComparison.Ordinal);
        Assert.DoesNotContain("ndm-back", html, StringComparison.Ordinal);
        Assert.Contains("ndm-mockup-footer", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-propose-date", html, StringComparison.Ordinal);
        Assert.DoesNotContain("datetime-local", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Route_rewrite_middleware_type_is_registered()
    {
        var root = FindRepoRoot();
        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("UseAfterMatchPickSlotRouteRewrite", program, StringComparison.Ordinal);
        Assert.Contains("MapAfterMatchPickSlotClientEndpoints", program, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repo root not found");
    }
}
