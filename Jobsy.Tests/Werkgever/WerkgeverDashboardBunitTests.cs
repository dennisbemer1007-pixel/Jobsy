using Bunit;
using Jobsy.Core.Interfaces;
using Jobsy.Web.Components.Ui.Enterprise;
using Jobsy.Web.Components.Werkgever.Dashboard;
using Jobsy.Web.Components.Werkgever.Shell;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Werkgever;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Werkgever;

public class WerkgeverDashboardBunitTests : TestContext
{
    public WerkgeverDashboardBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(new EmployerScopeState());
    }

    [Fact]
    public void WgTodoItem_renders_title_and_action()
    {
        var item = new WerkgeverTodoItemDto(
            "ApplicationsOverdue",
            "Danger",
            "WgTodo.ApplicationsOverdue.Title",
            ["17"],
            "WgTodo.ApplicationsOverdue.Meta",
            ["Ridderkerk"],
            "Bekijken",
            "/werkgever/sollicitaties?filter=overdue",
            17,
            [Guid.NewGuid()]);

        var cut = RenderComponent<WgTodoItem>(p => p
            .Add(x => x.Item, item)
            .Add(x => x.ShowAction, true));

        Assert.Contains("wg-todo-item--danger", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Bekijken", cut.Markup);
        Assert.Contains("/werkgever/sollicitaties?filter=overdue", cut.Markup);
    }

    [Fact]
    public void WgTodoItem_hides_action_when_requested()
    {
        var item = new WerkgeverTodoItemDto(
            "PublishRequests",
            "Warning",
            "WgTodo.PublishRequests.Title",
            ["3"],
            "WgTodo.PublishRequests.MetaRm",
            ["Naaldwijk"],
            "",
            "/werkgever/vacatures?tab=wacht",
            3,
            [Guid.NewGuid()]);

        var cut = RenderComponent<WgTodoItem>(p => p
            .Add(x => x.Item, item)
            .Add(x => x.ShowAction, false));

        Assert.DoesNotContain("wg-todo-item__action", cut.Markup);
    }

    [Fact]
    public void EntKpiCard_no_upsell_gold_on_dashboard_primitives()
    {
        var cut = RenderComponent<EntKpiCard>(p => p
            .Add(x => x.Label, "Actieve vacatures")
            .Add(x => x.Value, "48")
            .Add(x => x.Delta, "+4")
            .Add(x => x.DeltaTone, "up"));
        Assert.Contains("Actieve vacatures", cut.Markup);
        Assert.DoesNotContain("gold", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("upsell", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Kandidaatinzichten", cut.Markup);
    }

    [Fact]
    public void WgAction_rm_locks_write_primary()
    {
        var scope = Services.GetRequiredService<EmployerScopeState>();
        scope.Initialize(
            EmployerRole.Regiomanager,
            [new EmployerScopeOption(EmployerScopeKind.Region, Guid.NewGuid(), "Westland")],
            new Dictionary<string, IReadOnlyList<Guid>>());

        var cut = RenderComponent<WgAction>(p => p
            .Add(x => x.RequiresWrite, true)
            .Add(x => x.Kind, WgAction.WgActionKind.Primary)
            .AddChildContent("Vacature plaatsen"));

        Assert.Contains("is-locked", cut.Markup);
        Assert.Contains("disabled", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rights_matrix_includes_dashboard_apis_and_te_doen_page()
    {
        Assert.Contains(WerkgeverRightsMatrix.Pages, p => p.Route == "/werkgever/te-doen");
        Assert.Contains(WerkgeverRightsMatrix.Apis, a => a.Path == "api/werkgever/dashboard");
        Assert.Contains(WerkgeverRightsMatrix.Apis, a => a.Path == "api/werkgever/te-doen");
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
