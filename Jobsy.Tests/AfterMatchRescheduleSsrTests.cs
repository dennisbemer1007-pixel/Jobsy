using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Ops;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchRescheduleSsrTests : BunitContext
{
    public AfterMatchRescheduleSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Reschedule_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Candidate/CandidateAfterMatchRescheduleSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("ndm-reschedule.js?v=20261027-ndm-reschedule4", razor, StringComparison.Ordinal);
        Assert.Contains("na-de-match.css?v=20261027-ndm-messages1", razor, StringComparison.Ordinal);
        var ndmCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/na-de-match.css"));
        Assert.Contains(".ndm-reschedule-page button.chip.on{border-color:var(--brand)", ndmCss, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_renders_mockup_08_blocks()
    {
        var culture = Culture;
        var appId = TestAccountsIds.SollicDemoAppScheduled;
        var start = new DateTime(2026, 10, 22, 8, 30, 0, DateTimeKind.Utc);
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Scheduled,
            AfterMatchContactShareState.From(InterviewStatus.Scheduled, null),
            null,
            "Administratief medewerker (test)",
            "Testbedrijf Blauw (test)",
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
            "/candidate/applications?tab=berichten",
            $"/candidate/applications/{appId:D}/after-match?return=%2Fcandidate%2Fapplications%3Ftab%3Dberichten",
            $"/candidate/applications/{appId:D}/after-match?panel=pick-slot",
            "Linda",
            "Dat kan gebeuren. Laat Linda weten waarom, dan zoeken jullie samen een nieuw moment.",
            "Donderdag 22 oktober, 10:30",
            "Op locatie, Loosduinseweg 120, Den Haag",
            "13 okt",
            "15 okt",
            "22 okt",
            null);

        var html = AfterMatchRescheduleSsrShells.BuildBody(culture, data, appId, "reschedule");
        var htmlCancel = AfterMatchRescheduleSsrShells.BuildBody(culture, data, appId, "cancel");

        Assert.Contains("data-ndm-reschedule-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ndm-reschedule-eyb", html, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"", html, StringComparison.Ordinal);
        Assert.Contains("Mijn zoon is ziek", html, StringComparison.Ordinal);
        Assert.Contains("ndm-reschedule-nevermind", html, StringComparison.Ordinal);
        Assert.Contains("Gesprek verzetten", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-mode-chip=\"reschedule\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-mode-chip=\"cancel\"", html, StringComparison.Ordinal);
        Assert.Contains("Zorg voor familie", html, StringComparison.Ordinal);
        Assert.Contains("Wanneer kan het wel?", html, StringComparison.Ordinal);
        Assert.Contains("Verstuur naar Linda", html, StringComparison.Ordinal);
        Assert.Contains("Toch niet", html, StringComparison.Ordinal);
        Assert.Contains("data-back-href=", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-when-block", html, StringComparison.Ordinal);

        Assert.Contains("data-ndm-submit-cancel", htmlCancel, StringComparison.Ordinal);
        Assert.Contains("Afzeggen telt niet tegen je", htmlCancel, StringComparison.Ordinal);
        Assert.Contains("data-ndm-submit-cancel=\"\" hidden", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-ndm-submit-reschedule=\"\" hidden", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-submit-reschedule=\"\" hidden", htmlCancel, StringComparison.Ordinal);
    }

    [Fact]
    public void Blauw_scheduled_app_id_is_035()
    {
        Assert.Equal(Guid.Parse("b0000001-0000-4000-8000-000000000035"), TestAccountsIds.SollicDemoAppScheduled);
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
