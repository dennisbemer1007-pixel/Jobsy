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

public sealed class AfterMatchEmployerRescheduleSsrTests : BunitContext
{
    public AfterMatchEmployerRescheduleSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Employer_reschedule_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchRescheduleSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("/after-match/reschedule-ssr", razor, StringComparison.Ordinal);
        Assert.Contains("WwwrootAssetCatalog.Url(\"js/ndm-employer-reschedule.js\")", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_ssr_renders_reschedule_inline_without_http_redirect()
    {
        var src = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchPanelSsr.razor"));
        Assert.Contains("ShouldRedirectToRescheduleSsr", src, StringComparison.Ordinal);
        Assert.Contains("AfterMatchEmployerRescheduleSsrShells.BuildBody", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Response.Redirect", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_renders_screen09_mockup_blocks()
    {
        var culture = Culture;
        var appId = AfterMatchDemoIds.RescheduleApp;
        var slotId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var slotStart = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.RescheduleRequested,
            AfterMatchContactShareState.From(InterviewStatus.RescheduleRequested, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            null,
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            null,
            slotStart,
            slotStart.AddMinutes(30),
            [
                new AfterMatchSlotDto(
                    slotId,
                    slotStart,
                    slotStart.AddMinutes(30),
                    InterviewSlotProposer.Candidate,
                    "Mijn zoon is ziek; ik kan op dat moment niet weg.",
                    false)
            ],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [
                new AfterMatchChangeDto(
                    InterviewChangeKind.Reschedule,
                    "Candidate",
                    "zorg voor familie. \"Mijn zoon is ziek; ik kan op dat moment niet weg.\"",
                    DateTime.UtcNow)
            ],
            [],
            "Sanne de Vries",
            "sanne.demo@jobsy.local",
            new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
            null);

        var data = AfterMatchEmployerRescheduleSsrData.FromThread(culture, thread, appId);
        var html = AfterMatchEmployerRescheduleSsrShells.BuildBody(culture, data, appId);

        Assert.Contains("data-testid=\"ndm-werkgever-live\"", html, StringComparison.Ordinal);
        Assert.Contains("warnin", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-employer-accept-slot", html, StringComparison.Ordinal);
        Assert.Contains(slotId.ToString("D"), html, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Employer.Reschedule.WeekAside"], html, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Employer.Reschedule.Footnote"], html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-open-panel=\"propose\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("15 okt", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(AfterMatchTimelineLabels.InterviewStep(thread), html, StringComparison.Ordinal);
    }

    [Fact]
    public void Accept_shows_when_candidate_picked_employer_alternative_slot()
    {
        var culture = Culture;
        var appId = AfterMatchDemoIds.RescheduleApp;
        var slotId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var slotStart = new DateTime(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.RescheduleRequested,
            AfterMatchContactShareState.From(InterviewStatus.RescheduleRequested, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            null,
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            null,
            slotStart,
            slotStart.AddMinutes(30),
            [
                new AfterMatchSlotDto(
                    slotId,
                    slotStart,
                    slotStart.AddMinutes(30),
                    InterviewSlotProposer.Employer,
                    null,
                    true)
            ],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [
                new AfterMatchChangeDto(
                    InterviewChangeKind.Reschedule,
                    "Candidate",
                    "Kan die dag niet.",
                    DateTime.UtcNow)
            ],
            [],
            "Sanne de Vries",
            null,
            new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
            null);

        var data = AfterMatchEmployerRescheduleSsrData.FromThread(culture, thread, appId);
        var html = AfterMatchEmployerRescheduleSsrShells.BuildBody(culture, data, appId);
        Assert.Contains("data-ndm-employer-accept-slot", html, StringComparison.Ordinal);
        Assert.Contains(slotId.ToString("D"), html, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Employer.Reschedule.EmployerSlotHint"], html, StringComparison.Ordinal);
    }

    [Fact]
    public void Ssr_routing_only_reschedule_requested()
    {
        var baseThread = new AfterMatchThreadDto(
            AfterMatchDemoIds.RescheduleApp,
            Guid.NewGuid(),
            InterviewStatus.RescheduleRequested,
            AfterMatchContactShareState.From(InterviewStatus.RescheduleRequested, null),
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

        Assert.True(AfterMatchEmployerSsrRouting.ShouldUseRescheduleScreen(baseThread));
        Assert.False(AfterMatchEmployerSsrRouting.ShouldUseRescheduleScreen(
            baseThread with { InterviewStatus = InterviewStatus.Proposing }));
        Assert.False(AfterMatchEmployerSsrRouting.ShouldUseRescheduleScreen(
            baseThread with { InterviewStatus = InterviewStatus.Scheduled }));
        Assert.True(AfterMatchEmployerSsrRouting.ShouldAllowEmployerRescheduleSsrRoute(
            baseThread with { InterviewStatus = InterviewStatus.Scheduled }));
        Assert.False(AfterMatchEmployerSsrRouting.ShouldRedirectToRescheduleSsr(
            baseThread with { InterviewStatus = InterviewStatus.Scheduled },
            ""));
        Assert.False(AfterMatchEmployerSsrRouting.ShouldRedirectToRescheduleSsr(
            baseThread with { InterviewStatus = InterviewStatus.Scheduled },
            "outcome"));
        Assert.True(AfterMatchEmployerSsrRouting.ShouldRedirectToRescheduleSsr(baseThread, ""));
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
