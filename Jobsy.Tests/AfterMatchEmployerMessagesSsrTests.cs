using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Jobsy.Web.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchEmployerMessagesSsrTests : BunitContext
{
    public AfterMatchEmployerMessagesSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    private CultureState Culture => Services.GetRequiredService<CultureState>();

    [Fact]
    public void Employer_messages_ssr_page_is_no_blazor_static()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Pages/Werkgever/WerkgeverAfterMatchMessagesSsr.razor"));
        Assert.Contains("[NoBlazorRuntime]", razor, StringComparison.Ordinal);
        Assert.Contains("?panel=messages", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void Middleware_routes_werkgever_panel_messages_to_panel_ssr()
    {
        var src = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Middleware/AfterMatchPickSlotRouteRewriteMiddleware.cs"));
        Assert.Contains("AfterMatchEmployerPanelRouting", src, StringComparison.Ordinal);
        Assert.Equal("/panel-ssr", AfterMatchEmployerPanelRouting.PathSuffixForPanel("messages"));
    }

    [Fact]
    public void Shell_renders_employer_messages_hub()
    {
        var culture = Culture;
        var appId = AfterMatchDemoIds.ScheduledApp;
        var thread = DemoThread(appId);
        var item = new Jobsy.Web.Models.EmployerApplicationItem
        {
            Id = appId,
            VacancyTitle = "Medewerker bakkerij",
            CandidateName = "Sanne de Vries",
            Status = "Accepted",
            PiiRevealed = true,
        };
        var data = new AfterMatchEmployerMessagesSsrData.PageData(
            appId,
            appId,
            AfterMatchEmployerSollicitatiesUrls.Messages(appId),
            "/werkgever/sollicitaties",
            [new AfterMatchEmployerMessagesSsrData.ThreadRow(item, thread)]);

        var html = AfterMatchEmployerMessagesSsrShells.BuildBody(culture, data, appId);
        Assert.Contains("data-testid=\"ndm-werkgever-live\"", html, StringComparison.Ordinal);
        Assert.Contains("data-ndm-employer-messages-ssr=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("Sanne de Vries", html, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Screen11.SafeTitle"], html, StringComparison.Ordinal);
        Assert.Contains("ndm-wg-messages-compose", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Ssr_routing_messages_eligible_when_thread_has_messages()
    {
        var thread = DemoThread(AfterMatchDemoIds.ScheduledApp);
        Assert.True(AfterMatchEmployerSsrRouting.ShouldUseMessagesScreen(thread));
        Assert.False(AfterMatchEmployerSsrRouting.ShouldUseMessagesScreen(
            thread with { Messages = [], AcceptedAtUtc = null, InterviewStatus = InterviewStatus.Proposing }));
    }

    [Fact]
    public void Web_host_maps_werkgever_client_berichten_bff()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Hosting/WerkgeverClientEndpoints.cs"));
        Assert.Contains("/api/werkgever-client/berichten/mark-read", source, StringComparison.Ordinal);
        Assert.Contains("/api/werkgever-client/berichten/send", source, StringComparison.Ordinal);
    }

    private static AfterMatchThreadDto DemoThread(Guid appId) =>
        new(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Scheduled,
            AfterMatchContactShareState.From(InterviewStatus.Scheduled, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            DateTime.UtcNow.AddDays(2),
            null,
            [],
            [
                new AfterMatchMessageDto(
                    Guid.NewGuid(),
                    true,
                    "Jij",
                    "Hoi Sanne!",
                    DateTime.UtcNow.AddDays(-1),
                    null,
                    ApplicationMessageBlockReason.None,
                    false),
            ],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-3),
            null);

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
